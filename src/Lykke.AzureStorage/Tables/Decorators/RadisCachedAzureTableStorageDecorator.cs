using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables.Redis;
using Common.Log;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using StackExchange.Redis;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    ///  Cached in Redis <see cref="INoSQLTableStorage{T}"/> decorator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class RedisCachedAzureTableStorageDecorator<T> : INoSQLTableStorage<T> where T : class, ITableEntity, new()
    {
        public string Name => _table.Name;
        
        private readonly INoSQLTableStorage<T> _table;
        private readonly INoSQLTableStorage<T> _redis;
        private readonly ILog _log;
        private readonly bool _useLazyLoadInCache;

        //todo: Если на запросе изменения редис не доступен, то попробовать еще пару раз. Если не получилось, то пометить кеш не валидным и перезалить при востановлении связи

        //todo: Если массовые операции на изменения не пройдут в БД, то будет ошибка и кеш не обновится. Нет гарантии что в БД изменения частично не ваыполнились. Надо перезалить или очистить кеш в таком случае. 

        public RedisCachedAzureTableStorageDecorator(
            INoSQLTableStorage<T> table, 
            IDistributedCache cache, 
            IDatabase database, 
            IServer redisServer,
            IAzureRedisSettings settings, 
            string tableKeysOffset,
            ILog log)
        {
            _table = table;
            var radisSettings = new NoSqlTableInRedisSettings()
            {
                AbsoluteExpirationRelativeToNow = settings.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = settings.SlidingExpiration,
                TableName = tableKeysOffset
            };
            _redis = new NoSqlTableInRedis<T>(cache, database, redisServer, radisSettings, log);
            _log = log;

            _useLazyLoadInCache = settings.UseLazyLoadInCache;

            if (!_useLazyLoadInCache)
            {
                LoadCache().Wait();
            }
        }

        private async Task<T> RefreshEntityInCache(string partitionKey, string rowKey)
        {
            var entity = await GetEntityFromTable(partitionKey, rowKey);
            if (entity != null)
            {
                await _redis.InsertOrReplaceAsync(entity);
            }
            else
            {
                await _redis.DeleteAsync(partitionKey, rowKey);
            }
            return entity;
        }

        private async Task<T> GetEntityFromTable(string partitionKey, string rowKey)
        {
            var entity = await _table.GetDataAsync(partitionKey, rowKey);
            return entity;
        }

        private async Task<IEnumerable<T>> GetPartition(string partition)
        {
            if (_useLazyLoadInCache)
            {
                var tableData = _table[partition];
                var data = tableData as T[] ?? tableData.ToArray();
                await _redis.InsertOrReplaceAsync(data);
                return data;
            }
            else
            {
                var data = _redis[partition];
                return data;
            }
        }

        public async Task InsertAsync(T item, params int[] notLogCodes)
        {
            await _table.InsertAsync(item, notLogCodes);
            await _redis.InsertAsync(item);
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            var data = items as T[] ?? items.ToArray();
            await _table.InsertAsync(data);
            await _redis.InsertAsync(data);
        }

        public async Task InsertOrMergeAsync(T item)
        {
            await _table.InsertOrMergeAsync(item);
            await RefreshEntityInCache(item.PartitionKey, item.RowKey);
        }

        public async Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            var data = items.ToArray();
            await _table.InsertOrMergeBatchAsync(data);

            foreach (var item in data)
                await RefreshEntityInCache(item.PartitionKey, item.RowKey);
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _table.ReplaceAsync(partitionKey, rowKey, item);
            await _redis.InsertOrReplaceAsync(result);

            return result;
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _table.MergeAsync(partitionKey, rowKey, item);
            await _redis.InsertOrReplaceAsync(result);
            return result;
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entites)
        {
            var data = entites as T[] ?? entites.ToArray();
            await _table.InsertOrReplaceBatchAsync(data);
            await _redis.InsertOrReplaceBatchAsync(data);
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await _table.InsertOrReplaceAsync(item);
            await _redis.InsertOrReplaceAsync(item);
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            var data = items as T[] ?? items.ToArray();
            await _table.InsertOrReplaceAsync(data);
            foreach (var item in data)
                await _redis.InsertOrReplaceAsync(item);
        }

        public async Task DeleteAsync(T item)
        {
            await _table.DeleteAsync(item);
            await _redis.DeleteAsync(item);
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var result = await _table.DeleteAsync(partitionKey, rowKey);
            await _redis.DeleteAsync(partitionKey, rowKey);
            return result;
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            try
            {
                await DeleteAsync(partitionKey, rowKey);
            }
            catch (StorageException ex)
            {
                if (ex.RequestInformation.HttpStatusCode == 404)
                    return false;

                throw;
            }

            await _redis.DeleteAsync(partitionKey, rowKey);

            return true;
        }

        public async Task<bool> DeleteAsync()
        {
            await _redis.DeleteAsync();
            var result = await _table.DeleteAsync();
            if (!result && !_useLazyLoadInCache)
            {
                await LoadCache();
            }
            return result;
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            var data = items as T[] ?? items.ToArray();
            await _table.DeleteAsync(data);
            await _redis.DeleteAsync(data);
        }

        public async Task<bool> CreateIfNotExistsAsync(T item)
        {
            var res = await _table.CreateIfNotExistsAsync(item);
            await _redis.InsertOrReplaceAsync(item);

            return res;
        }

        public bool RecordExists(T item)
        {
            var result = _redis.RecordExists(item);
            if (!result && _useLazyLoadInCache)
            {
                result = _table.RecordExists(item);
            }
            return result;
        }

        public Task<bool> RecordExistsAsync(T item)
        {
            return _table.RecordExistsAsync(item);
        }

        public async Task DoBatchAsync(TableBatchOperation batch)
        {
            if (_useLazyLoadInCache)
            {
                await _table.DoBatchAsync(batch);
                await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(DoBatchAsync), $"Table {_table.Name}, Entity {nameof(T)}", 
                    "Method DoBatchAsync not supported in radis cache");
            }
            else
            {
                var error = new NotSupportedException("Method DoBatchAsync not supported in radis cache and can't be used in mode UseLazyLoadInCache=false");
                await _log.WriteErrorAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(DoBatchAsync), $"Table {_table.Name}, Entity {nameof(T)}", error);
                throw error;
            }
        }

        public async Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(ExecuteQueryWithPaginationAsync), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method ExecuteQueryWithPaginationAsync not supported in radis cache");

            return await _table.ExecuteQueryWithPaginationAsync(query, pagingInfo);
        }

        public Task CreateTableIfNotExistsAsync()
        {
            return _table.CreateTableIfNotExistsAsync();
        }

        T INoSQLTableStorage<T>.this[string partition, string row]
        {
            get
            {
                var data = _redis[partition, row];
                if (data == null && _useLazyLoadInCache)
                {
                    data = RefreshEntityInCache(partition, row).Result;
                }
                return data;
            }
        }

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => GetPartition(partition).Result;

        public async Task<T> GetDataAsync(string partition, string row)
        {
            var item = await _redis.GetDataAsync(partition, row);
            if (item == null && _useLazyLoadInCache)
            {
                item = RefreshEntityInCache(partition, row).Result;
            }
            return item;
        }

        public Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            if (_useLazyLoadInCache)
            {
                return _table.GetDataAsync(filter);
            }

            return  _redis.GetDataAsync(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            if (!_useLazyLoadInCache)
            {
                return await _redis.GetDataAsync(partitionKey, rowKeys, pieceSize, filter);
            }

            var rows = rowKeys as string[] ?? rowKeys.ToArray();
            
            var itemsResult = await _redis.GetDataAsync(partitionKey, rows, pieceSize, filter);
            var items = itemsResult as T[] ?? itemsResult.ToArray();

            if (items.Length == pieceSize)
                return items;

            rows = rows.Where(e => items.All(i => i.RowKey != e)).ToArray();

            var tableItemsResult = await _table.GetDataAsync(partitionKey, rows, pieceSize - items.Length, filter);
            var tableItems = tableItemsResult as T[] ?? tableItemsResult.ToArray();

            await _redis.InsertOrReplaceAsync(tableItems);
            
            return items.Union(tableItems);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            if (!_useLazyLoadInCache)
            {
                return await _redis.GetDataAsync(partitionKeys, pieceSize, filter);
            }

            var partitions = partitionKeys as string[] ?? partitionKeys.ToArray();

            var itemsResult = await _redis.GetDataAsync(partitions, pieceSize, filter);
            var items = itemsResult as T[] ?? itemsResult.ToArray();

            if (items.Length == pieceSize)
                return items;

            partitions = partitions.Where(e => items.All(i => i.PartitionKey != e)).ToArray();

            var tableItemsResult = await _table.GetDataAsync(partitions, pieceSize - items.Length, filter);
            var tableItems = tableItemsResult as T[] ?? tableItemsResult.ToArray();

            await _redis.InsertOrReplaceAsync(tableItems);

            return items.Union(tableItems);
        }


        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            if (!_useLazyLoadInCache)
            {
                return await _redis.GetDataAsync(keys, pieceSize, filter);
            }

            var keysArray = keys as Tuple<string, string>[] ?? keys.ToArray();

            var itemsResult = await _redis.GetDataAsync(keysArray, pieceSize, filter);
            var items = itemsResult as T[] ?? itemsResult.ToArray();

            if (items.Length == pieceSize)
                return items;

            keysArray = keysArray.Where(e => items.All(i => i.PartitionKey != e.Item1 && i.RowKey != e.Item2)).ToArray();

            var tableItemsResult = await _table.GetDataAsync(keysArray, pieceSize - items.Length, filter);
            var tableItems = tableItemsResult as T[] ?? tableItemsResult.ToArray();

            await _redis.InsertOrReplaceAsync(tableItems);

            return items.Union(tableItems);
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            var item = await _redis.GetTopRecordAsync(partition);
            if (item == null && _useLazyLoadInCache)
            {
                item = await _table.GetTopRecordAsync(partition);
                if (item != null)
                {
                    await _redis.InsertOrReplaceAsync(item);
                }
            }
            return item;
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            if (!_useLazyLoadInCache)
            {
                return await _redis.GetTopRecordsAsync(partition, n);
            }

            var itemsResult = await _redis.GetTopRecordsAsync(partition, n);
            var items = itemsResult as T[] ?? itemsResult.ToArray();
            if (items.Length == n)
            {
                return items;
            }

            var tableItemsResult = await _table.GetTopRecordsAsync(partition, n);
            var tableItems = tableItemsResult as T[] ?? tableItemsResult.ToArray();

            await _redis.InsertOrReplaceAsync(tableItems);

            return tableItems;
        }

        public Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            if (_useLazyLoadInCache)
            {
                return _table.GetDataByChunksAsync(chunks);
            }

            return _redis.GetDataByChunksAsync(chunks);
        }

        public async Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunks)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(GetDataByChunksAsync), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method GetDataByChunksAsync with TableQuery not supported in radis cache");

            await _table.GetDataByChunksAsync(rangeQuery, chunks);
        }

        public Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            if (_useLazyLoadInCache)
            {
                return _table.GetDataByChunksAsync(chunks);
            }

            return _redis.GetDataByChunksAsync(chunks);
        }

        public async Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(GetDataByChunksAsync), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method GetDataByChunksAsync with TableQuery not supported in radis cache");

            await _table.GetDataByChunksAsync(rangeQuery, chunks);
        }

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            if (_useLazyLoadInCache)
            {
                return _table.GetDataByChunksAsync(partitionKey, chunks);
            }

            return _redis.GetDataByChunksAsync(partitionKey, chunks);
        }

        public Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        {
            if (_useLazyLoadInCache)
            {
                return _table.ScanDataAsync(partitionKey, chunk);
            }

            return _redis.ScanDataAsync(partitionKey, chunk);
        }

        public async Task ScanDataAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunk)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(ScanDataAsync), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method ScanDataAsync with TableQuery not supported in radis cache");

            await _table.ScanDataAsync(rangeQuery, chunk);
        }

        public Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            if (_useLazyLoadInCache)
            {
                return _table.FirstOrNullViaScanAsync(partitionKey, dataToSearch);
            }

            return _redis.FirstOrNullViaScanAsync(partitionKey, dataToSearch);
        }

        public Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            if (_useLazyLoadInCache)
            {
                return _table.GetDataAsync(partition, filter);
            }

            return _redis.GetDataAsync(partition, filter);
        }

        public Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            if (_useLazyLoadInCache)
            {
                return _table.GetDataRowKeysOnlyAsync(rowKeys);
            }

            return _redis.GetDataRowKeysOnlyAsync(rowKeys);
        }

        public async Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(WhereAsyncc), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method WhereAsyncc with TableQuery not supported in radis cache");

            return await _table.WhereAsyncc(rangeQuery, filter);
        }

        public async Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(WhereAsync), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method WhereAsync with TableQuery not supported in radis cache");

            return await _table.WhereAsync(rangeQuery, filter);
        }

        public async Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition)
        {
            await _log.WriteInfoAsync(nameof(RedisCachedAzureTableStorageDecorator<T>), nameof(WhereAsync), $"Table {_table.Name}, Entity {nameof(T)}",
                "Method ExecuteAsync with TableQuery not supported in radis cache");

            await _table.ExecuteAsync(rangeQuery, yieldResult, stopCondition);
        }


        public IEnumerator<T> GetEnumerator()
            => GetDataAsync().Result.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        private async Task LoadCache()
        {
            var data = await _table.GetDataAsync();
            await _redis.DeleteAsync();
            await _redis.InsertAsync(data);
        }
    }
}

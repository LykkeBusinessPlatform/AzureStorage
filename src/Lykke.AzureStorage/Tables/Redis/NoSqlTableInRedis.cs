using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.WindowsAzure.Storage.Table;
using StackExchange.Redis;

namespace AzureStorage.Tables.Redis
{
    public class NoSqlTableInRedis<T> : INoSQLTableStorage<T> where T : class, ITableEntity, new()
    {
        private readonly IDistributedCache _cache;
        private readonly IDatabase _database;
        private readonly ILog _log;
        private readonly DistributedCacheEntryOptions _options;
        private readonly IServer _redisServer;
        private readonly string _offset;

        public NoSqlTableInRedis(
            IDistributedCache cache, 
            IDatabase database, 
            IServer redisServer, 
            INoSqlTableInRedisSettings settings, 
            ILog log)
        {
            _cache = cache;
            _database = database;
            _redisServer = redisServer;
            _offset = settings.TableName;
            _log = log;

            _options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = settings.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = settings.SlidingExpiration
            };
        }

        private async Task<IEnumerable<T>> GetAllData(Func<T, bool> filter = null)
        {
            var pageOffset = 0;
            var listEntity = new List<T>();

            if (filter == null) filter = e => true;

            while (true)
            {
                var keys = _redisServer.Keys(pageOffset: pageOffset, pageSize: 100);
                var newkeys = keys as RedisKey[] ?? keys.ToArray();
                if (!newkeys.Any())
                    break;

                var tasks = newkeys.Select(e => Task.Run(async () =>
                    {
                        var entity = await GetEntityFromCache(e);
                        return entity;
                    })).ToArray();

                await Task.WhenAll(tasks);
                
                listEntity.AddRange(
                    tasks
                        .Select(e => e.Result)
                        .Where(e => e != null)
                        .Where(filter));

                pageOffset++;
            }

            return listEntity;
        }

        private async Task ClearCache()
        {
            while (true)
            {
                var keys = _redisServer.Keys(pattern: $":{_offset}:*", pageSize: 100);
                var arrayKeys = keys as RedisKey[] ?? keys.ToArray();
                if (!arrayKeys.Any())
                    break;

                await _database.KeyDeleteAsync(arrayKeys);
            }
        }

        private string GetCacheKey(T entity)
        {
            return GetCacheKey(entity.PartitionKey, entity.RowKey);
        }

        private string GetCacheKey(string partitionKey, string rowKey)
        {
            return $":{_offset}:{partitionKey}:{rowKey}";
        }

        private async Task<T> GetEntityFromCache(string partitionKey, string rowKey)
        {
            var key = GetCacheKey(partitionKey, rowKey);
            return await GetEntityFromCache(key);
        }

        private async Task<T> GetEntityFromCache(string cacheKey)
        {
            try
            {
                var entityJson = await _cache.GetStringAsync(cacheKey);
                if (string.IsNullOrEmpty(entityJson))
                {
                    return null;
                }

                var entity = entityJson.DeserializeJson<T>();
                return entity;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(NoSqlTableInRedis<T>), nameof(GetEntityFromCache), cacheKey, ex);
                return null;
            }
        }

        private async Task<IList<T>> GetPartition(string partitionKey, int? pieceSize)
        {
            var result = await GetEntityByKeyPattern($":{_offset}:{partitionKey}:*", pieceSize);
            return result;
        }

        private async Task<IList<T>> GetEntityByKeyPattern(string pattern, int? pieceSize)
        {
            var pageOffset = 0;
            var listEntity = new List<T>();

            while (true)
            {
                var keys = _redisServer.Keys(pattern: pattern, pageOffset: pageOffset, pageSize: 100);
                var newkeys = keys as RedisKey[] ?? keys.ToArray();
                if (!newkeys.Any())
                    break;

                var tasks = newkeys.Select(e => Task.Run(async () =>
                {
                    var entity = await GetEntityFromCache(e);
                    return entity;
                })).ToArray();

                var entities = await Task.WhenAll(tasks);

                listEntity.AddRange(entities.Where(e => e != null));

                pageOffset++;

                if (pieceSize.HasValue && listEntity.Count >= pieceSize.Value)
                {
                    listEntity = listEntity.Take(pieceSize.Value).ToList();
                    break;
                }
            }

            return listEntity;
        }


        private async Task SetEntityInCache(T entity)
        {
            await _cache.SetStringAsync(GetCacheKey(entity), entity.ToJson(), _options);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetAllData().Result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Name => $"RedisCache({_offset})";

        T INoSQLTableStorage<T>.this[string partition, string row] => GetEntityFromCache(partition, row).Result;

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => GetPartition(partition, null).Result;

        public async Task InsertAsync(T item, params int[] notLogCodes)
        {
            await SetEntityInCache(item);
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await SetEntityInCache(item);
            }
        }

        public Task InsertOrMergeAsync(T item)
        {
            throw new NotSupportedException("InsertOrMergeAsync not supported on Redis");
        }

        public Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            throw new NotSupportedException("InsertOrMergeBatchAsync not supported on Redis");
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var data = await GetDataAsync(partitionKey, rowKey);
            data = item(data);
            if (data != null)
            {
                await InsertOrReplaceAsync(data);
            }
            return data;
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var data = await GetDataAsync(partitionKey, rowKey);
            data = item(data);
            if (data != null)
            {
                await InsertOrReplaceAsync(data);
            }
            return data;
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            foreach (var item in entities)
            {
                await SetEntityInCache(item);
            }
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await SetEntityInCache(item);
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await SetEntityInCache(item);
            }
        }

        public async Task DeleteAsync(T item)
        {
            await _cache.RemoveAsync(GetCacheKey(item));
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var item = await GetEntityFromCache(partitionKey, rowKey);
            if (item != null) await DeleteAsync(item);
            return item;
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            var item = await DeleteAsync(partitionKey, rowKey);
            return item != null;
        }

        public async Task<bool> DeleteAsync()
        {
            await ClearCache();
            return true;
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await DeleteAsync(item);
            }
        }

        public async Task<bool> CreateIfNotExistsAsync(T item)
        {
            var isExisted = await _database.KeyExistsAsync(GetCacheKey(item));
            if (!isExisted)
            {
                await SetEntityInCache(item);
                return true;
            }
            return false;
        }

        public bool RecordExists(T item)
        {
            return _database.KeyExistsAsync(GetCacheKey(item)).Result;
        }

        public Task<bool> RecordExistsAsync(T item)
        {
            return _database.KeyExistsAsync(GetCacheKey(item));
        }

        public Task<T> GetDataAsync(string partition, string row)
        {
            var entity = GetEntityFromCache(partition, row);
            return entity;
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var list = await GetAllData(filter);
            return list as IList<T> ?? list.ToList();
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var tasks = rowKeys.Select(row => Task.Run(async () => await GetEntityFromCache(partitionKey, row))).ToArray();
            await Task.WhenAll(tasks);
            var result = tasks.Select(e => e.Result).Where(e => e != null).ToArray();
            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var result = new List<T>();
            if (filter == null)
                filter = e => true;

            foreach (var partitionKey in partitionKeys)
            {
                var data = await GetPartition(partitionKey, pieceSize);
                result.AddRange(data.Where(filter));

                if (result.Count >= pieceSize)
                {
                    return result.Take(pieceSize);
                }
            }
            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var result = new List<T>();

            foreach (var key in keys)
            {
                var data = await GetEntityFromCache(key.Item1, key.Item2);
                if (filter == null || filter(data))
                {
                    result.Add(data);
                }

                if (result.Count >= pieceSize)
                {
                    return result;
                }
            }

            return result;
        }

        public async Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            var items = await GetDataAsync();
            await chunks(items);
        }

        public Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunks)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public async Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            var items = await GetDataAsync();
            chunks(items);
        }

        public Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public async Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            var items = await GetDataAsync(partitionKey);
            chunks(items);
        }

        public async Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        {
            var items = await GetDataAsync(partitionKey);
            await chunk(items);
        }

        public Task ScanDataAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunk)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            var items = await GetDataAsync(partitionKey);
            return dataToSearch(items);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            var data = await GetPartition(partition, null);
            if (filter != null)
            {
                return data.Where(filter);
            }
            return data;
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            var data = await GetPartition(partition, 1);
            return data.FirstOrDefault();
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            var data = await GetPartition(partition, n);
            return data;
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            var result = new List<T>();

            foreach (var key in rowKeys)
            {
                var data = await GetEntityByKeyPattern($":{_offset}:*:{key}", null);
                result.AddRange(data);
            }

            return result;
        }

        public Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition = null)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public Task DoBatchAsync(TableBatchOperation batch)
        {
            throw new NotSupportedException("TableBatchOperation not supported in NoSqlTableInRedis");
        }

        public Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo)
        {
            throw new NotSupportedException("Find by TableQuery not supported in NoSqlTableInRedis");
        }

        public Task CreateTableIfNotExistsAsync()
        {
            return Task.CompletedTask;
        }
    }
}

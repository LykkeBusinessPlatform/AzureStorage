using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Tables.Redis;
using Common.Log;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.WindowsAzure.Storage.Table;
using StackExchange.Redis;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Cached in Redis <see cref="INoSQLTableStorage{T}"/> decorator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class RedisCachedAzureTableStorageDecorator<T> : INoSQLTableStorage<T>
        where T : class, ITableEntity, new()
    {
        private readonly INoSQLTableStorage<T> _storage;
        private readonly INoSQLTableStorage<T> _cache;
        private readonly ILog _log;

        private bool _cacheOutOfDate;

        public IEnumerable<T> this[string partition]
        {
            get
            {
                var cacheResult = TryGetFromCacheAsync(() => _cache.GetDataAsync(partition)).GetAwaiter().GetResult();

                return cacheResult.Item1
                    ? cacheResult.Item2
                    : _storage.GetDataAsync(partition).GetAwaiter().GetResult();
            }
        }

        public T this[string partition, string row]
        {
            get
            {
                var cacheResult = TryGetFromCacheAsync(() => _cache.GetDataAsync(partition, row)).GetAwaiter()
                    .GetResult();

                return cacheResult.Item1
                    ? cacheResult.Item2
                    : _storage.GetDataAsync(partition, row).GetAwaiter().GetResult();
            }
        }

        public string Name => _storage.Name;

        public RedisCachedAzureTableStorageDecorator(
            INoSQLTableStorage<T> storage,
            IDistributedCache cache,
            IDatabase database,
            IServer redisServer,
            IAzureRedisSettings settings,
            string tableKeysOffset,
            ILog log)
        {
            _storage = storage;
            var redisSettings = new NoSqlTableInRedisSettings
            {
                AbsoluteExpirationRelativeToNow = settings.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = settings.SlidingExpiration,
                TableName = tableKeysOffset
            };
            _cache = new RetryOnFailureAzureTableStorageDecorator<T>(
                new NoSqlTableInRedis<T>(cache, database, redisServer, redisSettings, log));
            _log = log;

            _cacheOutOfDate = true;
        }

        public async Task<bool> CreateIfNotExistsAsync(T item)
        {
            var result = await _storage.CreateIfNotExistsAsync(item);

            if (!result) return false;

            await TrySyncWithCacheAsync(() => _cache.CreateIfNotExistsAsync(item));

            return true;
        }

        public Task CreateTableIfNotExistsAsync()
        {
            return _storage.CreateTableIfNotExistsAsync();
        }

        public async Task DeleteAsync(T item)
        {
            await _storage.DeleteAsync(item);

            await TrySyncWithCacheAsync(() => _cache.DeleteAsync(item));
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var deletedItem = await _storage.DeleteAsync(partitionKey, rowKey);

            await TrySyncWithCacheAsync(() => _cache.DeleteAsync(partitionKey, rowKey));

            return deletedItem;
        }

        public async Task<bool> DeleteAsync()
        {
            var result = await _storage.DeleteAsync();

            if (!result) return false;

            await TrySyncWithCacheAsync(() => _cache.DeleteAsync());

            return true;
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.DeleteAsync(list);

            await TrySyncWithCacheAsync(() => _cache.DeleteAsync(list));
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            var result = await _storage.DeleteIfExistAsync(partitionKey, rowKey);

            if (!result) return false;

            await TrySyncWithCacheAsync(() => _cache.DeleteIfExistAsync(partitionKey, rowKey));

            return true;
        }

        public async Task DoBatchAsync(TableBatchOperation batch)
        {
            await _storage.DoBatchAsync(batch);

            await TrySyncWithCacheAsync(() => _cache.DoBatchAsync(batch));
        }

        public async Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition = null)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.ExecuteAsync(rangeQuery, yieldResult, stopCondition));

            if (!succedeed)
            {
                await _storage.ExecuteAsync(rangeQuery, yieldResult, stopCondition);
            }
        }

        public async Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.ExecuteQueryWithPaginationAsync(query, pagingInfo));

            return cacheResult.Item1
                ? cacheResult.Item2
                : await _storage.ExecuteQueryWithPaginationAsync(query, pagingInfo);
        }

        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            var cacheResult =
                await TryGetFromCacheAsync(() => _cache.FirstOrNullViaScanAsync(partitionKey, dataToSearch));

            return cacheResult.Item1
                ? cacheResult.Item2
                : await _storage.FirstOrNullViaScanAsync(partitionKey, dataToSearch);
        }

        public async Task<T> GetDataAsync(string partition, string row)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataAsync(partition, row));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetDataAsync(partition, row);
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataAsync(filter));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetDataAsync(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataAsync(partitionKey, rowKeys, pieceSize, filter));

            return cacheResult.Item1
                ? cacheResult.Item2
                : await _storage.GetDataAsync(partitionKey, rowKeys, pieceSize, filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataAsync(partitionKeys, pieceSize, filter));

            return cacheResult.Item1
                ? cacheResult.Item2
                : await _storage.GetDataAsync(partitionKeys, pieceSize, filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataAsync(keys, pieceSize, filter));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetDataAsync(keys, pieceSize, filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataAsync(partition, filter));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetDataAsync(partition, filter);
        }

        public async Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.GetDataByChunksAsync(chunks));

            if (!succedeed)
            {
                await _storage.GetDataByChunksAsync(chunks);
            }
        }

        public async Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunks)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.GetDataByChunksAsync(rangeQuery, chunks));

            if (!succedeed)
            {
                await _storage.GetDataByChunksAsync(rangeQuery, chunks);
            }
        }

        public async Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.GetDataByChunksAsync(chunks));

            if (!succedeed)
            {
                await _storage.GetDataByChunksAsync(chunks);
            }
        }

        public async Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.GetDataByChunksAsync(rangeQuery, chunks));

            if (!succedeed)
            {
                await _storage.GetDataByChunksAsync(rangeQuery, chunks);
            }
        }

        public async Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.GetDataByChunksAsync(partitionKey, chunks));

            if (!succedeed)
            {
                await _storage.GetDataByChunksAsync(partitionKey, chunks);
            }
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetDataRowKeysOnlyAsync(rowKeys));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetDataRowKeysOnlyAsync(rowKeys);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetDataAsync().Result.GetEnumerator();
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetTopRecordAsync(partition));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetTopRecordAsync(partition);
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.GetTopRecordsAsync(partition, n));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.GetTopRecordsAsync(partition, n);
        }

        public async Task InsertAsync(T item, params int[] notLogCodes)
        {
            await _storage.InsertAsync(item, notLogCodes);

            await TrySyncWithCacheAsync(() => _cache.InsertAsync(item, notLogCodes));
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.InsertAsync(list);

            await TrySyncWithCacheAsync(() => _cache.InsertAsync(list));
        }

        public async Task InsertOrMergeAsync(T item)
        {
            await _storage.InsertOrMergeAsync(item);

            await TrySyncWithCacheAsync(() => _cache.InsertOrMergeAsync(item));
        }

        public async Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.InsertOrMergeBatchAsync(list);

            await TrySyncWithCacheAsync(() => _cache.InsertOrMergeBatchAsync(list));
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await _storage.InsertOrReplaceAsync(item);

            await TrySyncWithCacheAsync(() => _cache.InsertOrReplaceAsync(item));
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.InsertOrReplaceAsync(list);

            await TrySyncWithCacheAsync(() => _cache.InsertOrReplaceAsync(list));
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            var list = entities.ToList();

            await _storage.InsertOrReplaceBatchAsync(list);

            await TrySyncWithCacheAsync(() => _cache.InsertOrReplaceBatchAsync(list));
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _storage.MergeAsync(partitionKey, rowKey, item);

            if (result != null)
            {
                await TrySyncWithCacheAsync(() => _cache.MergeAsync(partitionKey, rowKey, item));
            }

            return result;
        }

        public bool RecordExists(T item)
        {
            var cacheResult = TryGetFromCache(() => _cache.RecordExists(item));

            return cacheResult.Item1 ? cacheResult.Item2 : _storage.RecordExists(item);
        }

        public async Task<bool> RecordExistsAsync(T item)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.RecordExistsAsync(item));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.RecordExistsAsync(item);
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _storage.ReplaceAsync(partitionKey, rowKey, item);

            if (result != null)
            {
                await TrySyncWithCacheAsync(() => _cache.ReplaceAsync(partitionKey, rowKey, item));
            }

            return result;
        }

        public async Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.ScanDataAsync(partitionKey, chunk));

            if (!succedeed)
            {
                await _storage.ScanDataAsync(partitionKey, chunk);
            }
        }

        public async Task ScanDataAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunk)
        {
            var succedeed = await TryGetFromCacheAsync(() => _cache.ScanDataAsync(rangeQuery, chunk));

            if (!succedeed)
            {
                await _storage.ScanDataAsync(rangeQuery, chunk);
            }
        }

        public async Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.WhereAsync(rangeQuery, filter));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.WhereAsync(rangeQuery, filter);
        }

        public async Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            var cacheResult = await TryGetFromCacheAsync(() => _cache.WhereAsyncc(rangeQuery, filter));

            return cacheResult.Item1 ? cacheResult.Item2 : await _storage.WhereAsyncc(rangeQuery, filter);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private async Task<bool> RefreshCache()
        {
            var oldCacheDeleted = await _cache.DeleteAsync();
            if (!oldCacheDeleted)
            {
                return false;
            }

            var records = await _storage.GetDataAsync();

            await _cache.InsertAsync(records);

            return true;
        }

        private async Task TrySyncWithCacheAsync(Func<Task> action)
        {
            try
            {
                if (_cacheOutOfDate)
                {
                    _cacheOutOfDate = !await RefreshCache();
                }
                else
                {
                    await action();
                }
            }
            catch (Exception)
            {
                _cacheOutOfDate = true;
            }
        }

        private async Task<Tuple<bool, TResult>> TryGetFromCacheAsync<TResult>(Func<Task<TResult>> getFunc)
        {
            var failedResult = new Tuple<bool, TResult>(false, default(TResult));

            if (_cacheOutOfDate)
            {
                if (!await RefreshCache())
                {
                    return failedResult;
                }

                _cacheOutOfDate = false;
            }

            try
            {
                var data = await getFunc();

                return new Tuple<bool, TResult>(true, data);
            }
            catch (Exception)
            {
                return failedResult;
            }
        }

        private async Task<bool> TryGetFromCacheAsync(Func<Task> getFunc)
        {
            if (_cacheOutOfDate)
            {
                if (!await RefreshCache())
                {
                    return false;
                }

                _cacheOutOfDate = false;
            }

            try
            {
                await getFunc();
            }
            catch (Exception)
            {
                return false;
            }

            return true;
        }

        private Tuple<bool, TResult> TryGetFromCache<TResult>(Func<TResult> getFunc)
        {
            var failedResult = new Tuple<bool, TResult>(false, default(TResult));

            if (_cacheOutOfDate)
            {
                if (!RefreshCache().GetAwaiter().GetResult())
                {
                    return failedResult;
                }

                _cacheOutOfDate = false;
            }

            try
            {
                var data = getFunc();

                return new Tuple<bool, TResult>(true, data);
            }
            catch (Exception)
            {
                return failedResult;
            }
        }
    }
}

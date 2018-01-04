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
        #region fields

        private readonly IDistributedCache _cache;
        private readonly IDatabase _database;
        private readonly ILog _log;
        private readonly DistributedCacheEntryOptions _options;
        private readonly IServer _redisServer;
        private readonly string _offset;

        #endregion

        #region consts

        private const int _cachePageSize = 100;
        private const int _dataRetrievalPieceSize = 100;

        #endregion

        private static class Patterns
        {
            public static string GetAll(string offset)
            {
                return $":{offset}:*";
            }

            public static string GetPartition(string offset, string partitionKey)
            {
                return $":{offset}:{partitionKey}:*";
            }

            public static string GetKey(string offset, string partitionKey, string rowKey)
            {
                return $":{offset}:{partitionKey}:{rowKey}";
            }
        }

        public NoSqlTableInRedis(
            IDistributedCache cache, 
            IDatabase database, 
            IServer redisServer, 
            INoSqlTableInRedisSettings settings, 
            ILog log)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _redisServer = redisServer ?? throw new ArgumentNullException(nameof(redisServer));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _offset = settings?.TableName;

            if (string.IsNullOrWhiteSpace(_offset))
                throw new ArgumentNullException(nameof(settings.TableName));

            _options = new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = settings.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = settings.SlidingExpiration
            };
        }

        private IEnumerable<RedisKey> GetAllKeys()
        {
            var keys = new List<RedisKey>();

            var pageOffset = 0;

            while (true)
            {
                var batchKeys = _redisServer.Keys(pattern: Patterns.GetAll(_offset), pageOffset: pageOffset, pageSize: _cachePageSize);

                if (!batchKeys.Any()) break;

                keys.AddRange(batchKeys);

                pageOffset++;
            }

            return keys;
        }

        private async Task<IEnumerable<T>> GetAllData(Func<T, bool> filter = null)
        {
            if (filter == null) filter = e => true;

            var batch = _database.CreateBatch();
            var task = batch.StringGetAsync(GetAllKeys().ToArray());
            batch.Execute();
            var values = await task;

            return (await GetEntities(values)).Where(filter);
        }

        private async Task ClearCache()
        {
            var batch = _database.CreateBatch();

            var task = batch.KeyDeleteAsync(GetAllKeys().ToArray());

            batch.Execute();

            await task;
        }

        private string GetCacheKey(T entity)
        {
            return GetCacheKey(entity.PartitionKey, entity.RowKey);
        }

        private string GetCacheKey(string partitionKey, string rowKey)
        {
            return Patterns.GetKey(_offset, partitionKey, rowKey);
        }

        private async Task<T> GetEntityFromCache(string partitionKey, string rowKey)
        {
            var key = Patterns.GetKey(_offset, partitionKey, rowKey);
            return await GetEntityFromCache(key);
        }

        private async Task<T> GetEntityFromCache(string cacheKey)
        {
            var entityJson = await _cache.GetStringAsync(cacheKey);

            return await GetEntity(entityJson);
        }

        private async Task<IEnumerable<T>> GetEntities(IEnumerable<RedisValue> values)
        {
            var tasks = values.Select(x => GetEntity(x));
            var entities = await Task.WhenAll(tasks);

            return entities.Where(x => x != null);
        }

        private async Task<T> GetEntity(RedisValue value)
        {
            var entityJson = (string) value;

            try
            {
                if (string.IsNullOrEmpty(entityJson))
                {
                    return null;
                }

                var entity = entityJson.DeserializeJson<T>();
                return entity;
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(NoSqlTableInRedis<T>), nameof(GetEntity), value, ex);
                return null;
            }
        }

        private async Task<IList<T>> GetPartition(string partitionKey)
        {
            return await GetEntityByKeyPattern(Patterns.GetPartition(_offset, partitionKey));
        }

        private async Task<IList<T>> GetEntityByKeyPattern(string pattern)
        {
            var pageOffset = 0;
            var listEntity = new List<T>();

            while (true)
            {
                var batchKeys = _redisServer.Keys(pattern: pattern, pageOffset: pageOffset, pageSize: _cachePageSize);
                var newkeys = batchKeys as RedisKey[] ?? batchKeys.ToArray();
                if (!newkeys.Any())
                    break;

                var batch = _database.CreateBatch();
                var task = batch.StringGetAsync(newkeys);
                batch.Execute();
                var batchValues = await task;

                listEntity.AddRange(await GetEntities(batchValues));

                pageOffset++;
            }

            return listEntity;
        }

        private async Task SetEntityInCache(T entity)
        {
            await _cache.SetStringAsync(GetCacheKey(entity), entity.ToJson(), _options);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return GetAllData().GetAwaiter().GetResult().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Name => $"RedisCache({_offset})";

        T INoSQLTableStorage<T>.this[string partition, string row] => GetEntityFromCache(partition, row).GetAwaiter().GetResult();

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => GetPartition(partition).GetAwaiter().GetResult();

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
            throw new NotSupportedException($"{nameof(InsertOrMergeAsync)} is not supported on Redis");
        }

        public Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            throw new NotSupportedException($"{nameof(InsertOrMergeBatchAsync)} is not supported on Redis");
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var existing = await GetDataAsync(partitionKey, rowKey);
            var newValue = item(existing);
            if (newValue != null)
            {
                await InsertOrReplaceAsync(newValue);
            }

            return newValue;
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            return await ReplaceAsync(partitionKey, rowKey, item);
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            foreach (var item in entities)
            {
                await InsertOrReplaceAsync(item);
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
            if (item != null)
                await DeleteAsync(item);
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
            var keys = items.Select(x => GetCacheKey(x));

            var batch = _database.CreateBatch();
            var task = batch.KeyDeleteAsync(keys.Cast<RedisKey>().ToArray());
            batch.Execute();

            await task;
        }

        public async Task<bool> CreateIfNotExistsAsync(T item)
        {
            var exists = await _database.KeyExistsAsync(GetCacheKey(item));
            if (!exists)
            {
                await SetEntityInCache(item);
                return true;
            }
            return false;
        }

        public bool RecordExists(T item)
        {
            var key = GetCacheKey(item);

            return _database.KeyExistsAsync(key).GetAwaiter().GetResult();
        }

        public Task<bool> RecordExistsAsync(T item)
        {
            var key = GetCacheKey(item);

            return _database.KeyExistsAsync(key);
        }

        public Task<T> GetDataAsync(string partition, string row)
        {
            return GetEntityFromCache(partition, row);
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var list = await GetAllData(filter);
            return list as IList<T> ?? list.ToList();
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            if (filter == null)
                filter = e => true;

            var batch = _database.CreateBatch();
            var task = batch.StringGetAsync(rowKeys
                .Select(x => GetCacheKey(partitionKey, x))
                .Cast<RedisKey>()
                .ToArray());
            batch.Execute();
            var values = await task;

            return (await GetEntities(values)).Where(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var result = new List<T>();
            if (filter == null)
                filter = e => true;

            foreach (var partitionKey in partitionKeys)
            {
                var data = await GetPartition(partitionKey);
                result.AddRange(data.Where(filter));
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
            throw new NotSupportedException($"Searching by TableQuery is not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public async Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            var items = await GetDataAsync();
            chunks(items);
        }

        public Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks)
        {
            throw new NotSupportedException($"Searching by TableQuery is not supported in {nameof(NoSqlTableInRedis<T>)}");
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
            throw new NotSupportedException($"Scan by TableQuery is not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            var items = await GetDataAsync(partitionKey);
            return dataToSearch(items);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            var data = await GetPartition(partition);
            if (filter != null)
            {
                return data.Where(filter);
            }
            return data;
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            throw new NotSupportedException($"{nameof(GetTopRecordAsync)} not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            throw new NotSupportedException($"{nameof(GetTopRecordAsync)} not supported in {nameof(NoSqlTableInRedis<T>)}"); ;
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            var patternKeys = rowKeys.Select(x => GetCacheKey("*", x));

            var list = new List<T>();
            foreach (var patternKey in patternKeys)
            {
                var patternEntities = await GetEntityByKeyPattern(patternKey);
                list.AddRange(patternEntities);
            }

            return list;
        }

        public Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            throw new NotSupportedException($"Searching by TableQuery not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            throw new NotSupportedException($"Searching by TableQuery not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition = null)
        {
            throw new NotSupportedException($"Searching by TableQuery not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public Task DoBatchAsync(TableBatchOperation batch)
        {
            throw new NotSupportedException($"TableBatchOperation not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo)
        {
            throw new NotSupportedException($"Searching by TableQuery not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public Task CreateTableIfNotExistsAsync()
        {
            return Task.CompletedTask;
        }
    }
}

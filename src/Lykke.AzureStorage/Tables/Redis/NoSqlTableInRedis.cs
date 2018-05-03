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
        private const int CachePageSize = 100;

        private readonly IDistributedCache _cache;
        private readonly IDatabase _database;
        private readonly ILog _log;
        private readonly DistributedCacheEntryOptions _options;
        private readonly IServer _redisServer;
        private readonly string _tableName;

        private static class Patterns
        {
            public static string GetAll(string tableName)
            {
                return $":{tableName}:*";
            }

            public static string GetPartition(string tableName, string partitionKey)
            {
                return $":{tableName}:{partitionKey}:*";
            }

            public static string GetKey(string tableName, string partitionKey, string rowKey)
            {
                return $":{tableName}:{partitionKey}:{rowKey}";
            }
        }

        public NoSqlTableInRedis(
            IDistributedCache cache,
            IDatabase database,
            IServer redisServer,
            NoSqlTableInRedisSettings settings,
            ILog log)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _redisServer = redisServer ?? throw new ArgumentNullException(nameof(redisServer));
            _log = log.CreateComponentScope(nameof(NoSqlTableInRedis<T>));
            _tableName = settings?.TableName;

            if (string.IsNullOrWhiteSpace(_tableName))
                throw new ArgumentNullException(nameof(settings.TableName));

            _options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = settings?.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = settings?.SlidingExpiration
            };
        }

        private IEnumerable<RedisKey> GetAllKeys()
        {
            var keys = new List<RedisKey>();

            var pageOffset = 0;

            while (true)
            {
                var batchKeys = _redisServer.Keys(pattern: Patterns.GetAll(_tableName), pageOffset: pageOffset, pageSize: CachePageSize).ToList();

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

            return DeserializeMany(values).Where(filter);
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
            return Patterns.GetKey(_tableName, partitionKey, rowKey);
        }

        private async Task<T> GetAsync(string partitionKey, string rowKey)
        {
            var key = GetCacheKey(partitionKey, rowKey);
            var entityJson = await _cache.GetStringAsync(key);
            return Deserialize(entityJson);
        }

        private IEnumerable<T> DeserializeMany(IEnumerable<RedisValue> values)
        {
            return values.Select(Deserialize);
        }

        private T Deserialize(RedisValue value)
        {
            var entityJson = (string)value;

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
                _log.WriteError("Deserialize", value, ex); // todo: warning?
                return null;
            }
        }

        private async Task<IList<T>> GetPartition(string partitionKey)
        {
            return await GetEntitiesByKeyPattern(Patterns.GetPartition(_tableName, partitionKey));
        }

        private async Task<IList<T>> GetEntitiesByKeyPattern(string pattern)
        {
            var pageOffset = 0;
            var listEntity = new List<T>();

            while (true)
            {
                var batchKeys = _redisServer.Keys(pattern: pattern, pageOffset: pageOffset, pageSize: CachePageSize);
                var newkeys = batchKeys as RedisKey[] ?? batchKeys.ToArray();
                if (!newkeys.Any())
                    break;

                var batch = _database.CreateBatch();
                var task = batch.StringGetAsync(newkeys);
                batch.Execute();
                var batchValues = await task;

                listEntity.AddRange(DeserializeMany(batchValues));

                pageOffset++;
            }

            return listEntity;
        }

        private async Task SaveAsync(T entity)
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

        public string Name => $"RedisCache({_tableName})";

        T INoSQLTableStorage<T>.this[string partition, string row] => GetAsync(partition, row).GetAwaiter().GetResult();

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => GetPartition(partition).GetAwaiter().GetResult();

        public Task InsertAsync(T item, params int[] notLogCodes)
        {
            return SaveAsync(item);
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await InsertAsync(item);
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

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> replaceAction)
        {
            var existing = await GetDataAsync(partitionKey, rowKey);
            var newValue = replaceAction(existing);
            if (newValue != null)
            {
                await InsertOrReplaceAsync(newValue);
            }

            return newValue;
        }

        public Task ReplaceAsync(T entity)
        {
            return InsertAsync(entity);
        }

        public Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            return ReplaceAsync(partitionKey, rowKey, item);
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            foreach (var item in entities)
            {
                await InsertOrReplaceAsync(item);
            }
        }

        public Task InsertOrReplaceAsync(T item)
        {
            return SaveAsync(item);
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await SaveAsync(item);
            }
        }

        public Task<bool> InsertOrReplaceAsync(T entity, Func<T, bool> replaceCondition)
        {
            throw new NotImplementedException();
        }

        public Task<bool> InsertOrModifyAsync(string partitionKey, string rowKey, Func<T> create, Func<T, bool> modify)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(T item)
        {
            return _cache.RemoveAsync(GetCacheKey(item));
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var item = await GetAsync(partitionKey, rowKey);
            if (item != null)
                await DeleteAsync(item);
            return item;
        }

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            return DeleteIfExistAsync(partitionKey, rowKey, _ => true);
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey, Func<T, bool> deleteCondition)
        {
            var item = await GetAsync(partitionKey, rowKey);
            if (item != null && deleteCondition(item))
            {
                await DeleteAsync(item);
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync()
        {
            await ClearCache();
            return true;
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            var keys = items.Select(GetCacheKey);

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
                await SaveAsync(item);
                return true;
            }
            return false;
        }

        public bool RecordExists(T item)
        {
            return _database.KeyExists(GetCacheKey(item));
        }

        public Task<bool> RecordExistsAsync(T item)
        {
            return _database.KeyExistsAsync(GetCacheKey(item));
        }

        public Task<T> GetDataAsync(string partition, string row)
        {
            return GetAsync(partition, row);
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var list = await GetAllData(filter);
            return list as IList<T> ?? list.ToList();
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

        public Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            return GetDataAsync(rowKeys.Select(key => new Tuple<string, string>(partitionKey, key)), pieceSize, filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            if (filter == null)
                filter = e => true;

            var batch = _database.CreateBatch();
            var task = batch.StringGetAsync(keys
                .Select(key => GetCacheKey(key.Item1, key.Item2))
                .Cast<RedisKey>()
                .ToArray());
            batch.Execute();
            var values = await task;

            return DeserializeMany(values).Where(filter);
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

        public Task<(IEnumerable<T> Entities, string ContinuationToken)> GetDataWithContinuationTokenAsync(TableQuery<T> rangeQuery, string continuationToken)
        {
            throw new NotImplementedException();
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
            throw new NotSupportedException($"{nameof(GetTopRecordAsync)} not supported in {nameof(NoSqlTableInRedis<T>)}");
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            var patternKeys = rowKeys.Select(x => GetCacheKey("*", x));

            var list = new List<T>();
            foreach (var patternKey in patternKeys)
            {
                var patternEntities = await GetEntitiesByKeyPattern(patternKey);
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

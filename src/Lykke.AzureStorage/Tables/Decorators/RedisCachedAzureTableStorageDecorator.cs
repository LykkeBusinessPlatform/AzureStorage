using System;
using System.Collections;
using System.Collections.Generic;
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

        public RedisCachedAzureTableStorageDecorator(
            INoSQLTableStorage<T> primaryStorage,
            IDistributedCache redisCache,
            IDatabase redisDatabase,
            IServer redisServer,
            IAzureRedisSettings settings,
            string tableKeysOffset,
            ILog log)
        {
            var redisSettings = new NoSqlTableInRedisSettings
            {
                AbsoluteExpirationRelativeToNow = settings?.AbsoluteExpirationRelativeToNow,
                SlidingExpiration = settings?.SlidingExpiration,
                TableName = tableKeysOffset
            };

            var redisStorage = new RetryOnFailureAzureTableStorageDecorator<T>(
                new NoSqlTableInRedis<T>(
                    redisCache,
                    redisDatabase,
                    redisServer,
                    redisSettings,
                    log));

            _storage = new CachedAzureTableStorageDecorator<T>(primaryStorage, redisStorage, log);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _storage.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string Name => _storage.Name;

        T INoSQLTableStorage<T>.this[string partition, string row] => _storage[partition, row];

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => _storage[partition];

        public async Task InsertAsync(T item, params int[] notLogCodes)
        {
            await _storage.InsertAsync(item, notLogCodes);
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            await _storage.InsertAsync(items);
        }

        public async Task InsertOrMergeAsync(T item)
        {
            await _storage.InsertOrMergeAsync(item);
        }

        public async Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            await _storage.InsertOrMergeBatchAsync(items);
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            return await _storage.ReplaceAsync(partitionKey, rowKey, item);
        }

        public Task ReplaceAsync(T entity)
        {
            return _storage.ReplaceAsync(entity);
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            return await _storage.MergeAsync(partitionKey, rowKey, item);
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            await _storage.InsertOrReplaceBatchAsync(entities);
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await _storage.InsertOrReplaceAsync(item);
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            await _storage.InsertOrReplaceAsync(items);
        }

        public Task<bool> InsertOrReplaceAsync(T entity, Func<T, bool> replaceCondition)
        {
            return _storage.InsertOrReplaceAsync(entity, replaceCondition);
        }

        public Task<bool> InsertOrModifyAsync(string partitionKey, string rowKey, Func<T> create, Func<T, bool> modify)
        {
            return _storage.InsertOrModifyAsync(partitionKey, rowKey, create, modify);
        }

        public async Task DeleteAsync(T item)
        {
            await _storage.DeleteAsync(item);
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            return await _storage.DeleteAsync(partitionKey, rowKey);
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            return await _storage.DeleteIfExistAsync(partitionKey, rowKey);
        }

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey, Func<T, bool> deleteCondition)
        {
            return _storage.DeleteIfExistAsync(partitionKey, rowKey, deleteCondition);
        }

        public async Task<bool> DeleteAsync()
        {
            return await _storage.DeleteAsync();
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            await _storage.DeleteAsync(items);
        }

        public async Task<bool> CreateIfNotExistsAsync(T item)
        {
            return await _storage.CreateIfNotExistsAsync(item);
        }

        public bool RecordExists(T item)
        {
            return _storage.RecordExists(item);
        }

        public async Task<bool> RecordExistsAsync(T item)
        {
            return await _storage.RecordExistsAsync(item);
        }

        public async Task<T> GetDataAsync(string partition, string row)
        {
            return await _storage.GetDataAsync(partition, row);
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            return await _storage.GetDataAsync(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            return await _storage.GetDataAsync(partitionKey, rowKeys, pieceSize, filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            return await _storage.GetDataAsync(partitionKeys, pieceSize, filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            return await _storage.GetDataAsync(keys, pieceSize, filter);
        }

        public async Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            await _storage.GetDataByChunksAsync(chunks);
        }

        public async Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunks)
        {
            await _storage.GetDataByChunksAsync(rangeQuery, chunks);
        }

        public async Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            await _storage.GetDataByChunksAsync(chunks);
        }

        public async Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks)
        {
            await _storage.GetDataByChunksAsync(rangeQuery, chunks);
        }

        public async Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            await _storage.GetDataByChunksAsync(partitionKey, chunks);
        }

        public Task<(IEnumerable<T> Entities, string ContinuationToken)> GetDataWithContinuationTokenAsync(TableQuery<T> rangeQuery, string continuationToken)
        {
            return _storage.GetDataWithContinuationTokenAsync(rangeQuery, continuationToken);
        }

        public async Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        {
            await _storage.ScanDataAsync(partitionKey, chunk);
        }

        public async Task ScanDataAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunk)
        {
            await _storage.ScanDataAsync(rangeQuery, chunk);
        }

        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            return await _storage.FirstOrNullViaScanAsync(partitionKey, dataToSearch);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            return await _storage.GetDataAsync(partition, filter);
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            return await _storage.GetTopRecordAsync(partition);
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            return await _storage.GetTopRecordsAsync(partition, n);
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            return await _storage.GetDataRowKeysOnlyAsync(rowKeys);
        }

        public async Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            return await _storage.WhereAsyncc(rangeQuery, filter);
        }

        public async Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            return await _storage.WhereAsync(rangeQuery, filter);
        }

        public async Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition = null)
        {
            await _storage.ExecuteAsync(rangeQuery, yieldResult, stopCondition);
        }

        public async Task DoBatchAsync(TableBatchOperation batch)
        {
            await _storage.DoBatchAsync(batch);
        }

        public async Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo)
        {
            return await _storage.ExecuteQueryWithPaginationAsync(query, pagingInfo);
        }

        public async Task CreateTableIfNotExistsAsync()
        {
            await _storage.CreateTableIfNotExistsAsync();
        }
    }
}

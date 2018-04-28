﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Cached in any INoSQLTableStorage <see cref="INoSQLTableStorage{T}"/> decorator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CachedAzureTableStorageDecorator<T> : INoSQLTableStorage<T>
        where T : class, ITableEntity, new()
    {
        private readonly INoSQLTableStorage<T> _storage;
        private readonly INoSQLTableStorage<T> _cache;
        private readonly ILog _log;

        private bool _cacheOutOfDate;

        public CachedAzureTableStorageDecorator(
            INoSQLTableStorage<T> storage,
            INoSQLTableStorage<T> cache,
            ILog log)
        {
            _storage = storage;
            _cache = cache;
            _log = log;

            _cacheOutOfDate = true;
        }

        public string Name => _storage.Name;

        public IEnumerable<T> this[string partition]
        {
            get
            {
                var cacheResult = TryGetFromCache(() => _cache[partition]);

                return cacheResult.Item1
                    ? cacheResult.Item2
                    : _storage[partition];
            }
        }

        public T this[string partition, string row]
        {
            get
            {
                var cacheResult = TryGetFromCache(() => _cache[partition, row]);

                return cacheResult.Item1
                    ? cacheResult.Item2
                    : _storage[partition, row];
            }
        }

        public async Task<bool> CreateIfNotExistsAsync(T item)
        {
            var result = await _storage.CreateIfNotExistsAsync(item);

            if (!result) return false;

            await TryActionOrRefreshCacheAsync(() => _cache.CreateIfNotExistsAsync(item));

            return true;
        }

        public Task CreateTableIfNotExistsAsync()
        {
            return _storage.CreateTableIfNotExistsAsync();
            // todo: _cache.CreateTableIfNotExistsAsync(); ?
        }

        public async Task<bool> InsertOrModifyAsync(string partitionKey, string rowKey, Func<T> create, Func<T, bool> modify)
        {
            var result = await _storage.InsertOrModifyAsync(partitionKey, rowKey, create, modify);

            if (!result) return false;

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrModifyAsync(partitionKey, rowKey, create, modify));

            return true;
        }

        public async Task DeleteAsync(T item)
        {
            await _storage.DeleteAsync(item);

            await TryActionOrRefreshCacheAsync(() => _cache.DeleteAsync(item));
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var deletedItem = await _storage.DeleteAsync(partitionKey, rowKey);

            await TryActionOrRefreshCacheAsync(() => _cache.DeleteAsync(partitionKey, rowKey));

            return deletedItem;
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey, Func<T, bool> deleteCondition)
        {
            var result = await _storage.DeleteIfExistAsync(partitionKey, rowKey, deleteCondition);

            if (!result) return false;

            await TryActionOrRefreshCacheAsync(() => _cache.DeleteIfExistAsync(partitionKey, rowKey, deleteCondition));

            return true;
        }

        public async Task<bool> DeleteAsync()
        {
            var result = await _storage.DeleteAsync();

            if (!result) return false;

            await TryActionOrRefreshCacheAsync(() => _cache.DeleteAsync());

            return true;
        }

        public async Task DeleteAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.DeleteAsync(list);

            await TryActionOrRefreshCacheAsync(() => _cache.DeleteAsync(list));
        }

        public async Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            var result = await _storage.DeleteIfExistAsync(partitionKey, rowKey);

            if (!result) return false;

            await TryActionOrRefreshCacheAsync(() => _cache.DeleteIfExistAsync(partitionKey, rowKey));

            return true;
        }

        public async Task DoBatchAsync(TableBatchOperation batch)
        {
            await _storage.DoBatchAsync(batch);

            await TryActionOrRefreshCacheAsync(() => _cache.DoBatchAsync(batch));
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

        public Task<(IEnumerable<T> Entities, string ContinuationToken)> GetDataWithContinuationTokenAsync(TableQuery<T> rangeQuery, string continuationToken)
        {
            return _storage.GetDataWithContinuationTokenAsync(rangeQuery, continuationToken);
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

            await TryActionOrRefreshCacheAsync(() => _cache.InsertAsync(item, notLogCodes));
        }

        public async Task InsertAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.InsertAsync(list);

            await TryActionOrRefreshCacheAsync(() => _cache.InsertAsync(list));
        }

        public async Task InsertOrMergeAsync(T item)
        {
            await _storage.InsertOrMergeAsync(item);

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrMergeAsync(item));
        }

        public async Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.InsertOrMergeBatchAsync(list);

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrMergeBatchAsync(list));
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await _storage.InsertOrReplaceAsync(item);

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrReplaceAsync(item));
        }

        public async Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            var list = items.ToList();

            await _storage.InsertOrReplaceAsync(list);

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrReplaceAsync(list));
        }

        public async Task<bool> InsertOrReplaceAsync(T entity, Func<T, bool> replaceCondition)
        {
            var result = await _storage.InsertOrReplaceAsync(entity, replaceCondition);

            if (!result) return false;

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrReplaceAsync(entity, replaceCondition));

            return true;
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            var list = entities.ToList();

            await _storage.InsertOrReplaceBatchAsync(list);

            await TryActionOrRefreshCacheAsync(() => _cache.InsertOrReplaceBatchAsync(list));
        }

        public async Task ReplaceAsync(T entity)
        {
            await _storage.ReplaceAsync(entity);

            await TryActionOrRefreshCacheAsync(() => _cache.ReplaceAsync(entity));
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _storage.MergeAsync(partitionKey, rowKey, item);

            if (result != null)
            {
                await TryActionOrRefreshCacheAsync(() => _cache.MergeAsync(partitionKey, rowKey, item));
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
                await TryActionOrRefreshCacheAsync(() => _cache.ReplaceAsync(partitionKey, rowKey, item));
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

            //var records = await _storage.GetDataAsync();
            //await _cache.InsertAsync(records);
            await _cache.InsertAsync(_storage);

            return true;
        }

        private async Task TryActionOrRefreshCacheAsync(Func<Task> action)
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

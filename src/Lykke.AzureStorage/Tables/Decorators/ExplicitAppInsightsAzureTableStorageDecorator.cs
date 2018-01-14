using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage.Table;
using Lykke.AzureStorage.Tables.Paging;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Decorator which explicitly calls AppInsights API to submit Azure Table Storage call events
    /// </summary>
    internal class ExplicitAppInsightsAzureTableStorageDecorator<TEntity> : INoSQLTableStorage<TEntity>
        where TEntity : ITableEntity, new()
    {
        private const string _telemetryDependencyType = "Azure table";
        private readonly INoSQLTableStorage<TEntity> _impl;
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        public string Name => _impl.Name;

        internal ExplicitAppInsightsAzureTableStorageDecorator(INoSQLTableStorage<TEntity> storage)
        {
            _impl = storage;
        }

        #region INoSqlTableStorage{TEntity} decoration

        public IEnumerator<TEntity> GetEnumerator()
            => Wrap(() => _impl.GetEnumerator(), nameof(GetEnumerator));

        IEnumerator IEnumerable.GetEnumerator()
            => Wrap(() => ((IEnumerable)_impl).GetEnumerator(), nameof(GetEnumerator));

        TEntity INoSQLTableStorage<TEntity>.this[string partition, string row]
            => Wrap(() => _impl[partition, row], "[partition, row]");

        IEnumerable<TEntity> INoSQLTableStorage<TEntity>.this[string partition]
            => Wrap(() => _impl[partition], "[partition]");

        public Task InsertAsync(TEntity item, params int[] notLogCodes)
            => WrapAsync(() => _impl.InsertAsync(item, notLogCodes), nameof(InsertAsync));

        public Task InsertAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.InsertAsync(items), nameof(InsertAsync));

        public Task InsertOrMergeAsync(TEntity item)
            => WrapAsync(() => _impl.InsertOrMergeAsync(item), nameof(InsertOrMergeAsync));

        public Task InsertOrMergeBatchAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.InsertOrMergeBatchAsync(items), nameof(InsertOrMergeBatchAsync));

        public Task ReplaceAsync(TEntity entity)
            => WrapAsync(() => _impl.ReplaceAsync(entity), nameof(ReplaceAsync));

        public Task<TEntity> ReplaceAsync(string partitionKey, string rowKey, Func<TEntity, TEntity> item)
            => WrapAsync(() => _impl.ReplaceAsync(partitionKey, rowKey, item), nameof(ReplaceAsync));

        public Task<TEntity> MergeAsync(string partitionKey, string rowKey, Func<TEntity, TEntity> item)
            => WrapAsync(() => _impl.MergeAsync(partitionKey, rowKey, item), nameof(MergeAsync));

        public Task InsertOrReplaceBatchAsync(IEnumerable<TEntity> entities)
            => WrapAsync(() => _impl.InsertOrReplaceBatchAsync(entities), nameof(InsertOrReplaceBatchAsync));

        public Task InsertOrReplaceAsync(TEntity item)
            => WrapAsync(() => _impl.InsertOrReplaceAsync(item), nameof(InsertOrReplaceAsync));

        public Task InsertOrReplaceAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.InsertOrReplaceAsync(items), nameof(InsertOrReplaceAsync));

        public Task DeleteAsync(TEntity item)
            => WrapAsync(() => _impl.DeleteAsync(item), nameof(DeleteAsync));

        public Task<TEntity> DeleteAsync(string partitionKey, string rowKey)
            => WrapAsync(() => _impl.DeleteAsync(partitionKey, rowKey), nameof(DeleteAsync));

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
            => WrapAsync(() => _impl.DeleteIfExistAsync(partitionKey, rowKey), nameof(DeleteIfExistAsync));

        public Task<bool> DeleteAsync()
            => WrapAsync(() => _impl.DeleteAsync(), nameof(DeleteAsync));

        public Task DeleteAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.DeleteAsync(items), nameof(DeleteAsync));

        public Task<bool> CreateIfNotExistsAsync(TEntity item)
            => WrapAsync(() => _impl.CreateIfNotExistsAsync(item), nameof(CreateIfNotExistsAsync));

        public bool RecordExists(TEntity item)
            => Wrap(() => _impl.RecordExists(item), nameof(RecordExists));

        public Task<bool> RecordExistsAsync(TEntity item)
            => WrapAsync(() => _impl.RecordExistsAsync(item), nameof(RecordExistsAsync));

        public Task<TEntity> GetDataAsync(string partition, string row)
            => WrapAsync(() => _impl.GetDataAsync(partition, row), nameof(GetDataAsync));

        public Task<IList<TEntity>> GetDataAsync(Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(filter), nameof(GetDataAsync));

        public Task<IEnumerable<TEntity>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(partitionKey, rowKeys, pieceSize, filter), nameof(GetDataAsync));

        public Task<IEnumerable<TEntity>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(partitionKeys, pieceSize, filter), nameof(GetDataAsync));

        public Task<IEnumerable<TEntity>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(keys, pieceSize, filter), nameof(GetDataAsync));

        public Task GetDataByChunksAsync(Func<IEnumerable<TEntity>, Task> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(chunks), nameof(GetDataByChunksAsync));

        public Task GetDataByChunksAsync(TableQuery<TEntity> rangeQuery, Func<IEnumerable<TEntity>, Task> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(rangeQuery, chunks), nameof(GetDataByChunksAsync));

        public Task GetDataByChunksAsync(Action<IEnumerable<TEntity>> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(chunks), nameof(GetDataByChunksAsync));

        public Task GetDataByChunksAsync(TableQuery<TEntity> rangeQuery, Action<IEnumerable<TEntity>> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(rangeQuery, chunks), nameof(GetDataByChunksAsync));

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<TEntity>> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(partitionKey, chunks), nameof(GetDataByChunksAsync));

        public Task ScanDataAsync(string partitionKey, Func<IEnumerable<TEntity>, Task> chunk)
            => WrapAsync(() => _impl.ScanDataAsync(partitionKey, chunk), nameof(ScanDataAsync));

        public Task ScanDataAsync(TableQuery<TEntity> rangeQuery, Func<IEnumerable<TEntity>, Task> chunk)
            => WrapAsync(() => _impl.ScanDataAsync(rangeQuery, chunk), nameof(ScanDataAsync));

        public Task<TEntity> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<TEntity>, TEntity> dataToSearch)
            => WrapAsync(() => _impl.FirstOrNullViaScanAsync(partitionKey, dataToSearch), nameof(FirstOrNullViaScanAsync));

        public Task<IEnumerable<TEntity>> GetDataAsync(string partition, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(partition, filter), nameof(GetDataAsync));

        public Task<TEntity> GetTopRecordAsync(string partition)
            => WrapAsync(() => _impl.GetTopRecordAsync(partition), nameof(GetTopRecordAsync));

        public Task<IEnumerable<TEntity>> GetTopRecordsAsync(string partition, int n)
            => WrapAsync(() => _impl.GetTopRecordsAsync(partition, n), nameof(GetTopRecordsAsync));

        public Task<IEnumerable<TEntity>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
            => WrapAsync(() => _impl.GetDataRowKeysOnlyAsync(rowKeys), nameof(GetDataRowKeysOnlyAsync));

        public Task<IEnumerable<TEntity>> WhereAsyncc(TableQuery<TEntity> rangeQuery, Func<TEntity, Task<bool>> filter = null)
            => WrapAsync(() => _impl.WhereAsyncc(rangeQuery, filter), nameof(WhereAsyncc));

        public Task<IEnumerable<TEntity>> WhereAsync(TableQuery<TEntity> rangeQuery, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.WhereAsync(rangeQuery, filter), nameof(WhereAsync));

        public Task ExecuteAsync(TableQuery<TEntity> rangeQuery, Action<IEnumerable<TEntity>> yieldResult, Func<bool> stopCondition = null)
            => WrapAsync(() => _impl.ExecuteAsync(rangeQuery, yieldResult, stopCondition), nameof(ExecuteAsync));

        public Task DoBatchAsync(TableBatchOperation batch)
            => WrapAsync(() => _impl.DoBatchAsync(batch), nameof(DoBatchAsync));

        public Task<IPagedResult<TEntity>> ExecuteQueryWithPaginationAsync(TableQuery<TEntity> query, PagingInfo pagingInfo)
            => WrapAsync(() => _impl.ExecuteQueryWithPaginationAsync(query, pagingInfo), nameof(ExecuteQueryWithPaginationAsync));

        public Task CreateTableIfNotExistsAsync()
            => WrapAsync(() => _impl.CreateTableIfNotExistsAsync(), nameof(CreateTableIfNotExistsAsync));

        #endregion

        #region decoration logic

        private async Task<TResult> WrapAsync<TResult>(Func<Task<TResult>> func, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                isSuccess = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackDependency(
                    _telemetryDependencyType,
                    _impl.Name,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        private async Task WrapAsync(Func<Task> func, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                await func();
            }
            catch (Exception e)
            {
                isSuccess = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackDependency(
                    _telemetryDependencyType,
                    _impl.Name,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        private TResult Wrap<TResult>(Func<TResult> func, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                return func();
            }
            catch (Exception e)
            {
                isSuccess = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackDependency(
                    _telemetryDependencyType,
                    _impl.Name,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        #endregion
    }
}

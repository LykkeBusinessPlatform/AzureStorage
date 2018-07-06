using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Lykke.AzureStorage.Tables.Paging;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Decorator which explicitly calls AppInsights API to submit Azure Table Storage call events
    /// </summary>
    internal class ExplicitAppInsightsAzureTableStorageDecorator<TEntity> : ExplicitAppInsightsCallDecoratorBase, INoSQLTableStorage<TEntity>
        where TEntity : ITableEntity, new()
    {
        private readonly INoSQLTableStorage<TEntity> _impl;

        protected override string TrackType => "Azure table";

        public string Name => _impl.Name;

        internal ExplicitAppInsightsAzureTableStorageDecorator(INoSQLTableStorage<TEntity> storage)
        {
            _impl = storage;
        }

        #region INoSqlTableStorage{TEntity} decoration

        public IEnumerator<TEntity> GetEnumerator()
            => Wrap(() => _impl.GetEnumerator(), Name);

        IEnumerator IEnumerable.GetEnumerator()
            => Wrap(() => ((IEnumerable)_impl).GetEnumerator(), Name);

        TEntity INoSQLTableStorage<TEntity>.this[string partition, string row]
            => Wrap(() => _impl[partition, row], Name, "[partition, row]");

        IEnumerable<TEntity> INoSQLTableStorage<TEntity>.this[string partition]
            => Wrap(() => _impl[partition], Name, "[partition]");

        public Task InsertAsync(TEntity item, params int[] notLogCodes)
            => WrapAsync(() => _impl.InsertAsync(item, notLogCodes), Name, "InsertAsync item");

        public Task InsertAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.InsertAsync(items), Name, "InsertAsync items");

        public Task InsertOrMergeAsync(TEntity item)
            => WrapAsync(() => _impl.InsertOrMergeAsync(item), Name);

        public Task InsertOrMergeBatchAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.InsertOrMergeBatchAsync(items), Name);

        public Task ReplaceAsync(TEntity entity)
            => WrapAsync(() => _impl.ReplaceAsync(entity), Name, "ReplaceAsync - item");

        public Task<TEntity> ReplaceAsync(string partitionKey, string rowKey, Func<TEntity, TEntity> replaceAction)
            => WrapAsync(() => _impl.ReplaceAsync(partitionKey, rowKey, replaceAction), Name, "ReplaceAsync - pk, rk, func");

        public Task<TEntity> MergeAsync(string partitionKey, string rowKey, Func<TEntity, TEntity> mergeAction)
            => WrapAsync(() => _impl.MergeAsync(partitionKey, rowKey, mergeAction), Name);

        public Task InsertOrReplaceBatchAsync(IEnumerable<TEntity> entities)
            => WrapAsync(() => _impl.InsertOrReplaceBatchAsync(entities), Name);

        public Task InsertOrReplaceAsync(TEntity item)
            => WrapAsync(() => _impl.InsertOrReplaceAsync(item), Name, "InsertOrReplaceAsync item");

        public Task InsertOrReplaceAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.InsertOrReplaceAsync(items), Name, "InsertOrReplaceAsync items");

        public Task<bool> InsertOrReplaceAsync(TEntity entity, Func<TEntity, bool> replaceCondition)
            => WrapAsync(() => _impl.InsertOrReplaceAsync(entity, replaceCondition), Name, "InsertOrReplace with condition");

        public Task<bool> InsertOrModifyAsync(string partitionKey, string rowKey, Func<TEntity> create, Func<TEntity, bool> modify)
            => WrapAsync(() => _impl.InsertOrModifyAsync(partitionKey, rowKey, create, modify), Name);

        public Task DeleteAsync(TEntity item)
            => WrapAsync(() => _impl.DeleteAsync(item), Name, "DeleteAsync item");

        public Task<TEntity> DeleteAsync(string partitionKey, string rowKey)
            => WrapAsync(() => _impl.DeleteAsync(partitionKey, rowKey), Name, "DeleteAsync [pk, rk]");

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey, Func<TEntity, bool> deleteCondition)
            => WrapAsync(() => _impl.DeleteIfExistAsync(partitionKey, rowKey, deleteCondition), Name, "DeleteIfExist with condition");

        public Task<bool> DeleteAsync()
            => WrapAsync(() => _impl.DeleteAsync(), Name, "DeleteAsync table");

        public Task DeleteAsync(IEnumerable<TEntity> items)
            => WrapAsync(() => _impl.DeleteAsync(items), Name, "DeleteAsync items");

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
            => WrapAsync(() => _impl.DeleteIfExistAsync(partitionKey, rowKey), Name);

        public Task<bool> CreateIfNotExistsAsync(TEntity item)
            => WrapAsync(() => _impl.CreateIfNotExistsAsync(item), Name);

        public bool RecordExists(TEntity item)
            => Wrap(() => _impl.RecordExists(item), Name);

        public Task<bool> RecordExistsAsync(TEntity item)
            => WrapAsync(() => _impl.RecordExistsAsync(item), Name);

        public Task<TEntity> GetDataAsync(string partition, string row)
            => WrapAsync(() => _impl.GetDataAsync(partition, row), Name, "GetDataAsync - pk, rk");

        public Task<IList<TEntity>> GetDataAsync(Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(filter), Name, "GetDataAsync - filter");

        public Task<IEnumerable<TEntity>> GetDataAsync(string partition, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(partition, filter), Name, "GetDataAsync - 1 pk, filter");

        public Task<IEnumerable<TEntity>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(partitionKey, rowKeys, pieceSize, filter), Name, "GetDataAsync - 1 pk, many rk");

        public Task<IEnumerable<TEntity>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(partitionKeys, pieceSize, filter), Name, "GetDataAsync - many pk");

        public Task<IEnumerable<TEntity>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.GetDataAsync(keys, pieceSize, filter), Name, "GetDataAsync - pairs");

        public Task GetDataByChunksAsync(Func<IEnumerable<TEntity>, Task> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(chunks), Name, "GetDataByChunksAsync - chunk tasks");

        public Task GetDataByChunksAsync(TableQuery<TEntity> rangeQuery, Func<IEnumerable<TEntity>, Task> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(rangeQuery, chunks), Name, "GetDataByChunksAsync - query, chunk tasks");

        public Task GetDataByChunksAsync(Action<IEnumerable<TEntity>> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(chunks), Name, "GetDataByChunksAsync - chunks");

        public Task GetDataByChunksAsync(TableQuery<TEntity> rangeQuery, Action<IEnumerable<TEntity>> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(rangeQuery, chunks), Name, "GetDataByChunksAsync - query, chunks");

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<TEntity>> chunks)
            => WrapAsync(() => _impl.GetDataByChunksAsync(partitionKey, chunks), Name, "GetDataByChunksAsync - 1 pk, chunks");

        public Task<(IEnumerable<TEntity> Entities, string ContinuationToken)> GetDataWithContinuationTokenAsync(TableQuery<TEntity> rangeQuery, string continuationToken) 
            => WrapAsync(() => _impl.GetDataWithContinuationTokenAsync(rangeQuery, continuationToken), Name, "GetDataWithContinuationTokenAsync - query");

        public Task ScanDataAsync(string partitionKey, Func<IEnumerable<TEntity>, Task> chunk)
            => WrapAsync(() => _impl.ScanDataAsync(partitionKey, chunk), Name, "ScanDataAsync - pk, chunk tasks");

        public Task ScanDataAsync(TableQuery<TEntity> rangeQuery, Func<IEnumerable<TEntity>, Task> chunk)
            => WrapAsync(() => _impl.ScanDataAsync(rangeQuery, chunk), Name, "ScanDataAsync - query, chunk tasks");

        public Task<TEntity> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<TEntity>, TEntity> dataToSearch)
            => WrapAsync(() => _impl.FirstOrNullViaScanAsync(partitionKey, dataToSearch), Name);

        public Task<TEntity> GetTopRecordAsync(string partition)
            => WrapAsync(() => _impl.GetTopRecordAsync(partition), Name);

        public Task<IEnumerable<TEntity>> GetTopRecordsAsync(string partition, int n)
            => WrapAsync(() => _impl.GetTopRecordsAsync(partition, n), Name);

        public Task<TEntity> GetTopRecordAsync(TableQuery<TEntity> query)
            => WrapAsync(() => _impl.GetTopRecordAsync(query), Name);

        public Task<IEnumerable<TEntity>> GetTopRecordsAsync(TableQuery<TEntity> query, int n)
            => WrapAsync(() => _impl.GetTopRecordsAsync(query, n), Name);

        public Task<IEnumerable<TEntity>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
            => WrapAsync(() => _impl.GetDataRowKeysOnlyAsync(rowKeys), Name);

        public Task<IEnumerable<TEntity>> WhereAsyncc(TableQuery<TEntity> rangeQuery, Func<TEntity, Task<bool>> filter = null)
            => WrapAsync(() => _impl.WhereAsyncc(rangeQuery, filter), Name);

        public Task<IEnumerable<TEntity>> WhereAsync(TableQuery<TEntity> rangeQuery, Func<TEntity, bool> filter = null)
            => WrapAsync(() => _impl.WhereAsync(rangeQuery, filter), Name);

        public Task ExecuteAsync(TableQuery<TEntity> rangeQuery, Action<IEnumerable<TEntity>> yieldResult, Func<bool> stopCondition = null)
            => WrapAsync(() => _impl.ExecuteAsync(rangeQuery, yieldResult, stopCondition), Name);

        public Task DoBatchAsync(TableBatchOperation batch)
            => WrapAsync(() => _impl.DoBatchAsync(batch), Name);

        public Task<IPagedResult<TEntity>> ExecuteQueryWithPaginationAsync(TableQuery<TEntity> query, PagingInfo pagingInfo)
            => WrapAsync(() => _impl.ExecuteQueryWithPaginationAsync(query, pagingInfo), Name);

        public Task CreateTableIfNotExistsAsync()
            => WrapAsync(() => _impl.CreateTableIfNotExistsAsync(), Name);

        #endregion
    }
}

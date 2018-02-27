using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Lykke.AzureStorage;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage
{
    public interface INoSQLTableStorage<T> : IEnumerable<T> where T : ITableEntity, new()
    {
        /// <summary>
        /// Storage name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Queries a row.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        /// <param name="partition">Partition</param>
        /// <param name="row">Row</param>
        /// <returns>null or row item</returns>
        T this[string partition, string row] { get; }

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        IEnumerable<T> this[string partition] { get; }

        /// <summary>
        /// Add new row to the table.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        /// <param name="item">Row item to insert</param>
        /// <param name="notLogCodes">Azure table storage exceptions codes, which are should not be logged</param>
        Task InsertAsync(T item, params int[] notLogCodes);

        /// <summary>
        /// Add new row to the table.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task InsertAsync(IEnumerable<T> items);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task InsertOrMergeAsync(T item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task InsertOrMergeBatchAsync(IEnumerable<T> items);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used.
        /// Doesn't logs optimistic concurrency exceptions.
        /// </summary>
        /// <exception cref="OptimisticConcurrencyException">Will be thrown when entity was changed by someone else</exception>
        Task ReplaceAsync(T entity);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task InsertOrReplaceBatchAsync(IEnumerable<T> entities);

        /// <summary>
        /// Adds or entirely replaces row.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task InsertOrReplaceAsync(T item);

        /// <summary>
        /// Adds or entirely replaces row.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task InsertOrReplaceAsync(IEnumerable<T> items);

        /// <summary>
        /// Adds or replaces a row if existing row state satisfies replace condition.
        /// This method is concurrent safe.
        /// This method assumes, that <paramref name="entity"/> is just created (has been not read) 
        /// and thus doesn't contain ETag.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used.
        /// </summary>
        /// <param name="entity">Entity which should be saved</param>
        /// <param name="replaceCondition">
        /// Condition to check: whether existing record can be replaced. 
        /// You should check passed entity state and return true, if 
        /// the entity can be replaced or return false if the entity can't be replaced.
        /// </param>
        /// <returns>
        /// true - the record has been inserted or replaced, otherwise - false
        /// </returns>
        Task<bool> InsertOrReplaceAsync(T entity, Func<T, bool> replaceCondition);

        /// <summary>
        /// Adds or modifies a row.
        /// This method is concurrent safe.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used.
        /// </summary>
        /// <param name="partitionKey">Record partition key</param>
        /// <param name="rowKey">Record row key</param>
        /// <param name="create">
        /// This delegate will be called if there is no record with given partition and row keys.
        /// You should return new entity.
        /// </param>
        /// <param name="modify">
        /// This delegate will be called if record with given partition and row keys is exists.
        /// Existing entity will be passed to this delegate, you can modify it as you wish except PartitionKey, PowKey and ETag.
        /// You should check passed entity state, whether it can be replaced and return truereturn true, if entity should be saved, 
        /// otherwise - you should return false. 
        /// </param>
        /// <returns>
        /// true - the record has been inserted or modified, otherwise - false
        /// </returns>
        Task<bool> InsertOrModifyAsync(string partitionKey, string rowKey, Func<T> create, Func<T, bool> modify);

        /// <summary>
        /// Deletes row from the table.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task DeleteAsync(T item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<T> DeleteAsync(string partitionKey, string rowKey);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey);

        /// <summary>
        /// Deletes the record if it exists and delete condition is satisfied. 
        /// This method is concurrent safe.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used.
        /// </summary>
        /// <param name="partitionKey">Record partition key</param>
        /// <param name="rowKey">Record row key</param>
        /// <param name="deleteCondition">
        /// Condition to check: whether the record can be deleted. 
        /// You should check passed entity state and return true, if 
        /// the entity can be deleted or return false if the entity can't be deleted.
        /// </param>
        /// <returns>true - the record has been delete, otherwise - false</returns>
        Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey, Func<T, bool> deleteCondition);

        /// <summary>
        /// Deletes the table.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<bool> DeleteAsync();

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task DeleteAsync(IEnumerable<T> items);

        /// <summary>
        /// Creates record if not existed before.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        /// <returns>true if created, false if existed before</returns>
        Task<bool> CreateIfNotExistsAsync(T item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        bool RecordExists(T item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<bool> RecordExistsAsync(T item);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<T> GetDataAsync(string partition, string row);

        /// <summary>
        /// Queries data with client-side filtering.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IList<T>> GetDataAsync(Func<T, bool> filter = null);

        /// <summary>
        /// Queries multiple rows of single partition.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        /// <param name="partitionKey">Partition key</param>
        /// <param name="rowKeys">Row keys</param>
        /// <param name="pieceSize">Chank size</param>
        /// <param name="filter">Rows filter</param>
        Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100,
            Func<T, bool> filter = null);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100,
            Func<T, bool> filter = null);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100,
            Func<T, bool> filter = null);
        
        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks);

        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunks);

        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks);

        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks);

        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<(IEnumerable<T> Entities, string ContinuationToken)> GetDataWithContinuationTokenAsync(TableQuery<T> rangeQuery, string continuationToken);

        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk);
        
        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task ScanDataAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunk);

        /// <summary>
        /// Scan table by chinks and find an instane.
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        /// <param name="partitionKey">Partition we are going to scan</param>
        /// <param name="dataToSearch">CallBack, which we going to call when we have chunk of data to scan. </param>
        /// <returns>Null or instance</returns>
        Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<T> GetTopRecordAsync(string partition);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n);

        /// <summary>
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null);

        /// <summary>
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null);

        /// <summary>
        /// Recieves data asynchronously. Could be used for memory saving
        /// Not auto-retried, if <see cref="AzureTableStorage{T}"/> implementation is used, since this is not atomic operation
        /// </summary>
        /// <param name="rangeQuery">Query</param>
        /// <param name="yieldResult">Data chank processing delegate</param>
        /// <param name="stopCondition">Stop condition func</param>
        Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition = null);

        /// <summary>
        /// Executes batch of operations.
        /// Auto retries, if <see cref="AzureTableStorage{T}"/> implementation is used
        /// </summary>
        Task DoBatchAsync(TableBatchOperation batch);

        /// <summary>
        /// Executes provided query with pagination. Not auto-retried.
        /// </summary>
        /// <param name="query">Query</param>
        /// <param name="pagingInfo">Paging information</param>
        /// <returns></returns>
        Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo);

        /// <summary>
        /// Creates the table if it doesn't exist
        /// </summary>
        Task CreateTableIfNotExistsAsync();
    }
}

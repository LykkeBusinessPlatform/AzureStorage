using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common.Extensions;
using JetBrains.Annotations;
using Lykke.AzureStorage;
using Lykke.AzureStorage.Cryptography;
using Lykke.AzureStorage.Tables.Decorators;
using Lykke.AzureStorage.Tables.Paging;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Wrapper on INoSQLTableStorage, uses symmetric encryption.
    /// Does not support queries (TableQuery<T>) and batches(TableBatchOperation).
    /// </summary>
    public class EncryptedTableStorageDecorator<T> : INoSQLTableStorage<T> where T : ITableEntity, new()
    {
        private readonly INoSQLTableStorage<T> _storage;
        private readonly ICryptographicSerializer _serializer;
        private readonly bool _forceEncrypt;
        private readonly List<PropertyInfo> _encryptedProperties;

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="serializer"></param>
        /// <param name="forceEncrypt">If true, checks if existed entries are encrypted, and encrypt them if not.</param>
        public EncryptedTableStorageDecorator(
            [NotNull] INoSQLTableStorage<T> storage,
            [NotNull] ICryptographicSerializer serializer,
            bool forceEncrypt = true)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _forceEncrypt = forceEncrypt;

            var encryptAttribute = typeof(EncryptAttribute);
            _encryptedProperties = typeof(T).GetProperties().Where(x => Attribute.IsDefined(x, encryptAttribute)).ToList();
            if (_encryptedProperties.Count == 0)
                throw new ArgumentException("No properties marked as encrypted.");
            if (_encryptedProperties.Any(x => x.PropertyType != typeof(string)))
                throw new ArgumentException($"Only {typeof(string).FullName} type properties can be marked as encrypted.");
            if (_encryptedProperties.Any(x => x.SetMethod == null || x.GetMethod == null))
                throw new ArgumentException("Only properties with both Get and Set method allowed. Please check this: " + string.Join(", ",
                                                _encryptedProperties.Where(x => x.SetMethod == null || x.GetMethod == null).Select(x => x.Name)));
        }

        public static INoSQLTableStorage<T> Create(INoSQLTableStorage<T> storage, ICryptographicSerializer algo)
        {
            return new EncryptedTableStorageDecorator<T>(storage, algo);
        }

        private T Decrypt(T entity)
        {
            if (entity == null)
            {
                return default(T);
            }

            var hasNonEncryptedProperties = false;
            foreach (var property in _encryptedProperties)
            {
                var encrypted = property.GetValue(entity) as string;
                if (string.IsNullOrEmpty(encrypted))
                    continue;

                if (_forceEncrypt && !_serializer.IsEncrypted(encrypted))
                {
                    hasNonEncryptedProperties = true;
                    continue;
                }

                var value = _serializer.Deserialize(encrypted);
                property.SetValue(entity, value);
            }

            if (hasNonEncryptedProperties)
            {
                ReplaceAsync(entity).GetAwaiter().GetResult();
            }

            return entity;
        }

        private T Encrypt(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            entity = entity.CloneJson();

            foreach (var property in _encryptedProperties)
            {
                var value = property.GetValue(entity) as string;
                if (string.IsNullOrEmpty(value))
                    continue;
                var encrypted = _serializer.Serialize(value);
                property.SetValue(entity, encrypted);
            }

            return entity;
        }

        private Func<T, bool> Map(Func<T, bool> func)
        {
            if (func == null)
                return null;

            Func<T, bool> mapFunc = entity =>
            {
                var data = Decrypt(entity);
                return func(data);
            };

            return mapFunc;
        }

        private Func<T, T> Map(Func<T, T> func)
        {
            if (func == null)
                return null;

            Func<T, T> mapFunc = entity =>
            {
                var data = Decrypt(entity);
                data = func(data);
                return Encrypt(data);
            };

            return mapFunc;
        }

        private Func<IEnumerable<T>, Task> Map(Func<IEnumerable<T>, Task> func)
        {
            if (func == null)
                return null;

            Func<IEnumerable<T>, Task> mapFunc = entities =>
            {
                var data = entities.Select(Decrypt);
                return func(data);
            };

            return mapFunc;
        }

        private Action<IEnumerable<T>> Map(Action<IEnumerable<T>> func)
        {
            if (func == null)
                return null;

            Action<IEnumerable<T>> mapFunc = entities =>
            {
                var data = entities.Select(Decrypt);
                func(data);
            };

            return mapFunc;
        }

        private Func<IEnumerable<T>, T> Map(Func<IEnumerable<T>, T> func)
        {
            if (func == null)
                return null;

            Func<IEnumerable<T>, T> mapFunc = entities =>
            {
                var data = entities.Select(Decrypt);
                var result = func(data);
                if (result == null)
                    return default(T);
                return Encrypt(result);
            };

            return mapFunc;
        }



        public IEnumerator<T> GetEnumerator()
        {
            return GetDataAsync().RunSync().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Task InsertAsync(T item, params int[] notLogCodes)
        {
            var cryptoItem = Encrypt(item);
            return _storage.InsertAsync(cryptoItem, notLogCodes);
        }

        public Task InsertAsync(IEnumerable<T> items)
        {
            var cryptoItems = items.Select(Encrypt);
            return _storage.InsertAsync(cryptoItems);
        }

        public Task InsertOrMergeAsync(T item)
        {
            var cryptoItem = Encrypt(item);
            return _storage.InsertOrMergeAsync(cryptoItem);
        }

        public Task InsertOrMergeBatchAsync(IEnumerable<T> items)
        {
            var cryptoItems = items.Select(Encrypt);
            return _storage.InsertOrMergeBatchAsync(cryptoItems);
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var cryptoItem = await _storage.ReplaceAsync(partitionKey, rowKey, Map(item));

            return Decrypt(cryptoItem);
        }

        public Task ReplaceAsync(T entity)
        {
            var cryptoItem = Encrypt(entity);
            return _storage.ReplaceAsync(cryptoItem);
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var cryptoItem = await _storage.MergeAsync(partitionKey, rowKey, Map(item));

            return Decrypt(cryptoItem);
        }

        public Task InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        {
            var cryptoItems = entities.Select(Encrypt);
            return _storage.InsertOrReplaceBatchAsync(cryptoItems);
        }

        public Task InsertOrReplaceAsync(T item)
        {
            var cryptoItem = Encrypt(item);
            return _storage.InsertOrReplaceAsync(cryptoItem);
        }

        public Task InsertOrReplaceAsync(IEnumerable<T> items)
        {
            var cryptoItems = items.Select(Encrypt);
            return _storage.InsertOrReplaceAsync(cryptoItems);
        }

        public Task<bool> InsertOrReplaceAsync(T entity, Func<T, bool> replaceCondition)
        {
            var cryptoItem = Encrypt(entity);
            return _storage.InsertOrReplaceAsync(cryptoItem, Map(replaceCondition));
        }

        public Task<bool> InsertOrModifyAsync(string partitionKey, string rowKey, Func<T> create, Func<T, bool> modify)
        {
            return _storage.InsertOrModifyAsync(partitionKey, rowKey, () => Encrypt(create()), Map(modify));
        }

        public Task DeleteAsync(T item)
        {
            var cryptoItem = Encrypt(item);
            return _storage.DeleteAsync(cryptoItem);
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var cryptoItem = await _storage.DeleteAsync(partitionKey, rowKey);
            return Decrypt(cryptoItem);
        }

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey)
        {
            return _storage.DeleteIfExistAsync(partitionKey, rowKey);
        }

        public Task<bool> DeleteIfExistAsync(string partitionKey, string rowKey, Func<T, bool> deleteCondition)
        {
            return _storage.DeleteIfExistAsync(partitionKey, rowKey, Map(deleteCondition));
        }

        public Task DeleteAsync(IEnumerable<T> items)
        {
            var cryptoItems = items.Select(Encrypt);
            return _storage.DeleteAsync(cryptoItems);
        }

        public Task<bool> CreateIfNotExistsAsync(T item)
        {
            var cryptoItem = Encrypt(item);
            return _storage.CreateIfNotExistsAsync(cryptoItem);
        }

        public bool RecordExists(T item)
        {
            var cryptoItem = Encrypt(item);
            return _storage.RecordExists(cryptoItem);
        }

        public Task<bool> RecordExistsAsync(T item)
        {
            var cryptoItem = Encrypt(item);
            return _storage.RecordExistsAsync(cryptoItem);
        }

        public async Task<T> GetDataAsync(string partition, string row)
        {
            var cryptoItem = await _storage.GetDataAsync(partition, row);
            return Decrypt(cryptoItem);
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var cryptoItems = await _storage.GetDataAsync(Map(filter));
            return cryptoItems.Select(Decrypt).ToList();
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var cryptoItems = await _storage.GetDataAsync(partitionKey, rowKeys, pieceSize, Map(filter));
            return cryptoItems.Select(Decrypt);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var cryptoItems = await _storage.GetDataAsync(partitionKeys, pieceSize, Map(filter));
            return cryptoItems.Select(Decrypt);
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var cryptoItems = await _storage.GetDataAsync(keys, pieceSize, Map(filter));
            return cryptoItems.Select(Decrypt);
        }

        public Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            return _storage.GetDataByChunksAsync(Map(chunks));
        }

        public Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            return _storage.GetDataByChunksAsync(Map(chunks));
        }

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            return _storage.GetDataByChunksAsync(partitionKey, Map(chunks));
        }

        public Task<(IEnumerable<T> Entities, string ContinuationToken)> GetDataWithContinuationTokenAsync(TableQuery<T> rangeQuery, string continuationToken)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        public Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        {
            return _storage.ScanDataAsync(partitionKey, Map(chunk));
        }

        public Task ScanDataAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunk)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            return Decrypt(await _storage.FirstOrNullViaScanAsync(partitionKey, Map(dataToSearch)));
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            return (await _storage.GetDataAsync(partition, Map(filter))).Select(Decrypt);
        }

        public async Task<T> GetTopRecordAsync(string partition)
        {
            return Decrypt(await _storage.GetTopRecordAsync(partition));
        }

        public async Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n)
        {
            return (await _storage.GetTopRecordsAsync(partition, n)).Select(Decrypt);
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            return (await _storage.GetDataRowKeysOnlyAsync(rowKeys)).Select(Decrypt);
        }

        public Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        public Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult, Func<bool> stopCondition = null)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        public Task DoBatchAsync(TableBatchOperation batch)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support batches");
        }

        public Task<bool> DeleteAsync()
        {
            return _storage.DeleteAsync();
        }

        public Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Func<IEnumerable<T>, Task> chunks)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        public Task GetDataByChunksAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> chunks)
        {
            throw new NotSupportedException($"{nameof(EncryptedTableStorageDecorator<T>)} does not support Queries");
        }

        /// <summary>
        /// Returns result with pagination.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="pagingInfo"></param>
        /// <remarks> This request will work for queries on non-encrypted fields only.</remarks>
        /// <returns></returns>
        public async Task<IPagedResult<T>> ExecuteQueryWithPaginationAsync(TableQuery<T> query, PagingInfo pagingInfo)
        {
            var result = await _storage.ExecuteQueryWithPaginationAsync(query, pagingInfo);
            return new PagedResult<T>(result.Select(Decrypt), result.PagingInfo);
        }

        public Task CreateTableIfNotExistsAsync()
        {
            return _storage.CreateTableIfNotExistsAsync();
        }

        public string Name => _storage.Name;

        T INoSQLTableStorage<T>.this[string partition, string row] => GetDataAsync(partition, row).RunSync();

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => GetDataAsync(partition).RunSync();
    }
}

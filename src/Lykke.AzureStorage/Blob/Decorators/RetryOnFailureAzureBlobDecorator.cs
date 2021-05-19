using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lykke.AzureStorage;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob.Decorators
{
    /// <summary>
    /// Decorator, which adds retries functionality to the operations of the <see cref="IBlobStorage"/> implementation
    /// </summary>
    internal class RetryOnFailureAzureBlobDecorator : IBlobStorage
    {
        public Stream this[string container, string key] 
            => _retryService.Retry(() => _impl[container, key], _onGettingRetryCount);

        private readonly IBlobStorage _impl;
        private readonly int _onModificationsRetryCount;
        private readonly int _onGettingRetryCount;
        private readonly RetryService _retryService;
            
        /// <summary>
        /// Creates decorator, which adds retries functionality to the operations of the <see cref="IBlobStorage"/> implementation
        /// </summary>
        /// <param name="impl"><see cref="IBlobStorage"/> instance to which actual work will be delegated</param>
        /// <param name="onModificationsRetryCount">Retries count for write operations</param>
        /// <param name="onGettingRetryCount">Retries count for read operations</param>
        /// <param name="retryDelay">Delay before next retry. Default value is 200 milliseconds</param>

        public RetryOnFailureAzureBlobDecorator(
            IBlobStorage impl,
            int onModificationsRetryCount = 10,
            int onGettingRetryCount = 10,
            TimeSpan? retryDelay = null)
        {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));

            if (onModificationsRetryCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(onModificationsRetryCount), onModificationsRetryCount, "Value should be greater than 0");
            }

            if (onGettingRetryCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(onGettingRetryCount), onGettingRetryCount, "Value should be greater than 0");
            }

            _onModificationsRetryCount = onModificationsRetryCount;
            _onGettingRetryCount = onGettingRetryCount;
            _retryService = new RetryService(
                retryDelay: retryDelay ?? TimeSpan.FromMilliseconds(200),
                exceptionFilter: e =>
                {
                    var storageException = e as StorageException;
                    var noRetryStatusCodes = new[]
                    {
                        HttpStatusCode.Conflict,
                        HttpStatusCode.BadRequest,
                        HttpStatusCode.PreconditionFailed
                    };

                    return storageException != null && noRetryStatusCodes.Contains((HttpStatusCode)storageException.RequestInformation.HttpStatusCode)
                        ? RetryService.ExceptionFilterResult.ThrowImmediately
                        : RetryService.ExceptionFilterResult.ThrowAfterRetries;
                });
        }

        public async Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false) 
            => await _retryService.RetryAsync(async () => await _impl.SaveBlobAsync(container, key, bloblStream, anonymousAccess), _onModificationsRetryCount);

        public async Task SaveBlobAsync(string container, string key, byte[] blob)
            => await _retryService.RetryAsync(async () => await _impl.SaveBlobAsync(container, key, blob), _onModificationsRetryCount);

        public async Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata) 
            => await _retryService.RetryAsync(async () => await _impl.SaveBlobAsync(container, key, blob, metadata), _onModificationsRetryCount);

        public async Task<bool> HasBlobAsync(string container, string key) 
            => await _retryService.RetryAsync(async () => await _impl.HasBlobAsync(container, key), _onGettingRetryCount);

        public async Task<bool> CreateContainerIfNotExistsAsync(string container) 
            => await _retryService.RetryAsync(async () => await _impl.CreateContainerIfNotExistsAsync(container), _onModificationsRetryCount);

        public async Task<DateTime> GetBlobsLastModifiedAsync(string container) 
            => await _retryService.RetryAsync(async () => await _impl.GetBlobsLastModifiedAsync(container), _onGettingRetryCount);

        public async Task<Stream> GetAsync(string container, string key) 
            => await _retryService.RetryAsync(async () => await _impl.GetAsync(container, key), _onGettingRetryCount);

        public async Task<string> GetAsTextAsync(string container, string key) 
            => await _retryService.RetryAsync(async () => await _impl.GetAsTextAsync(container, key), _onGettingRetryCount);

        public string GetBlobUrl(string container, string key) 
            => _retryService.Retry(() => _impl.GetBlobUrl(container, key), _onGettingRetryCount);

        public async Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix) 
            => await _retryService.RetryAsync(async () => await _impl.FindNamesByPrefixAsync(container, prefix), _onGettingRetryCount);

        public async Task DeleteBlobsByPrefixAsync(string container, string prefix)
            => await _retryService.RetryAsync(async () => await _impl.DeleteBlobsByPrefixAsync(container, prefix), _onModificationsRetryCount);

        public async Task<IEnumerable<string>> GetListOfBlobKeysByPrefixAsync(string container, string prefix,
            int? maxResultsCount = null)
            => await _retryService.RetryAsync(async () => await _impl.GetListOfBlobKeysByPrefixAsync(container, prefix, maxResultsCount), _onGettingRetryCount);

        public async Task<IEnumerable<string>> GetListOfBlobsAsync(string container) 
            => await _retryService.RetryAsync(async () => await _impl.GetListOfBlobsAsync(container), _onGettingRetryCount);

        public async Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null) 
            => await _retryService.RetryAsync(async () => await _impl.GetListOfBlobKeysAsync(container, maxResultsCount), _onGettingRetryCount);

        public async Task DelBlobAsync(string container, string key) 
            => await _retryService.RetryAsync(async () => await _impl.DelBlobAsync(container, key), _onModificationsRetryCount);

        public async Task<string> GetMetadataAsync(string container, string key, string metaDataKey) 
            => await _retryService.RetryAsync(async () => await _impl.GetMetadataAsync(container, key, metaDataKey), _onGettingRetryCount);

        public async Task<IDictionary<string, string>> GetMetadataAsync(string container, string key) 
            => await _retryService.RetryAsync(async () => await _impl.GetMetadataAsync(container, key), _onGettingRetryCount);

        public async Task<List<string>> ListBlobsAsync(string container, string path)
            => await _retryService.RetryAsync(async () => await _impl.ListBlobsAsync(container, path), _onGettingRetryCount);

        public async Task<BlobProperties> GetPropertiesAsync(string container, string key)
            => await _retryService.RetryAsync(async () => await _impl.GetPropertiesAsync(container, key), _onGettingRetryCount);
        
        public async Task<string> AcquireLeaseAsync(string container, string key, TimeSpan? leaseTime, string proposedLeaseId = null)
            => await _retryService.RetryAsync(async () => await _impl.AcquireLeaseAsync(container, key, leaseTime, proposedLeaseId), _onModificationsRetryCount);

        public async Task ReleaseLeaseAsync(string container, string key, string leaseId)
            => await _retryService.RetryAsync(async () => await _impl.ReleaseLeaseAsync(container, key, leaseId), _onModificationsRetryCount);

        public async Task RenewLeaseAsync(string container, string key, string leaseId)
            => await _retryService.RetryAsync(async () => await _impl.RenewLeaseAsync(container, key, leaseId), _onModificationsRetryCount);

        public async Task SetContainerPermissionsAsync(string container, BlobContainerPublicAccessType publicAccessType)
            => await _retryService.RetryAsync(async () => await _impl.SetContainerPermissionsAsync(container, publicAccessType), _onModificationsRetryCount);
    }
}

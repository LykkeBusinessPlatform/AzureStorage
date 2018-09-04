using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Blob.Decorators;
using JetBrains.Annotations;
using Lykke.AzureStorage.Blob.Exceptions;
using Lykke.SettingsReader;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob
{
    [PublicAPI]
    public class AzureBlobStorage : IBlobStorage
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly TimeSpan _maxExecutionTime;

        private AzureBlobStorage(string connectionString, TimeSpan? maxExecutionTimeout = null)
        {
            _storageAccount = CloudStorageAccount.Parse(connectionString);
            _maxExecutionTime = maxExecutionTimeout.GetValueOrDefault(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Creates <see cref="AzureBlobStorage"/> with automatically connection string reloading and auto-retries
        /// </summary>
        /// <param name="connectionStringManager">Connection string reloading manager</param>
        /// <param name="maxExecutionTimeout">Max execution timeout</param>
        /// <param name="onModificationRetryCount">Retries count for write operations</param>
        /// <param name="onGettingRetryCount">Retries count for read operations</param>
        /// <param name="retryDelay">Delay before next retry. Default value is 200 milliseconds</param>
        public static IBlobStorage Create(
            IReloadingManager<string> connectionStringManager, 
            TimeSpan? maxExecutionTimeout = null,
            int onModificationRetryCount = 10,
            int onGettingRetryCount = 10,
            TimeSpan? retryDelay = null)
        {
            return new RetryOnFailureAzureBlobDecorator(
                new ReloadingConnectionStringOnFailureAzureBlobDecorator(
                    async (bool reload) => new AzureBlobStorage(
                        reload ? await connectionStringManager.Reload() : connectionStringManager.CurrentValue,
                        maxExecutionTimeout)
                ),
                onModificationRetryCount,
                onGettingRetryCount,
                retryDelay);
        }

        private CloudBlobContainer GetContainerReference(string container)
        {
            NameValidator.ValidateContainerName(container);

            var blobClient = _storageAccount.CreateCloudBlobClient();
            return blobClient.GetContainerReference(container.ToLower());
        }

        private BlobRequestOptions GetRequestOptions()
        {
            return new BlobRequestOptions
            {
                MaximumExecutionTime = _maxExecutionTime
            };
        }

        public async Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(container, key, anonymousAccess, createIfNotExists: true);

            bloblStream.Position = 0;

            var mimeType = ContentTypesHelper.GetContentType(key);

            if (mimeType != null)
            {
                blockBlob.Properties.ContentType = mimeType;
            }

            await blockBlob.UploadFromStreamAsync(bloblStream, null, GetRequestOptions(), null);

            return blockBlob.Uri.AbsoluteUri;
        }

        private async Task<CloudBlockBlob> GetBlockBlobReferenceAsync(string container, string key, bool anonymousAccess = false, bool createIfNotExists = false)
        {
            NameValidator.ValidateBlobName(key);

            var containerRef = GetContainerReference(container);

            if (createIfNotExists)
            {
                await containerRef.CreateIfNotExistsAsync(GetRequestOptions(), null);
            }
            if (anonymousAccess)
            {
                var permissions = await containerRef.GetPermissionsAsync(null, GetRequestOptions(), null);
                permissions.PublicAccess = BlobContainerPublicAccessType.Container;
                await containerRef.SetPermissionsAsync(permissions, null, GetRequestOptions(), null);
            }
            
            return containerRef.GetBlockBlobReference(key);
        }

        public async Task SaveBlobAsync(string container, string key, byte[] blob)
        {
            await SaveBlobAsync(container, key, blob, null);
        }

        public async Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(container, key, createIfNotExists: true);

            var mimeType = ContentTypesHelper.GetContentType(key);
            
            if (mimeType != null)
            {
                blockBlob.Properties.ContentType = mimeType;
            }

            if (metadata != null)
            {
                foreach (var keyValuePair in metadata)
                {
                    blockBlob.Metadata[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            await blockBlob.UploadFromByteArrayAsync(blob, 0, blob.Length, null, GetRequestOptions(), null);
        }

        public async Task<bool> CreateContainerIfNotExistsAsync(string container)
        {
            var containerRef = GetContainerReference(container);

            return await containerRef.CreateIfNotExistsAsync(GetRequestOptions(), null);
        }

        public Task<bool> HasBlobAsync(string container, string key)
        {
            NameValidator.ValidateBlobName(key);

            var blobRef = GetContainerReference(container).GetBlobReference(key);
            return blobRef.ExistsAsync(GetRequestOptions(), null);
        }

        public async Task<List<string>> ListBlobsAsync(string container, string path)
        {
            var blobRef = GetContainerReference(container);

            var dir = blobRef.GetDirectoryReference(path);

            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                var response = await dir.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            } while (continuationToken != null);

            return results.Select(x => x.Uri.LocalPath.Substring(container.Length + 2)).ToList();
        }

        public async Task<DateTime> GetBlobsLastModifiedAsync(string container)
        {
            BlobContinuationToken continuationToken = null;
            var results = new List<IListBlobItem>();
            var containerRef = GetContainerReference(container);

            do
            {
                var response = await containerRef.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, null, continuationToken, GetRequestOptions(), null);
                continuationToken = response.ContinuationToken;
                foreach (var listBlobItem in response.Results)
                {
                    if (listBlobItem is CloudBlob)
                        results.Add(listBlobItem);
                }
            } while (continuationToken != null);

            var dateTimeOffset = results.Where(x => x is CloudBlob).Max(x => ((CloudBlob)x).Properties.LastModified);

            return dateTimeOffset.GetValueOrDefault().UtcDateTime;
        }

        public async Task<Stream> GetAsync(string container, string key)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(container, key);
            
            var ms = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(ms, null, GetRequestOptions(), null);
            ms.Position = 0;
            return ms;
        }

        public async Task<string> GetAsTextAsync(string container, string key)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(container, key);
            return await blockBlob.DownloadTextAsync(null, GetRequestOptions(), null);
        }

        public string GetBlobUrl(string container, string key)
        {
            var blockBlob = GetBlockBlobReferenceAsync(container, key).GetAwaiter().GetResult();

            return blockBlob.Uri.AbsoluteUri;
        }

        [Obsolete("This method requires Uri to be present in prefix. Use GetListOfBlobKeysByPrefixAsync instead.")]
        public async Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
        {
            BlobContinuationToken continuationToken = null;
            var results = new List<string>();
            var containerRef = GetContainerReference(container);

            do
            {
                var response = await containerRef.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, null, continuationToken, GetRequestOptions(), null);
                continuationToken = response.ContinuationToken;
                foreach (var listBlobItem in response.Results)
                {
                    if (listBlobItem.Uri.ToString().StartsWith(prefix))
                        results.Add(listBlobItem.Uri.ToString());
                }
            } while (continuationToken != null);

            return results;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> is null or whitespace.</exception>
        public async Task DeleteBlobsByPrefixAsync(string container, string prefix)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentNullException(nameof(container));

            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));

            BlobContinuationToken continuationToken = null;
            var containerRef = GetContainerReference(container);

            do
            {
                var result = await containerRef.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.None, null,
                    continuationToken, GetRequestOptions(), null);

                continuationToken = result.ContinuationToken;

                await Task.WhenAll(result.Results
                    .Select(item => (item as CloudBlob)?.DeleteIfExistsAsync())
                    .Where(task => task != null)
                );

            } while (continuationToken != null);
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="container"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefix"/> is null or whitespace.</exception>
        public async Task<IEnumerable<string>> GetListOfBlobKeysByPrefixAsync(string container, string prefix, int? maxResultsCount = null)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentNullException(nameof(container));

            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentNullException(nameof(prefix));

            BlobContinuationToken continuationToken = null;
            var results = new List<string>();
            var containerRef = GetContainerReference(container);

            do
            {
                var maxCount = maxResultsCount.HasValue ? maxResultsCount.Value - results.Count : (int?) null;

                var result = await containerRef.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.None, maxCount,
                    continuationToken, GetRequestOptions(), null);

                results.AddRange(result.Results
                    .Select(item => (item as CloudBlob)?.Name)
                    .Where(item => item != null)
                );
                
                continuationToken = result.ContinuationToken;

            } while (continuationToken != null && (!maxResultsCount.HasValue || results.Count < maxResultsCount.Value));

            return results;
        }

        public async Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
        {
            var containerRef = GetContainerReference(container);

            BlobContinuationToken token = null;
            var results = new List<string>();
            do
            {
                var result = await containerRef.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, null, token, GetRequestOptions(), null);
                token = result.ContinuationToken;
                foreach (var listBlobItem in result.Results)
                {
                    results.Add(listBlobItem.Uri.ToString());
                }

                //Now do something with the blobs
            } while (token != null);

            return results;
        }

        public async Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null)
        {
            var containerRef = GetContainerReference(container);

            BlobContinuationToken token = null;
            var results = new List<string>();
            do
            {
                var result = await containerRef.ListBlobsSegmentedAsync(null, false, BlobListingDetails.None, maxResultsCount, token, GetRequestOptions(), null);
                token = result.ContinuationToken;
                foreach (var listBlobItem in result.Results.OfType<CloudBlockBlob>())
                {
                    results.Add(listBlobItem.Name);
                }

                //Now do something with the blobs
            } while (token != null && (maxResultsCount == null || results.Count <= maxResultsCount.Value));

            return results;
        }

        public async Task DelBlobAsync(string container, string key)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(container, key);
            await blockBlob.DeleteAsync(DeleteSnapshotsOption.None, null, GetRequestOptions(), null);
        }

        public Stream this[string container, string key]
        {
            get
            {
                var blockBlob = GetBlockBlobReferenceAsync(container, key).GetAwaiter().GetResult();
                var ms = new MemoryStream();
                blockBlob.DownloadToStreamAsync(ms, null, GetRequestOptions(), null).GetAwaiter().GetResult();
                ms.Position = 0;
                return ms;
            }
        }

        public async Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
        {
            var metadata = await GetMetadataAsync(container, key);
            if ((metadata?.Count ?? 0) == 0 || (!metadata?.ContainsKey(metaDataKey) ?? true))
                return null;

            return metadata[metaDataKey];
        }

        public async Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
        {
            if (string.IsNullOrWhiteSpace(container) || string.IsNullOrWhiteSpace(key))
                return new Dictionary<string, string>();

            if (!await HasBlobAsync(container, key))
                return new Dictionary<string, string>();

            var blockBlob = await GetBlockBlobReferenceAsync(container, key);
            await blockBlob.FetchAttributesAsync();

            return blockBlob.Metadata;
        }

        /// <inheritdoc />
        public async Task<BlobProperties> GetPropertiesAsync(string container, string key)
        {
            if (string.IsNullOrWhiteSpace(container))
                throw new ArgumentNullException(container);

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(key);              

            var blockBlob = await GetBlockBlobReferenceAsync(container, key);

            if (blockBlob == null)
                throw new BlobNotFoundException($"Blob with key: {key} not found in container: {container}.");

            await blockBlob.FetchAttributesAsync();

            return blockBlob.Properties;
        }
    }
}

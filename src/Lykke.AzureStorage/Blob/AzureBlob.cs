﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage.Blob.Decorators;
using JetBrains.Annotations;
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
            var blockBlob = await GetBlockBlobReferenceAsync(container, key, createIfNotExists: true);

            var mimeType = ContentTypesHelper.GetContentType(key);

            if (mimeType != null)
            {
                blockBlob.Properties.ContentType = mimeType;
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

        public async Task<Stream> GetAsync(string blobContainer, string key)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(blobContainer, key);
            
            var ms = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(ms, null, GetRequestOptions(), null);
            ms.Position = 0;
            return ms;
        }

        public async Task<string> GetAsTextAsync(string blobContainer, string key)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(blobContainer, key);
            return await blockBlob.DownloadTextAsync(null, GetRequestOptions(), null);
        }

        public string GetBlobUrl(string container, string key)
        {
            var blockBlob = GetBlockBlobReferenceAsync(container, key).GetAwaiter().GetResult();

            return blockBlob.Uri.AbsoluteUri;
        }

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

        public async Task DelBlobAsync(string blobContainer, string key)
        {
            var blockBlob = await GetBlockBlobReferenceAsync(blobContainer, key);
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
            if ((metadata?.Count ?? 0) == 0 || !metadata.ContainsKey(metaDataKey))
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
    }
}

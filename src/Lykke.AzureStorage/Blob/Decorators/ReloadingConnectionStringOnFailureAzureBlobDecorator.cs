using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob.Decorators
{
    /// <summary>
    /// Decorator, which adds reloading ConnectionString on authenticate failure to operations of <see cref="IBlobStorage"/> implementation
    /// </summary>
    internal class ReloadingConnectionStringOnFailureAzureBlobDecorator : ReloadingOnFailureDecoratorBase<IBlobStorage>, IBlobStorage
    {
        protected override Func<bool, Task<IBlobStorage>> MakeStorage { get; }

        public ReloadingConnectionStringOnFailureAzureBlobDecorator(Func<bool, Task<IBlobStorage>> makeStorage)
        {
            MakeStorage = makeStorage;
        }

        public Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false) 
            => WrapAsync(x => x.SaveBlobAsync(container, key, bloblStream, anonymousAccess));
        public Task SaveBlobAsync(string container, string key, byte[] blob)
            => WrapAsync(x => x.SaveBlobAsync(container, key, blob));

        public Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata)
            => WrapAsync(x => x.SaveBlobAsync(container, key, blob, metadata));

        public Task<bool> HasBlobAsync(string container, string key)
            => WrapAsync(x => x.HasBlobAsync(container, key));

        public Task<bool> CreateContainerIfNotExistsAsync(string container)
            => WrapAsync(x => x.CreateContainerIfNotExistsAsync(container));

        public Task<DateTime> GetBlobsLastModifiedAsync(string container)
            => WrapAsync(x => x.GetBlobsLastModifiedAsync(container));

        public Task<Stream> GetAsync(string container, string key)
            => WrapAsync(x => x.GetAsync(container, key));

        public Task<string> GetAsTextAsync(string container, string key)
            => WrapAsync(x => x.GetAsTextAsync(container, key));

        public string GetBlobUrl(string container, string key)
            => Wrap(x => x.GetBlobUrl(container, key));

        public Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
            => WrapAsync(x => x.FindNamesByPrefixAsync(container, prefix));

        public Task DeleteBlobsByPrefixAsync(string container, string prefix)
            => WrapAsync(x => x.DeleteBlobsByPrefixAsync(container, prefix));

        public Task<IEnumerable<string>> GetListOfBlobKeysByPrefixAsync(string container, string prefix,
                int? maxResultsCount = null)
            => WrapAsync(x => x.GetListOfBlobKeysByPrefixAsync(container, prefix, maxResultsCount));

        public Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
            => WrapAsync(x => x.GetListOfBlobsAsync(container));

        public Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null)
            => WrapAsync(x => x.GetListOfBlobKeysAsync(container, maxResultsCount));

        public Task DelBlobAsync(string container, string key)
            => WrapAsync(x => x.DelBlobAsync(container, key));

        public Stream this[string container, string key]
            => Wrap(x => x[container, key]);

        public Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
            => WrapAsync(x => x.GetMetadataAsync(container, key, metaDataKey));

        public Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
            => WrapAsync(x => x.GetMetadataAsync(container, key));

        public Task<List<string>> ListBlobsAsync(string container, string path)
            => WrapAsync(x => x.ListBlobsAsync(container, path));

        public Task<BlobProperties> GetPropertiesAsync(string container, string key)
            => WrapAsync(x => x.GetPropertiesAsync(container, key));

        public Task<string> AcquireLeaseAsync(string container, string key, TimeSpan? leaseTime, string proposedLeaseId = null)
            => WrapAsync(x => x.AcquireLeaseAsync(container, key, leaseTime, proposedLeaseId));

        public Task ReleaseLeaseAsync(string container, string key, string leaseId)
            => WrapAsync(x => x.ReleaseLeaseAsync(container, key, leaseId));

        public Task RenewLeaseAsync(string container, string key, string leaseId)
            => WrapAsync(x => x.RenewLeaseAsync(container, key, leaseId));

        public Task SetContainerPermissionsAsync(string container, BlobContainerPublicAccessType publicAccessType)
            => WrapAsync(x => x.SetContainerPermissionsAsync(container, publicAccessType));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureStorage.Blob.Decorators
{
    /// <summary>
    /// Decorator which explicitly calls AppInsights API to submit Azure Blob call events
    /// </summary>
    internal class ExplicitAppInsightsAzureBlobDecorator : ExplicitAppInsightsCallDecoratorBase, IBlobStorage
    {
        private readonly IBlobStorage _impl;

        protected override string TrackType => "Azure blob";

        internal ExplicitAppInsightsAzureBlobDecorator(IBlobStorage blobStorage)
        {
            _impl = blobStorage;
        }

        #region IQueueExt decoration

        public Stream this[string container, string key]
            => Wrap(() => _impl[container, key], container, "[container, key]");

        public async Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false)
            => await WrapAsync(async () => await _impl.SaveBlobAsync(container, key, bloblStream, anonymousAccess), container, "SaveBlobAsync from Stream");

        public async Task SaveBlobAsync(string container, string key, byte[] blob)
            => await WrapAsync(async () => await _impl.SaveBlobAsync(container, key, blob), container, "SaveBlobAsync from array");

        public async Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata)
            => await WrapAsync(async () => await _impl.SaveBlobAsync(container, key, blob, metadata), container, "SaveBlobAsync from array with metadata");
        
        public async Task<bool> HasBlobAsync(string container, string key)
            => await WrapAsync(async () => await _impl.HasBlobAsync(container, key), container);

        public async Task<bool> CreateContainerIfNotExistsAsync(string container)
            => await WrapAsync(async () => await _impl.CreateContainerIfNotExistsAsync(container), container);

        public async Task<DateTime> GetBlobsLastModifiedAsync(string container)
            => await WrapAsync(async () => await _impl.GetBlobsLastModifiedAsync(container), container);

        public async Task<Stream> GetAsync(string container, string key)
            => await WrapAsync(async () => await _impl.GetAsync(container, key), container);

        public async Task<string> GetAsTextAsync(string container, string key)
            => await WrapAsync(async () => await _impl.GetAsTextAsync(container, key), container);

        public string GetBlobUrl(string container, string key)
            => Wrap(() => _impl.GetBlobUrl(container, key), container);

        public async Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
            => await WrapAsync(async () => await _impl.FindNamesByPrefixAsync(container, prefix), container);

        public async Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
            => await WrapAsync(async () => await _impl.GetListOfBlobsAsync(container), container);

        public async Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null)
            => await WrapAsync(async () => await _impl.GetListOfBlobKeysAsync(container, maxResultsCount), container);

        public async Task DelBlobAsync(string container, string key)
            => await WrapAsync(async () => await _impl.DelBlobAsync(container, key), container);

        public async Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
            => await WrapAsync(async () => await _impl.GetMetadataAsync(container, key, metaDataKey), container, "GetMetadataAsync for 1 key");

        public async Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
            => await WrapAsync(async () => await _impl.GetMetadataAsync(container, key), container, "GetMetadataAsync for all keys");

        public async Task<List<string>> ListBlobsAsync(string container, string path)
            => await WrapAsync(async () => await _impl.ListBlobsAsync(container, path), container);

        #endregion
    }
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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

        public Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata = null)
            => WrapAsync(x => x.SaveBlobAsync(container, key, blob, metadata));

        public Task<bool> HasBlobAsync(string container, string key)
            => WrapAsync(x => x.HasBlobAsync(container, key));

        public Task<bool> CreateContainerIfNotExistsAsync(string container)
            => WrapAsync(x => x.CreateContainerIfNotExistsAsync(container));

        public Task<DateTime> GetBlobsLastModifiedAsync(string container)
            => WrapAsync(x => x.GetBlobsLastModifiedAsync(container));

        public Task<Stream> GetAsync(string blobContainer, string key)
            => WrapAsync(x => x.GetAsync(blobContainer, key));

        public Task<string> GetAsTextAsync(string blobContainer, string key)
            => WrapAsync(x => x.GetAsTextAsync(blobContainer, key));

        public string GetBlobUrl(string container, string key)
            => Wrap(x => x.GetBlobUrl(container, key));

        public Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
            => WrapAsync(x => x.FindNamesByPrefixAsync(container, prefix));

        public Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
            => WrapAsync(x => x.GetListOfBlobsAsync(container));

        public Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null)
            => WrapAsync(x => x.GetListOfBlobKeysAsync(container, maxResultsCount));

        public Task DelBlobAsync(string blobContainer, string key)
            => WrapAsync(x => x.DelBlobAsync(blobContainer, key));

        public Stream this[string container, string key]
            => Wrap(x => x[container, key]);

        public Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
            => WrapAsync(x => x.GetMetadataAsync(container, key, metaDataKey));

        public Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
            => WrapAsync(x => x.GetMetadataAsync(container, key));

        public Task<List<string>> ListBlobsAsync(string container, string path)
            => WrapAsync(x => x.ListBlobsAsync(container, path));
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob
{
    internal static class BlobInMemoryHelper
    {
        public static void AddOrReplace(this Dictionary<string, BlobAttributes> blob, string key, BlobAttributes data)
        {
            AddOrReplace<BlobAttributes>(blob, key, data);
        }

        public static void AddOrReplace(this Dictionary<string, byte[]> blob, string key, byte[] data)
        {
            AddOrReplace<byte[]>(blob, key, data);
        }

        private static void AddOrReplace<T>(IDictionary<string, T> blob, string key, T data)
        {
            if (blob.ContainsKey(key))
            {
                blob[key] = data;
                return;
            }


            blob.Add(key, data);
        }

        public static byte[] GetOrNull(this Dictionary<string, byte[]> blob, string key)
        {
            if (blob.ContainsKey(key))
                return blob[key];


            return null;
        }
    }

    internal class BlobAttributes
    {
        public IDictionary<string, string> Metadata { get; internal set; } = new Dictionary<string, string>();
    }


    public class AzureBlobInMemory : IBlobStorage
    {
        private readonly Dictionary<string, Dictionary<string, byte[]>> _blobs =
            new Dictionary<string, Dictionary<string, byte[]>>();

        private readonly Dictionary<string, Dictionary<string, BlobAttributes>> _blobsAttributes =
            new Dictionary<string, Dictionary<string, BlobAttributes>>();

        private readonly object _lockObject = new object();

        private Dictionary<string, byte[]> GetBlob(string container)
        {
            if (!_blobs.ContainsKey(container))
                _blobs.Add(container, new Dictionary<string, byte[]>());


            return _blobs[container];
        }

        private Dictionary<string, BlobAttributes> GetBlobAttributes(string container)
        {
            if (!_blobsAttributes.ContainsKey(container))
                _blobsAttributes.Add(container, new Dictionary<string, BlobAttributes>());


            return _blobsAttributes[container];
        }

        public void SaveBlob(string container, string key, Stream bloblStream)
        {
            lock (_lockObject)
                GetBlob(container).AddOrReplace(key, bloblStream.ToBytes());
        }       

	    public Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false)
	    {
			SaveBlob(container, key, bloblStream);
			return Task.FromResult(key);
		}

        public Task SaveBlobAsync(string container, string key, byte[] blob)
        {
            return SaveBlobAsync(container, key, blob, null);
        }

        public Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata)
        {
            lock (_lockObject)
            {
                GetBlob(container).AddOrReplace(key, blob);

                if (metadata == null) return Task.FromResult(0);
                var blobAttributes = new BlobAttributes
                {
                    Metadata = (IDictionary<string, string>) metadata
                };
                GetBlobAttributes(container).Add(key, blobAttributes);
            }

            return Task.FromResult(0);
        }

        public Task<bool> HasBlobAsync(string container, string key)
        {
            lock (_lockObject)
                return Task.Run(() => _blobs[container].ContainsKey(key));
        }

        public Task<DateTime> GetBlobsLastModifiedAsync(string container)
        {
            return Task.Run(() => DateTime.UtcNow);
        }

        public Stream this[string container, string key]
        {
            get
            {
                lock (_lockObject)
                    return GetBlob(container).GetOrNull(key).ToStream();
            }
        }

        public Task<Stream> GetAsync(string container, string key)
        {
            var result = this[container, key];
            return Task.FromResult(result);
        }

        public Task<string> GetAsTextAsync(string container, string key)
        {
            var result = this[container, key];
            using (var sr = new StreamReader(result))
            {
                return Task.FromResult(sr.ReadToEnd());
            }
        }

        public string GetBlobUrl(string container, string key)
        {
            return string.Empty;
        }

        public Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
        {
            lock (_lockObject)
                return Task.Run(() => GetBlob(container).Where(itm => itm.Key.StartsWith(prefix)).Select(itm => itm.Key));
        }

        public Task DeleteBlobsByPrefixAsync(string container, string prefix)
        {
            lock (_lockObject)
            {
                return Task.Run(() =>
                {
                    var blobs = GetBlob(container);
                    var keysToDelete = blobs
                        .Where(itm => itm.Key.StartsWith(prefix))
                        .Select(itm => itm.Key);

                    foreach (var key in keysToDelete) blobs.Remove(key);
                });
            }
        }

        public Task<IEnumerable<string>> GetListOfBlobKeysByPrefixAsync(string container, string prefix, int? maxResultsCount = null)
        {
            lock (_lockObject)
                return Task.Run(() =>
                {
                    var startWithPrefix = GetBlob(container).Where(itm => itm.Key.StartsWith(prefix));
                    startWithPrefix = maxResultsCount == null ? startWithPrefix : startWithPrefix.Take((int)maxResultsCount);
                    return startWithPrefix.Select(itm => itm.Key);
                });
        }

        public Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
        {
            lock (_lockObject)
                return Task.Run(() => GetBlob(container).Select(itm => itm.Key));
        }

        public Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null)
        {
            lock (_lockObject)
                return Task.Run(() =>
                {
                    var keys = GetBlob(container).Select(itm => itm.Key);
                    if (maxResultsCount.HasValue)
                        keys = keys.Take(maxResultsCount.Value);
                    return keys;
                });
        }

        public void DelBlob(string container, string key)
        {
            lock (_lockObject)
                GetBlob(container).Remove(key);
        }

        public Task DelBlobAsync(string container, string key)
        {
            DelBlob(container, key);
            return Task.FromResult(0);
        }

        public Task<bool> CreateContainerIfNotExistsAsync(string container)
        {
            if (!_blobs.ContainsKey(container))
            {
                _blobs.Add(container, new Dictionary<string, byte[]>());
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
        {
            lock (_lockObject)
            {
                if (!GetBlobAttributes(container).ContainsKey(key)) return Task.FromResult<string>(null);

                var metadata = GetBlobAttributes(container)[key].Metadata;
                return Task.FromResult<string>(metadata.ContainsKey(metaDataKey) ? metadata[metaDataKey] : null);
            }
        }

        public Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
        {
            return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());
        }

        public Task<List<string>> ListBlobsAsync(string container, string path)
        {
            throw new NotImplementedException();
        }

        public Task<BlobProperties> GetPropertiesAsync(string container, string key)
        {
            return Task.FromResult(new BlobProperties());
        }

        public Task<string> AcquireLeaseAsync(string container, string key, TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLeaseAsync(string container, string key, string leaseId)
        {
            return Task.CompletedTask;
        }

        public Task RenewLeaseAsync(string container, string key, string leaseId)
        {
            return Task.CompletedTask;
        }

        public Task SetContainerPermissionsAsync(string container, BlobContainerPublicAccessType publicAccessType)
        {
            return Task.CompletedTask;
        }
    }
}

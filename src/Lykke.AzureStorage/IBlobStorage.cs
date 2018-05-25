﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureStorage
{
    public interface IBlobStorage
    {
        /// <summary>Save binary stream to container.</summary>
        /// <param name="container">Container name</param>
        /// <param name="key">Key</param>
        /// <param name="bloblStream">Binary stream</param>
        /// <param name="anonymousAccess">Anonymous access</param>
        Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false);

        Task SaveBlobAsync(string container, string key, byte[] blob);

        /// <summary>
        /// Save array of data to container with metadata
        /// </summary>
        /// <param name="container">Container name</param>
        /// <param name="key">Key</param>
        /// <param name="blob">Array of data</param>
        /// <param name="metadata">Metadata to be saved additionally in blob storage</param>
        Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata);

        Task<bool> HasBlobAsync(string container, string key);

        Task<bool> CreateContainerIfNotExistsAsync(string container);

        /// <summary>Returns datetime of latest modification among all blobs</summary>
        Task<DateTime> GetBlobsLastModifiedAsync(string container);

        Task<Stream> GetAsync(string container, string key);
        Task<string> GetAsTextAsync(string container, string key);

        string GetBlobUrl(string container, string key);

        Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix);

        Task<IEnumerable<string>> GetListOfBlobsAsync(string container);
        Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null);

        Task DelBlobAsync(string container, string key);

        Stream this[string container, string key] { get; }

        Task<string> GetMetadataAsync(string container, string key, string metaDataKey);
        Task<IDictionary<string, string>> GetMetadataAsync(string container, string key);

        Task<List<string>> ListBlobsAsync(string container, string path);
    }
}

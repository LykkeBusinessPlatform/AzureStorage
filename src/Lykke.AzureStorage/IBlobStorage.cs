using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lykke.AzureStorage.Blob.Exceptions;
using Microsoft.WindowsAzure.Storage.Blob;

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

        [Obsolete("This method requires Uri to be present in prefix. Use GetListOfBlobKeysByPrefixAsync instead.")]
        Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix);

        /// <summary>
        ///     Delete all blobs inside <paramref name="container"/>, starting with <paramref name="prefix" />.
        ///     This method is useful for deleting folders.
        ///     Note: to delete folder, specify folder name as <paramref name="prefix" />.
        /// </summary>
        /// <param name="container">Container name.</param>
        /// <param name="prefix">
        ///     Blob prefix.
        ///     Note: it should not contain storage Uri. It is just a part of name.
        /// </param>
        /// <returns>Completed task if operation succeeded.</returns>
        Task DeleteBlobsByPrefixAsync(string container, string prefix);

        /// <summary>
        ///     Get all blob names inside <paramref name="container"/>, starting with <paramref name="prefix" />.
        ///     Note: to get all blob names in folder, specify folder name as <paramref name="prefix" />.
        /// </summary>
        /// <param name="container">Container name.</param>
        /// <param name="prefix"></param>
        ///     Blob prefix.
        ///     Note: it should not contain storage Uri. It is just a part of name.
        /// <param name="maxResultsCount">Maximum number of blobs to be returned.</param>
        /// <returns>Collection of blob names, starting with <paramref name="prefix" />.</returns>
        Task<IEnumerable<string>> GetListOfBlobKeysByPrefixAsync(string container, string prefix,
            int? maxResultsCount = null);


        Task<IEnumerable<string>> GetListOfBlobsAsync(string container);
        Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null);

        Task DelBlobAsync(string container, string key);

        Stream this[string container, string key] { get; }

        Task<string> GetMetadataAsync(string container, string key, string metaDataKey);
        Task<IDictionary<string, string>> GetMetadataAsync(string container, string key);

        Task<List<string>> ListBlobsAsync(string container, string path);

        /// <summary>Get blob properties.</summary>
        /// <param name="container">Container name.</param>
        /// <param name="key">Blob key.</param>
        /// <returns>
        ///      Returns <see cref="BlobProperties" /> object if blob exists.
        /// </returns>
        /// <exception cref="BlobNotFoundException">
        ///     Thrown when blob cannot be found by specified
        ///     <paramref name="container" /> and <paramref name="key" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="container" /> or <paramref name="key" /> is not provided.
        /// </exception>
        Task<BlobProperties> GetPropertiesAsync(string container, string key);
    }
}

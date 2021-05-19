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

        /// <summary>
        /// Creates blob container if it doesn't exist
        /// </summary>
        /// <param name="container">Container name</param>
        /// <returns>True if blob container was created, false otherwise</returns>
        Task<bool> CreateContainerIfNotExistsAsync(string container);

        /// <summary>
        /// Set public access permissions to blob container.
        /// </summary>
        /// <param name="container">Container name</param>
        /// <param name="publicAccessType">Public access type</param>
        Task SetContainerPermissionsAsync(string container, BlobContainerPublicAccessType publicAccessType);

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

        /// <summary>
        ///    Acquires blob lease. Creates empty blob if not exists.
        /// </summary>
        /// <param name="container">
        ///    Container name.
        /// </param>
        /// <param name="key">
        ///    Blob key.
        /// </param>
        /// <param name="leaseTime">
        ///    A <see cref="System.TimeSpan"/> representing the span of time for which to acquire the lease,
        ///    which will be rounded down to seconds. If <c>null</c>, an infinite lease will be acquired. If not null, this must be
        ///    15 to 60 seconds.
        /// </param>
        /// <param name="proposedLeaseId">
        ///    A string representing the proposed lease ID for the new lease, or <c>null</c> if no lease id is proposed.
        /// </param>
        /// <returns>
        ///    A <see cref="Task{T}"/> object that results in the id of the acquired lease.
        /// </returns>
        Task<string> AcquireLeaseAsync(string container, string key, TimeSpan? leaseTime, string proposedLeaseId = null);

        /// <summary>
        ///    Releases blob lease.
        /// </summary>
        /// <param name="container">
        ///    Container name.
        /// </param>
        /// <param name="key">
        ///    Blob key.
        /// </param>
        /// <param name="leaseId">
        ///    Lease id.
        /// </param>
        Task ReleaseLeaseAsync(string container, string key, string leaseId);
        
        /// <summary>
        ///    Renews blob lease.
        /// </summary>
        /// <param name="container">
        ///    Container name.
        /// </param>
        /// <param name="key">
        ///    Blob key.
        /// </param>
        /// <param name="leaseId">
        ///    Lease id.
        /// </param>
        Task RenewLeaseAsync(string container, string key, string leaseId);
    }
}

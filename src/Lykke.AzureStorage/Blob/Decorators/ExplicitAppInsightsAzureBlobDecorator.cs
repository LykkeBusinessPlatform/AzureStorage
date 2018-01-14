using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.ApplicationInsights;

namespace AzureStorage.Blob.Decorators
{
    /// <summary>
    /// Decorator which explicitly calls AppInsights API to submit Azure Blob call events
    /// </summary>
    internal class ExplicitAppInsightsAzureBlobDecorator : IBlobStorage
    {
        private const string _telemetryDependencyType = "Azure blob";
        private readonly IBlobStorage _impl;
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        internal ExplicitAppInsightsAzureBlobDecorator(IBlobStorage blobStorage)
        {
            _impl = blobStorage;
        }

        #region IQueueExt decoration

        public Stream this[string container, string key]
            => Wrap(() => _impl[container, key], container, "[container, key]");

        public async Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false)
            => await WrapAsync(async () => await _impl.SaveBlobAsync(container, key, bloblStream, anonymousAccess), container, nameof(SaveBlobAsync));

        public async Task SaveBlobAsync(string container, string key, byte[] blob)
            => await WrapAsync(async () => await _impl.SaveBlobAsync(container, key, blob), container, nameof(SaveBlobAsync));

        public async Task<bool> HasBlobAsync(string container, string key)
            => await WrapAsync(async () => await _impl.HasBlobAsync(container, key), container, nameof(HasBlobAsync));

        public async Task<bool> CreateContainerIfNotExistsAsync(string container)
            => await WrapAsync(async () => await _impl.CreateContainerIfNotExistsAsync(container), container, nameof(CreateContainerIfNotExistsAsync));

        public async Task<DateTime> GetBlobsLastModifiedAsync(string container)
            => await WrapAsync(async () => await _impl.GetBlobsLastModifiedAsync(container), container, nameof(GetBlobsLastModifiedAsync));

        public async Task<Stream> GetAsync(string container, string key)
            => await WrapAsync(async () => await _impl.GetAsync(container, key), container, nameof(GetAsync));

        public async Task<string> GetAsTextAsync(string container, string key)
            => await WrapAsync(async () => await _impl.GetAsTextAsync(container, key), container, nameof(GetAsTextAsync));

        public string GetBlobUrl(string container, string key)
            => Wrap(() => _impl.GetBlobUrl(container, key), container, nameof(GetBlobUrl));

        public async Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
            => await WrapAsync(async () => await _impl.FindNamesByPrefixAsync(container, prefix), container, nameof(FindNamesByPrefixAsync));

        public async Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
            => await WrapAsync(async () => await _impl.GetListOfBlobsAsync(container), container, nameof(GetListOfBlobsAsync));

        public async Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container)
            => await WrapAsync(async () => await _impl.GetListOfBlobKeysAsync(container), container, nameof(GetListOfBlobKeysAsync));

        public async Task DelBlobAsync(string container, string key)
            => await WrapAsync(async () => await _impl.DelBlobAsync(container, key), container, nameof(DelBlobAsync));

        public async Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
            => await WrapAsync(async () => await _impl.GetMetadataAsync(container, key, metaDataKey), container, nameof(GetMetadataAsync));

        public async Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
            => await WrapAsync(async () => await _impl.GetMetadataAsync(container, key), container, nameof(GetMetadataAsync));

        #endregion

        #region decoration logic

        private async Task<TResult> WrapAsync<TResult>(Func<Task<TResult>> func, string container, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                return await func();
            }
            catch (Exception e)
            {
                isSuccess = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackDependency(
                    _telemetryDependencyType,
                    container,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        private async Task WrapAsync(Func<Task> func, string container, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                await func();
            }
            catch (Exception e)
            {
                isSuccess = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackDependency(
                    _telemetryDependencyType,
                    container,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        private TResult Wrap<TResult>(Func<TResult> func, string container, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                return func();
            }
            catch (Exception e)
            {
                isSuccess = false;
                _telemetry.TrackException(e);
                throw;
            }
            finally
            {
                timer.Stop();
                _telemetry.TrackDependency(
                    _telemetryDependencyType,
                    container,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        #endregion
    }
}

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorage.Queue.Decorators
{
    /// <summary>
    /// Decorator which explicitly calls AppInsights API to submit Azure Queue call events
    /// </summary>
    internal class ExplicitAppInsightsAzureQueueDecorator : IQueueExt
    {
        private const string _telemetryDependencyType = "Azure queue";
        private readonly IQueueExt _impl;
        private readonly TelemetryClient _telemetry = new TelemetryClient();

        public string Name => _impl.Name;

        internal ExplicitAppInsightsAzureQueueDecorator(IQueueExt queue)
        {
            _impl = queue;
        }

        #region IQueueExt decoration

        public Task PutRawMessageAsync(string msg)
            => WrapAsync(() => _impl.PutRawMessageAsync(msg), nameof(PutRawMessageAsync));

        public Task<string> PutMessageAsync(object itm)
            => WrapAsync(() => _impl.PutMessageAsync(itm), nameof(PutMessageAsync));

        public Task<QueueData> GetMessageAsync()
            => WrapAsync(() => _impl.GetMessageAsync(), nameof(GetMessageAsync));

        public Task FinishMessageAsync(QueueData token)
            => WrapAsync(() => _impl.FinishMessageAsync(token), nameof(FinishMessageAsync));

        public Task<object[]> GetMessagesAsync(int maxCount)
            => WrapAsync(() => _impl.GetMessagesAsync(maxCount), nameof(GetMessagesAsync));

        public Task ClearAsync()
            => WrapAsync(() => _impl.ClearAsync(), nameof(ClearAsync));

        public void RegisterTypes(params QueueType[] type)
            => Wrap(() => _impl.RegisterTypes(type), nameof(RegisterTypes));

        public Task<CloudQueueMessage> GetRawMessageAsync(int visibilityTimeoutSeconds = 30)
            => WrapAsync(() => _impl.GetRawMessageAsync(visibilityTimeoutSeconds), nameof(GetRawMessageAsync));

        public Task FinishRawMessageAsync(CloudQueueMessage msg)
            => WrapAsync(() => _impl.FinishRawMessageAsync(msg), nameof(FinishRawMessageAsync));

        public Task ReleaseRawMessageAsync(CloudQueueMessage msg)
            => WrapAsync(() => _impl.ReleaseRawMessageAsync(msg), nameof(ReleaseRawMessageAsync));

        public Task<int?> Count()
            => WrapAsync(() => _impl.Count(), nameof(Count));

        #endregion

        #region decoration logic

        private async Task<TResult> WrapAsync<TResult>(Func<Task<TResult>> func, string callName)
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
                    _impl.Name,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        private async Task WrapAsync(Func<Task> func, string callName)
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
                    _impl.Name,
                    callName,
                    callName,
                    startTime,
                    timer.Elapsed,
                    null,
                    isSuccess);
            }
        }

        private void Wrap(Action func, string callName)
        {
            bool isSuccess = true;
            var startTime = DateTime.UtcNow;
            var timer = Stopwatch.StartNew();
            try
            {
                func();
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
                    _impl.Name,
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

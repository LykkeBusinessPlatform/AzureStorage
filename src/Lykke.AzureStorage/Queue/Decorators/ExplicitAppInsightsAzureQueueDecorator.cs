using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorage.Queue.Decorators
{
    /// <summary>
    /// Decorator which explicitly calls AppInsights API to submit Azure Queue call events
    /// </summary>
    internal class ExplicitAppInsightsAzureQueueDecorator : ExplicitAppInsightsCallDecoratorBase, IQueueExt
    {
        private readonly IQueueExt _impl;

        protected override string TrackType => "Azure queue";

        public string Name => _impl.Name;

        internal ExplicitAppInsightsAzureQueueDecorator(IQueueExt queue)
        {
            _impl = queue;
        }

        #region IQueueExt decoration

        public Task PutRawMessageAsync(string msg)
            => WrapAsync(() => _impl.PutRawMessageAsync(msg), Name);

        public Task PutRawMessageAsync(string msg, TimeSpan initialVisibilityDelay)
            => WrapAsync(() => _impl.PutRawMessageAsync(msg, initialVisibilityDelay), Name);

        public Task<string> PutMessageAsync(object itm)
            => WrapAsync(() => _impl.PutMessageAsync(itm), Name);

        public Task<string> PutMessageAsync(object itm, TimeSpan initialVisibilityDelay)
            => WrapAsync(() => _impl.PutMessageAsync(itm, initialVisibilityDelay), Name);

        public Task<QueueData> GetMessageAsync()
            => WrapAsync(() => _impl.GetMessageAsync(), Name);

        public Task FinishMessageAsync(QueueData token)
            => WrapAsync(() => _impl.FinishMessageAsync(token), Name);

        public Task<object[]> GetMessagesAsync(int maxCount)
            => WrapAsync(() => _impl.GetMessagesAsync(maxCount), Name);

        public Task ClearAsync()
            => WrapAsync(() => _impl.ClearAsync(), Name);

        public void RegisterTypes(params QueueType[] types)
            => Wrap(() => _impl.RegisterTypes(types), Name);

        public Task<CloudQueueMessage> GetRawMessageAsync(int visibilityTimeoutSeconds = 30)
            => WrapAsync(() => _impl.GetRawMessageAsync(visibilityTimeoutSeconds), Name);

        public Task FinishRawMessageAsync(CloudQueueMessage msg)
            => WrapAsync(() => _impl.FinishRawMessageAsync(msg), Name);

        public Task ReleaseRawMessageAsync(CloudQueueMessage msg)
            => WrapAsync(() => _impl.ReleaseRawMessageAsync(msg), Name);

        public Task<int?> Count()
            => WrapAsync(() => _impl.Count(), Name);

        #endregion
    }
}

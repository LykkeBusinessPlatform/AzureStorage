﻿using System;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorage.Queue.Decorators
{
    internal class ReloadingConnectionStringOnFailureAzureQueueDecorator : ReloadingOnFailureDecoratorBase<IQueueExt>, IQueueExt
    {
        protected override Func<bool, Task<IQueueExt>> MakeStorage { get; }

        public string Name => Wrap(x => x.Name);

        public ReloadingConnectionStringOnFailureAzureQueueDecorator(Func<bool, Task<IQueueExt>> makeStorage)
        {
            MakeStorage = makeStorage;
        }

        public Task PutRawMessageAsync(string msg)
            => WrapAsync(x => x.PutRawMessageAsync(msg));

        public Task<string> PutMessageAsync(object itm) 
            => WrapAsync(x => x.PutMessageAsync(itm));

        public Task<QueueData> GetMessageAsync()
            => WrapAsync(x => x.GetMessageAsync());

        public Task FinishMessageAsync(QueueData token)
            => WrapAsync(x => x.FinishMessageAsync(token));

        public Task<object[]> GetMessagesAsync(int maxCount) 
            => WrapAsync(x => x.GetMessagesAsync(maxCount));

        public Task ClearAsync() 
            => WrapAsync(x => x.ClearAsync());

        public void RegisterTypes(params QueueType[] types) 
            => Wrap(x => x.RegisterTypes(types));

        public Task<CloudQueueMessage> GetRawMessageAsync(int visibilityTimeoutSeconds = 30)
            => WrapAsync(x => x.GetRawMessageAsync(visibilityTimeoutSeconds));

        public Task FinishRawMessageAsync(CloudQueueMessage msg)
            => WrapAsync(x => x.FinishRawMessageAsync(msg));

        public Task ReleaseRawMessageAsync(CloudQueueMessage msg)
            => WrapAsync(x => x.ReleaseRawMessageAsync(msg));

        public Task<int?> Count() 
            => WrapAsync(x => x.Count());
    }
}

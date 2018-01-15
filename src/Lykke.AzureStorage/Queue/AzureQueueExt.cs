﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AzureStorage.Queue.Decorators;
using JetBrains.Annotations;
using Lykke.SettingsReader;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

using Newtonsoft.Json;

namespace AzureStorage.Queue
{
    [PublicAPI]
    public class AzureQueueExt : IQueueExt
    {
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();
        private readonly string _queueName;
        private readonly CloudStorageAccount _storageAccount;
        private bool _queueCreated;
        private readonly TimeSpan _maxExecutionTime;

        public string Name => _queueName;

        private AzureQueueExt(string conectionString, string queueName, TimeSpan? maxExecutionTimeout = null)
        {
            _storageAccount = CloudStorageAccount.Parse(conectionString);
            _queueName = queueName;
            
            _maxExecutionTime = maxExecutionTimeout.GetValueOrDefault(TimeSpan.FromSeconds(30));
        }

        public static IQueueExt Create(IReloadingManager<string> connectionStringManager, string queueName, TimeSpan? maxExecutionTimeout = null)
        {
            queueName = queueName.ToLower();

            NameValidator.ValidateQueueName(queueName);

            return new ReloadingConnectionStringOnFailureAzureQueueDecorator(
                async (bool reload) => new AzureQueueExt(
                    reload ? await connectionStringManager.Reload() : connectionStringManager.CurrentValue,
                    queueName,
                    maxExecutionTimeout)
            );
        }

        private async Task<CloudQueue> GetQueue()
        {
            if (_queueCreated)
                return CreateQueueReference();
            return await CreateQueueIfNotExists();
        }

        private async Task<CloudQueue> CreateQueueIfNotExists()
        {
            var queue = CreateQueueReference();

            await queue.CreateIfNotExistsAsync();

            _queueCreated = true;

            return queue;
        }

        private CloudQueue CreateQueueReference()
        {
            var queueClient = _storageAccount.CreateCloudQueueClient();
            return queueClient.GetQueueReference(_queueName);
        }

        private QueueRequestOptions GetRequestOptions()
        {
            return new QueueRequestOptions { MaximumExecutionTime = _maxExecutionTime };
        }

        public async Task<QueueData> GetMessageAsync()
        {
            var queue = await GetQueue();
            var msg = await queue.GetMessageAsync(null, GetRequestOptions(), null);

            if (msg == null)
                return null;

            return new QueueData
            {
                Token = msg,
                Data = DeserializeObject(msg.AsString)
            };
        }

        public async Task PutRawMessageAsync(string msg)
        {
            var queue = await GetQueue();
            await queue.AddMessageAsync(new CloudQueueMessage(msg), null, null, GetRequestOptions(), null);
        }

        public async Task FinishMessageAsync(QueueData token)
        {
            if (token.Token is CloudQueueMessage cloudQueueMessage)
            {
                var queue = await GetQueue();

                await queue.DeleteMessageAsync(cloudQueueMessage, GetRequestOptions(), null);
            }
        }
        
        public async Task<string> PutMessageAsync(object itm)
        {
            var msg = SerializeObject(itm);
            if (msg == null)
                return string.Empty;

            var queue = await GetQueue();

            await queue.AddMessageAsync(new CloudQueueMessage(msg), null, null, GetRequestOptions(), null);
            return msg;
        }

        public async Task<object[]> GetMessagesAsync(int maxCount)
        {
            var queue = await GetQueue();

            var messages = await queue.GetMessagesAsync(maxCount, null, GetRequestOptions(), null);

            var cloudQueueMessages = messages as CloudQueueMessage[] ?? messages.ToArray();
            foreach (var cloudQueueMessage in cloudQueueMessages)
                await queue.DeleteMessageAsync(cloudQueueMessage);

            return cloudQueueMessages
                .Select(message => DeserializeObject(message.AsString))
                .Where(itm => itm != null).ToArray();
        }

        public async Task ClearAsync()
        {
            var queue = await GetQueue();

            await queue.ClearAsync(GetRequestOptions(), null);
        }

        public void RegisterTypes(params QueueType[] types)
        {
            foreach (var type in types)
                _types.Add(type.Id, type.Type);
        }

        public async Task<CloudQueueMessage> GetRawMessageAsync(int visibilityTimeoutSeconds = 30)
        {
            var queue = await GetQueue();
            return await queue.GetMessageAsync(TimeSpan.FromSeconds(visibilityTimeoutSeconds), GetRequestOptions(), null);
        }

        public async Task FinishRawMessageAsync(CloudQueueMessage msg)
        {
            var queue = await GetQueue();
            await queue.DeleteMessageAsync(msg, GetRequestOptions(), null);
        }

        public async Task ReleaseRawMessageAsync(CloudQueueMessage msg)
        {
            var queue = await GetQueue();
            await queue.UpdateMessageAsync(msg, TimeSpan.Zero, MessageUpdateFields.Visibility, GetRequestOptions(), null);
        }

        private string SerializeObject(object itm)
        {
            var myType = itm.GetType();
            return
                (from tp in _types where tp.Value == myType select tp.Key + ":" + JsonConvert.SerializeObject(itm))
                    .FirstOrDefault();
        }

        private object DeserializeObject(string itm)
        {
            try
            {
                var i = itm.IndexOf(':');

                var typeStr = itm.Substring(0, i);

                if (!_types.ContainsKey(typeStr))
                    return null;

                var data = itm.Substring(i + 1, itm.Length - i - 1);

                return JsonConvert.DeserializeObject(data, _types[typeStr]);
            }
            catch
            {
                return null;
            }
        }

        public async Task<int?> Count()
        {
            var queue = await GetQueue();
            await queue.FetchAttributesAsync(GetRequestOptions(), null);
            return queue.ApproximateMessageCount;
        }
    }
}

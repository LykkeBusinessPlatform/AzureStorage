using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorage.Queue
{
    public class QueueData
    {
        public object Token { get; set; }
        public object Data { get; set; }
    }

    public class QueueType
    {
        public string Id { get; set; }
        public Type Type { get; set; }

        public static QueueType Create(string id, Type type)
        {
            return new QueueType
            {
                Id = id,
                Type = type
            };
        }
    }

    public interface IQueueExt
    {
        /// <summary>Queue name</summary>
        string Name { get; }

        /// <summary>
        ///    Adds message to the queue.
        /// </summary>
        Task PutRawMessageAsync(string msg);
        
        /// <summary>
        ///    Adds message to the queue with specified initial visibility delay.
        /// </summary>
        Task PutRawMessageAsync(string msg, TimeSpan initialVisibilityDelay);
		
        /// <summary>
        ///    Adds message to the queue.
        /// </summary>
        Task<string> PutMessageAsync(object itm);
        
        /// <summary>
        ///    Adds message to the queue with specified initial visibility delay.
        /// </summary>
        Task<string> PutMessageAsync(object itm, TimeSpan initialVisibilityDelay);

        Task<QueueData> GetMessageAsync();
        Task FinishMessageAsync(QueueData token);

        Task<object[]> GetMessagesAsync(int maxCount);

        Task ClearAsync();

        void RegisterTypes(params QueueType[] types);
	    Task<CloudQueueMessage> GetRawMessageAsync(int visibilityTimeoutSeconds = 30);
	    Task FinishRawMessageAsync(CloudQueueMessage msg);
		Task ReleaseRawMessageAsync(CloudQueueMessage msg);

        Task<int?> Count();
    }
}

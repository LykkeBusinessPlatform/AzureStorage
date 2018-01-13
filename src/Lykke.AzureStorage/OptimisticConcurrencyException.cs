using System;
using JetBrains.Annotations;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage
{
    /// <summary>
    /// Exception, that will be thrown in case of optimistic concurrency failure while updating data
    /// </summary>
    [PublicAPI]
    public class OptimisticConcurrencyException : Exception
    {
        public ITableEntity Entity { get; set; }

        public OptimisticConcurrencyException(ITableEntity entity, StorageException inner) :
            base(BuildMessage(entity), inner)
        {
            Entity = entity;
        }
        
        private static string BuildMessage(ITableEntity entity)
        {
            return $"Entity was changed by someone else.\r\n- Entity type: {entity.GetType().FullName}\r\n- PK: {entity.PartitionKey}\r\n- RK: {entity.RowKey}\r\n- ETag: {entity.ETag}\r\n- Timestamp: {entity.Timestamp}";
        }
    }
}

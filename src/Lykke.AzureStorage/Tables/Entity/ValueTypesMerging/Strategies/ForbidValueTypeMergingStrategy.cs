using System;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies
{
    internal class ForbidValueTypeMergingStrategy : IValueTypeMergingStrategy
    {
        public void MarkValueTypePropertyAsDirty(string propertyName)
        {
        }

        public void NotifyEntityWasRead()
        {
        }

        public void NotifyEntityWasWritten()
        {
        }
        public EntityProperty GetEntityProperty(
            AzureTableEntity entity, 
            EntityPropertyAccessor propertyAccessor, 
            bool isMergingOperation)
        {
            if (!(isMergingOperation && propertyAccessor.IsValueType))
            {
                return propertyAccessor.GetProperty(entity);
            }

            throw new InvalidOperationException($"Merging of value types is forbidden. If you added value type property and do merging operation not accidentally, select different value type merging strategy for the entity type {entity.GetType()}");
        }
    }
}

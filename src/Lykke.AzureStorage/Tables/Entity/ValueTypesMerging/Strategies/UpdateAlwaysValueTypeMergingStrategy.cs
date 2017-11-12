using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies
{
    internal class UpdateAlwaysValueTypeMergingStrategy : IValueTypeMergingStrategy
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
            return propertyAccessor.GetProperty(entity);
        }
    }
}

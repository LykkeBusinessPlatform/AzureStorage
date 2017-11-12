using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies
{
    /// <summary>
    /// Implementation should be thread safe
    /// </summary>
    internal interface IValueTypeMergingStrategy
    {
        void MarkValueTypePropertyAsDirty(string propertyName);
        void NotifyEntityWasRead();
        void NotifyEntityWasWritten();
        EntityProperty GetEntityProperty(
            AzureTableEntity entity, 
            EntityPropertyAccessor propertyAccessor, 
            bool isMergingOperation);

    }
}

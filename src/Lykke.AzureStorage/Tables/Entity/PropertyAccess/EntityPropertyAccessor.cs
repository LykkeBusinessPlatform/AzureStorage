using System;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess
{
    internal class EntityPropertyAccessor
    {
        public string PropertyName { get; }
        public bool IsValueType { get; }

        private Func<AzureTableEntity, object> Getter { get; }
        private Action<AzureTableEntity, object> Setter { get; }
        private IStorageValueConverter Converter { get; }

        public EntityPropertyAccessor(string propertyName, bool isValueType, Func<AzureTableEntity, object> getter, Action<AzureTableEntity, object> setter, IStorageValueConverter converter)
        {
            PropertyName = propertyName;
            IsValueType = isValueType;
            Getter = getter;
            Setter = setter;
            Converter = converter;
        }

        public void SetProperty(AzureTableEntity entity, EntityProperty entityProperty)
        {
            try
            {
                var value = entityProperty.PropertyAsObject;
                var convertedValue = value != null ? Converter.ConvertFromStorage(value) : null;

                Setter(entity, convertedValue);
            }
            catch (Exception ex)
            {
                ITableEntity tableEntity = entity;
                var message = $@"Failed to set property {PropertyName} to the instance of the entity {entity.GetType()}.
PartitionKey = '{tableEntity.PartitionKey}'
RowKey = '{tableEntity.RowKey}'
ETag = '{tableEntity.ETag}'
Timestamp = '{tableEntity.Timestamp}'";

                throw new InvalidOperationException(message, ex);
            }
        }

        public EntityProperty GetProperty(AzureTableEntity entity)
        {
            try
            {
                var value = Getter(entity);
                var convertedValue = value != null ? Converter.ConvertFromEntity(value) : null;

                return EntityProperty.CreateEntityPropertyFromObject(convertedValue);
            }
            catch (Exception ex)
            {
                ITableEntity tableEntity = entity;
                var message = $@"Failed to get property {PropertyName} from the instance of the entity {entity.GetType()}.
PartitionKey = '{tableEntity.PartitionKey}'
RowKey = '{tableEntity.RowKey}'
ETag = '{tableEntity.ETag}'
Timestamp = '{tableEntity.Timestamp}'";

                throw new InvalidOperationException(message, ex);
            }
        }
    }
}

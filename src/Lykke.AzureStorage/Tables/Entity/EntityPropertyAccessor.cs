using System;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity
{
    internal class EntityPropertyAccessor
    {
        public string PropertyName { get; }

        private Func<object, object> Getter { get; }
        private Action<object, object> Setter { get; }
        private IStorageValueConverter Converter { get; }

        public EntityPropertyAccessor(string propertyName, Func<object, object> getter, Action<object, object> setter, IStorageValueConverter converter)
        {
            PropertyName = propertyName;
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

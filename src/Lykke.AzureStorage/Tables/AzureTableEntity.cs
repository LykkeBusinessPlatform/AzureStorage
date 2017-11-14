using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables
{
    /// <summary>
    /// Base Azure Table Entity class, which supports all builtin .net primitive types, 
    /// all types with the <see cref="TypeConverter"/> and user types with specified <see cref="IStorageValueSerializer"/>
    /// TODO: link to docs
    /// TODO: About value types merging suport
    /// </summary>
    [PublicAPI]
    public abstract class AzureTableEntity : ITableEntity
    {
        string ITableEntity.PartitionKey { get; set; }
        string ITableEntity.RowKey { get; set; }
        DateTimeOffset ITableEntity.Timestamp { get; set; }
        string ITableEntity.ETag { get; set; }

        void ITableEntity.ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            foreach(var propertyAccessor in EntityPropertyAccessorsManager.Instance.GetPropertyAccessors(GetType()))
            {
                // Property exists in the object, but ommited in the storage. Probaby it's new property,
                // so just ignore it, to support old data loading.

                if (!properties.TryGetValue(propertyAccessor.PropertyName, out var entityProperty))
                {
                    continue;
                }

                propertyAccessor.SetProperty(this, entityProperty);
            }
        }

        IDictionary<string, EntityProperty> ITableEntity.WriteEntity(OperationContext operationContext)
        {
            return EntityPropertyAccessorsManager
                .Instance
                .GetPropertyAccessors(GetType())
                .ToDictionary(
                    a => a.PropertyName, 
                    a => a.GetProperty(this));
        }

        protected void MarkValueTypePropertyAsDirty(string propertyName)
        {
            throw new NotImplementedException("Not yet");
        }
    }
}

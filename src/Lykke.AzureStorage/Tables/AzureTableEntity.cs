using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables
{
    /// <summary>
    /// <p>
    /// Base Azure Table Entity class, which supports all builtin .net primitive types, 
    /// all types with the <see cref="TypeConverter"/> and user types with specified <see cref="IStorageValueSerializer"/>.
    /// </p>
    /// <p>
    /// Also, value type properties merging is supported
    /// </p>
    /// <p>
    /// Read https://github.com/LykkeCity/AzureStorage/blob/master/README.md for more usage information
    /// </p>
    /// </summary>
    [PublicAPI]
    public abstract class AzureTableEntity : ITableEntity
    {
        internal const string MergingOperationContextHeader = "_AzureTableEntity.MergingOperation";

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public string ETag { get; set; }

        private readonly IValueTypeMergingStrategy _valueTypeMergingStrategy;

        protected AzureTableEntity()
        {
            _valueTypeMergingStrategy = EntityValueTypeMergingStrategiesManager.Instance.GetStrategy(GetType());
        }

        void ITableEntity.ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
        {
            foreach (var propertyAccessor in EntityPropertyAccessorsManager.Instance.GetPropertyAccessors(GetType()))
            {
                // Property exists in the object, but ommited in the storage. Probaby it's new property,
                // so just ignore it, to support old data loading.

                if (!properties.TryGetValue(propertyAccessor.PropertyName, out var entityProperty))
                {
                    continue;
                }

                propertyAccessor.SetProperty(this, entityProperty);
            }

            _valueTypeMergingStrategy.NotifyEntityWasRead();
        }

        IDictionary<string, EntityProperty> ITableEntity.WriteEntity(OperationContext operationContext)
        {
            var isMergingOperation = operationContext.UserHeaders?.ContainsKey(MergingOperationContextHeader) == true;

            if (isMergingOperation)
            {
                operationContext.UserHeaders.Remove(MergingOperationContextHeader);
            }

            var entityProperties = EntityPropertyAccessorsManager
                .Instance
                .GetPropertyAccessors(GetType())
                .ToDictionary(
                    accessor => accessor.PropertyName,
                    accessor => _valueTypeMergingStrategy.GetEntityProperty(this, accessor, isMergingOperation));

            _valueTypeMergingStrategy.NotifyEntityWasWritten();

            return entityProperties;
        }

        /// <summary>
        /// Call this method and pass <code>nameof(YourProperty)</code> as the <paramref name="propertyName"/>,
        /// when setter of the value type property is called and the value is changed.
        /// Method is thread safe.
        /// </summary>
        /// <param name="propertyName">The name of the property that was changed.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="propertyName"/> is <c>null</c>.</exception>
        protected void MarkValueTypePropertyAsDirty([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            _valueTypeMergingStrategy.MarkValueTypePropertyAsDirty(propertyName);
        }
    }
}

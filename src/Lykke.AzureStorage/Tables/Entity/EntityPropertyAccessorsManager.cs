using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity
{
    internal class EntityPropertyAccessorsManager : IEntityPropertyAccessorsManager
    {
        public static IEntityPropertyAccessorsManager Instance { get; }

        private readonly IEntityPropertyAccessorsFactory _propertyAccessorsFactory;
        private readonly ConcurrentDictionary<Type, EntityPropertyAccessor[]> _propertyAccessors;

        static EntityPropertyAccessorsManager()
        {
            Instance = new EntityPropertyAccessorsManager(
                new EntityPropertyAccessorsFactory(
                    new PropertyGettersFactory(),
                    new PropertySettersFactory(),
                    new StorageValueConvertersFactory(EntityMetamodel.Instance)));
        }

        public EntityPropertyAccessorsManager(IEntityPropertyAccessorsFactory propertyAccessorsFactory)
        {
            _propertyAccessorsFactory = propertyAccessorsFactory;
            _propertyAccessors = new ConcurrentDictionary<Type, EntityPropertyAccessor[]>();
        }

        public IEnumerable<EntityPropertyAccessor> GetPropertyAccessors(Type type)
        {
            return _propertyAccessors.GetOrAdd(type, t =>
            {
                if (!typeof(AzureTableEntity).IsAssignableFrom(type))
                {
                    throw new InvalidOperationException($"Type {type} should be descendant of the {nameof(AzureTableEntity)}");
                }

                return type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => !ShouldSkipProperty(p))
                    .Select(_propertyAccessorsFactory.Create)
                    .ToArray();
            });
        }

        private static bool ShouldSkipProperty(PropertyInfo property)
        {
            if(!property.CanRead || !property.CanWrite)
            {
                return true;
            }
            
            switch (property.Name)
            {
                case nameof(ITableEntity.PartitionKey):
                case nameof(ITableEntity.RowKey):
                case nameof(ITableEntity.Timestamp):
                case nameof(ITableEntity.ETag):
                    return true;
            }

            if (property.GetCustomAttribute<IgnorePropertyAttribute>() != null)
            {
                return true;
            }

            return false;
        }
    }
}

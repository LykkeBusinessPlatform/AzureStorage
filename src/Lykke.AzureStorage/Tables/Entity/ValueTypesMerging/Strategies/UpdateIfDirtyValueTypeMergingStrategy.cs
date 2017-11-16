using System.Collections.Concurrent;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies
{
    internal class UpdateIfDirtyValueTypeMergingStrategy : IValueTypeMergingStrategy
    {
        // ConcurrentDictionary is recommended by MS as the HashSet substitution, when thread safe is required
        private readonly ConcurrentDictionary<string, byte> _dirtyProperties;

        public UpdateIfDirtyValueTypeMergingStrategy()
        {
            _dirtyProperties = new ConcurrentDictionary<string, byte>();
        }

        public void MarkValueTypePropertyAsDirty(string propertyName)
        {
            _dirtyProperties.TryAdd(propertyName, default(byte));
        }

        public void NotifyEntityWasRead()
        {
            _dirtyProperties.Clear();
        }

        public void NotifyEntityWasWritten()
        {
            _dirtyProperties.Clear();
        }

        public EntityProperty GetEntityProperty(
            AzureTableEntity entity, 
            EntityPropertyAccessor propertyAccessor,
            bool isMergingOperation)
        {
            if (!(isMergingOperation && propertyAccessor.IsValueType))
            {
                // It's not a merging operation or it's a reference type property

                return propertyAccessor.GetProperty(entity);
            } 

            // It's merging operation and it's value type property

            if (_dirtyProperties.ContainsKey(propertyAccessor.PropertyName))
            {
                // Property was changed, so update it in the storage

                return propertyAccessor.GetProperty(entity);
            }

            // Property wasn't changed, so preserve storage value

            return null;
        }
    }
}

using System;
using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel
{
    internal class EntityMetamodelImpl : IEntityMetamodel
    {
        public static readonly EntityMetamodelImpl Empty = new EntityMetamodelImpl(new ImperativeMetamodelProvider());

        private readonly IMetamodelProvider _provider;

        public EntityMetamodelImpl(IMetamodelProvider provider)
        {
            _provider = provider;
        }

        public IStorageValueSerializer TryGetSerializer(PropertyInfo property)
        {
            try
            {
                // Serializer defined on the property, overrides that one, defined on the type, which property has

                return _provider.TryGetPropertySerializer(property) ??
                       _provider.TryGetTypeSerializer(property.PropertyType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get storage value serializer for property {property.DeclaringType}.{property.Name}, in context of type {property.ReflectedType}", ex);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

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
                       _provider.TryGetTypeSerializer(property.PropertyType) ??
                       TryGetCollectionSerializer(property.PropertyType) ??
                       TryGetNullableSerializer(property.PropertyType);

            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get storage value serializer for property {property.DeclaringType}.{property.Name}, in context of type {property.ReflectedType}", ex);
            }
        }

        public ValueTypeMergingStrategy? TryGetValueTypeMergingStrategy(Type type)
        {
            try
            {
                return _provider.TryGetValueTypeMergingStrategy(type);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get value type merging strategy factory for entity type {type}", ex);
            }
        }

        private IStorageValueSerializer TryGetCollectionSerializer(Type collectionType)
        {
            var enumerbleGenericArguments = GetGenericArgumentsOfAssignableType(collectionType, typeof(IEnumerable<>));
            if (enumerbleGenericArguments != null)
            {
                var elementType = enumerbleGenericArguments.First();

                return _provider.TryGetTypeSerializer(elementType);
            }

            if (collectionType.IsArray)
            {
                var elementType = collectionType.GetElementType();

                return _provider.TryGetTypeSerializer(elementType);
            }

            return null;
        }

        private IStorageValueSerializer TryGetNullableSerializer(Type nullableType)
        {
            var nullableGenericArguments = GetGenericArgumentsOfAssignableType(nullableType, typeof(Nullable<>));
            if (nullableGenericArguments != null)
            {
                var elementType = nullableGenericArguments.First();

                return _provider.TryGetTypeSerializer(elementType);
            }

            return null;
        }

        /// <summary>
        /// Returns generic arguments of type <paramref name="genericType"/>, 
        /// if <paramref name="givenType"/> is assignable to the <paramref name="genericType"/>,
        /// or null
        /// </summary>
        private static Type[] GetGenericArgumentsOfAssignableType(Type givenType, Type genericType)
        {
            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            {
                return givenType.GenericTypeArguments;
            }

            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                {
                    return it.GetGenericArguments();
                }
            }

            var baseType = givenType.BaseType;
            if (baseType == null)
            {
                return null;
            }

            return GetGenericArgumentsOfAssignableType(baseType, genericType);
        }
    }
}

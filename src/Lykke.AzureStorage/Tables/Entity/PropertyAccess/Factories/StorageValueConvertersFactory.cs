using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal class StorageValueConvertersFactory : IStorageValueConvertersFactory
    {
        private static readonly ISet<Type> TypesWhichAzureKnows = new HashSet<Type>
        {
            typeof(string),
            typeof(byte[]),
            typeof(bool),
            typeof(bool?),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(DateTimeOffset),
            typeof(DateTimeOffset?),
            typeof(double),
            typeof(double?),
            typeof(Guid),
            typeof(Guid?),
            typeof(int),
            typeof(int?),
            typeof(long),
            typeof(long?)
        };

        private static readonly PassThroughStorageValueConverter PassThroughStorageValueConverter;
        
        private readonly IEntityMetamodel _metamodel;

        public StorageValueConvertersFactory(IEntityMetamodel metamodel)
        {
            _metamodel = metamodel;
        }

        static StorageValueConvertersFactory()
        {
            PassThroughStorageValueConverter = new PassThroughStorageValueConverter();
        }

        public IStorageValueConverter Create(PropertyInfo property)
        {
            var propertyType = property.PropertyType;

            // If type is known for Azure use PassThroughtConverter
            if (TypesWhichAzureKnows.Contains(propertyType))
            {
                return PassThroughStorageValueConverter;
            }

            // If serializer is assigned in metamodel, use SerializerConverter
            var serializer = _metamodel.TryGetSerializer(property);
            if (serializer != null)
            {
                return new SerializerStorageValueConverter(serializer);
            }

            // If type has not default TypeConverter, which can converts to/from the string, use TypeDescriptorConverter
            var typeConverter = TypeDescriptor.GetConverter(propertyType);
            if (typeConverter.GetType() != typeof(TypeConverter) &&
                typeConverter.CanConvertFrom(typeof(string)) &&
                typeConverter.CanConvertTo(typeof(string)))
            {
                return new TypeDescriptorStringStorageValueConverter(typeConverter);
            }

            // No way to convert value is found
            var message =
                $@"Don't know how to convert property 
{property.DeclaringType.FullName}.{property.Name} of the type 
{propertyType.FullName} while initializing entity of the type
{property.ReflectedType.FullName}. 
Specified type is niether directly supported by the Azure Table Storage, nor the {typeof(IStorageValueSerializer).FullName} is specified to the type or the propery, nor the {typeof(TypeConverter).FullName} is specified for the type.
Please, specify {nameof(IStorageValueSerializer)} to the type or the property, using {typeof(ValueSerializerAttribute).FullName} or ";

            throw new InvalidOperationException(message);
        }
    }
}

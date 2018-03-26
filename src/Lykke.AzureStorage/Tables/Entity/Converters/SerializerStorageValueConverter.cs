﻿using System;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Converters
{
    internal sealed class SerializerStorageValueConverter : IStorageValueConverter
    {
        private readonly IStorageValueSerializer _serializer;
        private readonly Type _propertyType;

        public SerializerStorageValueConverter(IStorageValueSerializer serializer, Type propertyType)
        {
            _serializer = serializer;
            _propertyType = propertyType;
        }

        public object ConvertFromStorage(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value is string s)
            {
                var deserialized = _serializer.Deserialize(s, _propertyType);
                if (deserialized == null)
                {
                    throw new InvalidOperationException($"{nameof(IStorageValueSerializer.Deserialize)} shouldn't return null");
                }

                return deserialized;
            }

            throw new ArgumentException("Value should be a string", nameof(value));
        }

        public object ConvertFromEntity(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var serialized = _serializer.Serialize(value, _propertyType);
            if (serialized == null)
            {
                throw new InvalidOperationException($"{nameof(IStorageValueSerializer.Serialize)} shouldn't return null");
            }

            return serialized;
        }
    }
}

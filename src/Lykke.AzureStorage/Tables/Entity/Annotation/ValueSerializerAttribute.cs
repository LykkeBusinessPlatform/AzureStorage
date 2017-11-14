using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Annotation
{
    /// <summary>
    /// Marks an user type or property of the entity to serialize it using specified user implementation 
    /// of the <see cref="IStorageValueSerializer"/>, when reading or writing entity 
    /// from or to the AzureTable Storage
    /// </summary>
    [PublicAPI]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Property)]
    public class ValueSerializerAttribute : Attribute
    {
        /// <summary>
        /// Serializer type which implements <see cref="IStorageValueSerializer"/>
        /// </summary>
        public Type SerializerType { get; }

        /// <summary>
        /// Marks an user type or property of the entity to serialize it using specified user implementation 
        /// of the <see cref="IStorageValueSerializer"/>, when reading or writing entity 
        /// from or to the AzureTable Storage
        /// </summary>
        public ValueSerializerAttribute(Type serializerType)
        {
            SerializerType = serializerType ?? throw new ArgumentNullException(nameof(serializerType));
        }
    }
}

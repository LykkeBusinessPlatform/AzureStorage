using System;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Metamodel.Providers
{
    /// <summary>
    /// Metamodel provider abstraction.
    /// Implementations should provides entities metamodel, which contains
    /// specific <see cref="IStorageValueSerializer"/> serializer for given user type,
    /// or particular entity property
    /// </summary>
    [PublicAPI]
    public interface IMetamodelProvider
    {
        /// <summary>
        /// Implementation should returns specific <see cref="IStorageValueSerializer"/> instance
        /// for the given <paramref name="type"/>, or null if no serializer is specified 
        /// for the given <paramref name="type"/>
        /// </summary>
        /// <param name="type">User type, which infrastructure wants to serialize or deserialize</param>
        /// <returns>Serializer instance or null</returns>
        IStorageValueSerializer TryGetTypeSerializer(Type type);

        /// <summary>
        /// <p>
        /// Implementation should returns specific <see cref="IStorageValueSerializer"/> instance
        /// for the given <paramref name="propertyInfo"/>, or null if no serializer is specified 
        /// for the given <paramref name="propertyInfo"/>. 
        /// </p>
        /// <p>
        /// Implementation shouldn't return serializer for the type, but only serializer, 
        /// which is specified for the property itself
        /// </p>
        /// </summary>
        /// <param name="propertyInfo">Entity property, which infrastructure wants to serialize or deserialize</param>
        /// <returns>Serializer instance or null</returns>
        IStorageValueSerializer TryGetPropertySerializer(PropertyInfo propertyInfo);
    }
}

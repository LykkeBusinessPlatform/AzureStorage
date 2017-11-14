using JetBrains.Annotations;

namespace Lykke.AzureStorage.Tables.Entity.Serializers
{
    /// <summary>
    /// Abstraction of the serializer of the user types, when persisting they 
    /// as the part of the table entity <see cref="AzureTableEntity"/>
    /// </summary>
    [PublicAPI]
    public interface IStorageValueSerializer
    {
        /// <summary>
        /// Implementation should serialize the <paramref name="value"/> to the string and return it.
        /// </summary>
        /// <param name="value">Value to serialize. Can't be null</param>
        /// <returns>Serialized <paramref name="value"/> representation of the value. Can't be null</returns>
        string Serialize(object value);

        /// <summary>
        /// Implementation should deserialize the value from the string <paramref name="serialized"/> to the original type value and return it
        /// </summary>
        /// <param name="serialized">Serialized representation of the value. Can't be null</param>
        /// <returns>Original type value. Can't be null</returns>
        object Deserialize(string serialized);
    }
}

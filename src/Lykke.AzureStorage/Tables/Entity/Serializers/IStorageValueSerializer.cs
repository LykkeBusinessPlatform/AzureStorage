using JetBrains.Annotations;

namespace Lykke.AzureStorage.Tables.Entity.Serializers
{
    /// <summary>
    /// Abstraction of the user types serializer, used when persisting user types 
    /// as the part of the table entity <see cref="AzureTableEntity"/>
    /// </summary>
    [PublicAPI]
    public interface IStorageValueSerializer
    {
        /// <summary>
        /// <p>
        /// Implementation should serialize the <paramref name="value"/> to the string and return it.
        /// </p>
        /// <p>
        /// If implementation is inteded for single type T, it should at least support type T, IEnumerable&lt;T&gt; and Nullable&lt;T&gt; 
        /// being passed to the <paramref name="value"/>
        /// </p>
        /// </summary>
        /// <param name="value">Value to serialize. Can't be null</param>
        /// <returns>Serialized <paramref name="value"/> representation of the value. Can't be null</returns>
        string Serialize(object value);

        /// <summary>
        /// <p>
        /// Implementation should deserialize the value from the string <paramref name="serialized"/> to the original type value and return it
        /// </p>
        /// <p>
        /// If implementation is inteded for single type T, it should at least support serialized representations of 
        /// type T, IEnumerable&lt;T&gt; and Nullable&lt;T&gt; being passed to the <paramref name="serialized"/>
        /// </p>
        /// </summary>
        /// <param name="serialized">Serialized representation of the value. Can't be null</param>
        /// <returns>Original type value. Can't be null</returns>
        object Deserialize(string serialized);
    }
}

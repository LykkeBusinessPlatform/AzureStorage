using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Tables.Entity.Annotation
{
    /// <summary>
    /// Marks an user type or property of the entity to serialize it using Newtonsoft.Json serializer,
    /// when reading or writing entity from or to the AzureTable Storage
    /// </summary>
    [PublicAPI]
    public class JsonValueSerializerAttribute : ValueSerializerAttribute
    {
        /// <summary>
        /// Marks the entity type or property of the entity to serialize it using Newtonsoft.Json serializer,
        /// when reading or writing entity from or to the AzureTable Storage
        /// </summary>
        public JsonValueSerializerAttribute() : 
            base(typeof(JsonStorageValueSerializer))
        {
        }
    }
}

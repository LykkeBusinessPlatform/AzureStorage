using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Lykke.AzureStorage.Tables.Entity.Serializers
{
    /// <summary>
    /// Json serializer of the user types, when persisting they 
    /// as the part of the table entity <see cref="AzureTableEntity"/>
    /// </summary>
    [PublicAPI]
    public class JsonStorageValueSerializer : IStorageValueSerializer
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Json serializer of the user types, when persisting they 
        /// as the part of the table entity <see cref="AzureTableEntity"/>
        /// </summary>
        public JsonStorageValueSerializer()
        {
        }
        
        /// <summary>
        /// Json serializer of the user types, when persisting they 
        /// as the part of the table entity <see cref="AzureTableEntity"/>
        /// </summary>
        public JsonStorageValueSerializer(JsonSerializerSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc />
        public string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        /// <inheritdoc />
        public object Deserialize(string serialized)
        {
            return JsonConvert.DeserializeObject(serialized, _settings);
        }
    }
}

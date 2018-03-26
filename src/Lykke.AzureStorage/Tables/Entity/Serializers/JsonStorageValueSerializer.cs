using System;
using System.IO;
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
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Json serializer of the user types, when persisting they 
        /// as the part of the table entity <see cref="AzureTableEntity"/>
        /// </summary>
        public JsonStorageValueSerializer() :
            this(null)
        {
        }

        /// <summary>
        /// Json serializer of the user types, when persisting they 
        /// as the part of the table entity <see cref="AzureTableEntity"/>
        /// </summary>
        public JsonStorageValueSerializer(JsonSerializerSettings settings)
        {
            _serializer = JsonSerializer.Create(settings);
        }

        /// <inheritdoc />
        public string Serialize(object value, Type type)
        {
            using (var stringWriter = new StringWriter())
            {
                _serializer.Serialize(stringWriter, value, type);

                stringWriter.Flush();

                return stringWriter.ToString();
            }
        }

        /// <inheritdoc />
        public object Deserialize(string serialized, Type type)
        {
            using (var stringReader = new StringReader(serialized))
            {
                return _serializer.Deserialize(stringReader, type);
            }
        }
    }
}

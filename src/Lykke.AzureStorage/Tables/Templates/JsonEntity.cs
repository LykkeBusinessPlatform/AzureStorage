using System;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace AzureStorage.Tables.Templates
{
    /// <summary>
    /// Используем для сохранения сложный объектов (с листами, с объектами)
    /// </summary>
    /// <typeparam name="T">Тип, который сохраняем</typeparam>
    [Obsolete("Use AzureTableEntity. Will be removed in the future releases")]
    public class JsonTableEntity<T> : TableEntity 
    {
        private T _instance;

        public string Data
        {
            get => JsonConvert.SerializeObject(_instance);
            set => _instance = JsonConvert.DeserializeObject<T>(value);
        }
    }
}

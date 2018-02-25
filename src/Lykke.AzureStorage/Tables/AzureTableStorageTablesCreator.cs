using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage.Tables
{
    /// <summary>
    /// This class is extracted from the <see cref="AzureTableStorage{T}"/>
    /// due to static fields in generic class peculiarity
    /// </summary>
    internal static class AzureTableStorageTablesCreator
    {
        private static readonly ConcurrentDictionary<string, byte> CreatedTables;

        static AzureTableStorageTablesCreator()
        {
            CreatedTables = new ConcurrentDictionary<string, byte>();
        }

        public static async Task EnsureTableIsCreatedAsync(CloudTable table)
        {
            if (CreatedTables.TryAdd(table.Name, default(byte)))
            {
                await table.CreateIfNotExistsAsync();
            }
        }

        public static void InvalidateCreationCache(CloudTable table)
        {
            CreatedTables.TryRemove(table.Name, out var _);
        }
    }
}

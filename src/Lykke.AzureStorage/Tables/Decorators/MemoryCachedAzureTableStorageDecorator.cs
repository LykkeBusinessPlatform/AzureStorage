using Common.Log;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    ///  Cached <see cref="INoSQLTableStorage{T}"/> decorator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class MemoryCachedAzureTableStorageDecorator<T> : CachedAzureTableStorageDecorator<T> where T : class, ITableEntity, new()
    {

        public MemoryCachedAzureTableStorageDecorator(INoSQLTableStorage<T> table, ILog log)
        : base(table, new NoSqlTableInMemory<T>(), log)
        {
        }
    }
}

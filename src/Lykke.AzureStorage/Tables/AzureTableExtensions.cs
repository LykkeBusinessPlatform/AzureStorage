using Microsoft.WindowsAzure.Storage.Table;
using AzureStorage.Tables.Decorators;

namespace AzureStorage.Tables
{
    public static class AzureTableExtensions
    {
        public static INoSQLTableStorage<TEntity> UseExplicitAppInsightsSubmit<TEntity>(this INoSQLTableStorage<TEntity> tableStorage)
            where TEntity : ITableEntity, new()
        {
            return new ExplicitAppInsightsAzureTableStorageDecorator<TEntity>(tableStorage);
        }
    }
}

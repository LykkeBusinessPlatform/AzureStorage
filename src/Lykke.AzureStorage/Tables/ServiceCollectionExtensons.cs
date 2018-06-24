using System;

using AzureStorage;
using AzureStorage.Tables;

using Common.Log;

using Lykke.SettingsReader;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Tables
{
    [Obsolete("Register repository in the container, instead of INoSQLTableStorage")]
    public static class ServiceCollectionExtensons
    {
        public static IServiceCollection AddTableStorage<TTableEntity>(
            this IServiceCollection services,
            IReloadingManager<string> connectionStringManager,
            string tableName,
            ILog log = null,
            TimeSpan? maxExecutionTimeout = null)

            where TTableEntity : class, ITableEntity, new()
        {
            services.AddSingleton<INoSQLTableStorage<TTableEntity>>(serviceProvider => 
                AzureTableStorage<TTableEntity>.Create(
                    connectionStringManager, 
                    tableName, 
                    log ?? serviceProvider.GetService<ILog>(), 
                    maxExecutionTimeout
                )
            );

            return services;
        }
    }
}

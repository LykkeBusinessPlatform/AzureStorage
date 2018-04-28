using AzureStorage.Tables.Redis;
using Common.Log;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.WindowsAzure.Storage.Table;
using StackExchange.Redis;

namespace AzureStorage.Tables.Decorators
{
    /// <summary>
    /// Cached in Redis <see cref="INoSQLTableStorage{T}"/> decorator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class RedisCachedAzureTableStorageDecorator<T> : CachedAzureTableStorageDecorator<T>
        where T : class, ITableEntity, new()
    {
        public RedisCachedAzureTableStorageDecorator(
            INoSQLTableStorage<T> primaryStorage,
            IDistributedCache redisCache,
            IDatabase redisDatabase,
            IServer redisServer,
            AzureRedisSettings settings,
            string tableKeysOffset,
            ILog log)
        : base(
            primaryStorage,
            new RetryOnFailureAzureTableStorageDecorator<T>(
                new NoSqlTableInRedis<T>(
                    redisCache,
                    redisDatabase,
                    redisServer,
                    new NoSqlTableInRedisSettings
                    {
                        AbsoluteExpirationRelativeToNow = settings?.AbsoluteExpirationRelativeToNow,
                        SlidingExpiration = settings?.SlidingExpiration,
                        TableName = tableKeysOffset
                    },
                    log)),
            log)
        {
        }
    }
}

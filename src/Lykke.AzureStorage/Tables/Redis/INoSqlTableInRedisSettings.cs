using System;

namespace AzureStorage.Tables.Redis
{
    public interface INoSqlTableInRedisSettings
    {
        string TableName { get; }
        TimeSpan? AbsoluteExpirationRelativeToNow { get; }
        TimeSpan? SlidingExpiration { get; }
    }
}

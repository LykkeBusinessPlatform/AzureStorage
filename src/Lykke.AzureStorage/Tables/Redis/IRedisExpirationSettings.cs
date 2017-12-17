using System;

namespace AzureStorage.Tables.Redis
{
    public interface IAzureRedisSettings
    {
        bool UseLazyLoadInCache { get; }
        TimeSpan? AbsoluteExpirationRelativeToNow { get; }
        TimeSpan? SlidingExpiration { get; }
    }
}

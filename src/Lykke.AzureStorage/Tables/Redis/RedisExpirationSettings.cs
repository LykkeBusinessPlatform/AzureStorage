using System;

namespace AzureStorage.Tables.Redis
{
    public class AzureRedisSettings : IAzureRedisSettings
    {
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
        public bool UseLazyLoadInCache { get; set; }
    }
}

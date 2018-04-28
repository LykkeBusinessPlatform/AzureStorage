using System;

namespace AzureStorage.Tables.Redis
{
    public class NoSqlTableInRedisSettings
    {
        public string TableName { get; set; }
        public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }
        public TimeSpan? SlidingExpiration { get; set; }
    }
}

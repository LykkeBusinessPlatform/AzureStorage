using System;
using AzureStorage.Tables.Templates.Index;

namespace Lykke.AzureStorage.Tables
{
    public static class AzureIndexExtensions
    {
        internal static Tuple<string, string> ToTuple(this IAzureIndex src)
        {
            return Tuple.Create(src.PrimaryPartitionKey, src.PrimaryRowKey);
        }
    }
}

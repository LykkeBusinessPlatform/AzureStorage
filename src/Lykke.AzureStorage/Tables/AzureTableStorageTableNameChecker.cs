using System;
using System.Text.RegularExpressions;

namespace AzureStorage.Tables
{
    // This dedicated class is made because static member (TableNameRegex) in the 
    // generic class AzureTableStorage<T> will have led to the creation of the multiple 
    // instances of the Regex - one for each closed generic of AzureTableStorage<T>
    internal static class AzureTableStorageTableNameChecker
    {
        private static Regex TableNameRegex { get; }

        static AzureTableStorageTableNameChecker()
        {
            // Table names may contain only alphanumeric characters.
            // Table names cannot begin with a numeric character.
            // Table names are case-insensitive.
            // Table names must be from 3 to 63 characters long.
            TableNameRegex = new Regex("^[A-Za-z][A-Za-z0-9]{2,62}$", RegexOptions.Compiled);
        }

        public static void ThrowIfInvalid(string tableName)
        {
            if (!TableNameRegex.IsMatch(tableName))
            {
                throw new InvalidOperationException($"Table name {tableName} doesn't satisfy Azure Table name constraints - https://docs.microsoft.com/en-us/rest/api/storageservices/Understanding-the-Table-Service-Data-Model");
            }
        }
    }
}

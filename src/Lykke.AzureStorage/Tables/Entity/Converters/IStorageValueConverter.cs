namespace Lykke.AzureStorage.Tables.Entity.Converters
{
    internal interface IStorageValueConverter
    {
        object ConvertFromStorage(object value);
        object ConvertFromEntity(object value);
    }
}

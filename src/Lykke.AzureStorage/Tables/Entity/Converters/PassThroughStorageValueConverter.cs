namespace Lykke.AzureStorage.Tables.Entity.Converters
{
    internal sealed class PassThroughStorageValueConverter : IStorageValueConverter
    {
        public object ConvertFromStorage(object value) => value;
        public object ConvertFromEntity(object value) => value;
    }
}

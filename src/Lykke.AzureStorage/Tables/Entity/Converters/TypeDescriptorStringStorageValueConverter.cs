using System;
using System.ComponentModel;

namespace Lykke.AzureStorage.Tables.Entity.Converters
{
    internal sealed class TypeDescriptorStringStorageValueConverter : IStorageValueConverter
    {
        private readonly TypeConverter _converter;

        public TypeDescriptorStringStorageValueConverter(TypeConverter converter)
        {
            _converter = converter;
        }
        
        public object ConvertFromStorage(object value)
        {
            if (value is string s)
            {
                return _converter.ConvertFromInvariantString(s);
            }

            throw new ArgumentException("Value should be a string", nameof(value));
        }

        public object ConvertFromEntity(object value)
        {
            return _converter.ConvertToInvariantString(value);
        }
    }
}

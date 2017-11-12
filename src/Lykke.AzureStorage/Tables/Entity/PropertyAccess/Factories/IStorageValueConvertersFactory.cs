using System.Reflection;
using Lykke.AzureStorage.Tables.Entity.Converters;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal interface IStorageValueConvertersFactory
    {
        IStorageValueConverter Create(PropertyInfo property);
    }
}

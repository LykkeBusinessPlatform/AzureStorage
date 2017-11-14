using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation
{
    internal interface IEntityPropertyAccessorsFactory
    {
        EntityPropertyAccessor Create(PropertyInfo property);
    }
}

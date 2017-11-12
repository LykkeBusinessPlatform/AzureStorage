using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal interface IEntityPropertyAccessorsFactory
    {
        EntityPropertyAccessor Create(PropertyInfo property);
    }
}

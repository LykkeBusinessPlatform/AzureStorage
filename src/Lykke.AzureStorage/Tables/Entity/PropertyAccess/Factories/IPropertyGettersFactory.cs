using System;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal interface IPropertyGettersFactory
    {
        Func<AzureTableEntity, object> Create(PropertyInfo property);
    }
}

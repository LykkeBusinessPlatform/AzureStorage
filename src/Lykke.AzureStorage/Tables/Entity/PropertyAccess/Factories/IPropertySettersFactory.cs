using System;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal interface IPropertySettersFactory
    {
        Action<AzureTableEntity, object> Create(PropertyInfo property);
    }
}

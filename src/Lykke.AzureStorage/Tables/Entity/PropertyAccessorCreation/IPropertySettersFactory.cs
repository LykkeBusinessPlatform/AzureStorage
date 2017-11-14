using System;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation
{
    internal interface IPropertySettersFactory
    {
        Action<object, object> Create(PropertyInfo property);
    }
}

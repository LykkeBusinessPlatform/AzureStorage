using System;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation
{
    internal interface IPropertyGettersFactory
    {
        Func<object, object> Create(PropertyInfo property);
    }
}

using System;
using System.Reflection;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories
{
    internal interface IPropertyGettersFactory
    {
        Func<object, object> Create(PropertyInfo property);
    }
}

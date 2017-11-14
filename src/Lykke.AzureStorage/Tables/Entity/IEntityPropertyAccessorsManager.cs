using System;
using System.Collections.Generic;

namespace Lykke.AzureStorage.Tables.Entity
{
    internal interface IEntityPropertyAccessorsManager
    {
        IEnumerable<EntityPropertyAccessor> GetPropertyAccessors(Type type);
    }
}
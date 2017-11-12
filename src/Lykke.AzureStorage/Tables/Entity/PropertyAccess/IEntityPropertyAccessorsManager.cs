using System;
using System.Collections.Generic;

namespace Lykke.AzureStorage.Tables.Entity.PropertyAccess
{
    /// <summary>
    /// Implementation should be thread safe
    /// </summary>
    internal interface IEntityPropertyAccessorsManager
    {
        IEnumerable<EntityPropertyAccessor> GetPropertyAccessors(Type type);
    }
}

using System;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging
{
    /// <summary>
    /// Implementation should be thread safe
    /// </summary>
    internal interface IEntityValueTypeMergingStrategiesManager
    {
        IValueTypeMergingStrategy GetStrategy(Type type);
    }
}

using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Factories
{
    internal interface IValueTypeMergingStrategiesFactory
    {
        IValueTypeMergingStrategy Create(ValueTypeMergingStrategy strategyType);
    }
}

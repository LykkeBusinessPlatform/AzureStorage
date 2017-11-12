using System;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Factories
{
    internal class ValueTypeMergingStrategiesFactory : IValueTypeMergingStrategiesFactory
    {
        public IValueTypeMergingStrategy Create(ValueTypeMergingStrategy strategyType)
        {
            switch (strategyType)
            {
                case ValueTypeMergingStrategy.Forbid:
                    return new ForbidValueTypeMergingStrategy();

                case ValueTypeMergingStrategy.UpdateAlways:
                    return new UpdateAlwaysValueTypeMergingStrategy();

                case ValueTypeMergingStrategy.UpdateIfDirty:
                    return new UpdateIfDirtyValueTypeMergingStrategy();

                default:
                    throw new ArgumentOutOfRangeException(nameof(strategyType), strategyType, null);
            }
        }
    }
}

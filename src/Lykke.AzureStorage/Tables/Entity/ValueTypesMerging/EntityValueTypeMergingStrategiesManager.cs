using System;
using System.Collections.Concurrent;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Factories;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;

namespace Lykke.AzureStorage.Tables.Entity.ValueTypesMerging
{
    internal class EntityValueTypeMergingStrategiesManager : IEntityValueTypeMergingStrategiesManager
    {
        public static IEntityValueTypeMergingStrategiesManager Instance { get; private set; }

        private readonly IEntityMetamodel _metamodel;
        private readonly IValueTypeMergingStrategiesFactory _strategiesesFactory;
        private readonly ConcurrentDictionary<Type, ValueTypeMergingStrategy> _strategieTypes;

        static EntityValueTypeMergingStrategiesManager()
        {
            Instance = new EntityValueTypeMergingStrategiesManager(
                EntityMetamodel.Instance,
                new ValueTypeMergingStrategiesFactory());
        }

        public static void Configure(IEntityMetamodel metamodel, IValueTypeMergingStrategiesFactory strategiesFactory)
        {
            Instance = new EntityValueTypeMergingStrategiesManager(metamodel, strategiesFactory);   
        }

        public EntityValueTypeMergingStrategiesManager(IEntityMetamodel metamodel, IValueTypeMergingStrategiesFactory strategiesesFactory)
        {
            _metamodel = metamodel;
            _strategiesesFactory = strategiesesFactory;

            _strategieTypes = new ConcurrentDictionary<Type, ValueTypeMergingStrategy>();
        }

        public IValueTypeMergingStrategy GetStrategy(Type type)
        {
            var strategyType = _strategieTypes.GetOrAdd(type, t =>
            {
                if (!typeof(AzureTableEntity).IsAssignableFrom(type))
                {
                    throw new InvalidOperationException($"Type {type} should be descendant of the {nameof(AzureTableEntity)}");
                }

                var metamodelStrategy = _metamodel.TryGetValueTypeMergingStrategy(type);

                return metamodelStrategy == ValueTypeMergingStrategy.None
                    ? ValueTypeMergingStrategy.Forbid
                    : metamodelStrategy;
            });

            return _strategiesesFactory.Create(strategyType);
        }
    }
}

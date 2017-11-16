using System;
using System.Collections.Generic;
using System.Reflection;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Factories;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.ValueTypesMerging
{
    [TestClass]
    public class EntityValueTypeMergingStrategiesManagerTests
    {
        #region Mocks

        private class MetamodelMock : IEntityMetamodel
        {
            private readonly Dictionary<Type, int> _getStrategyCalledTimes;

            public MetamodelMock()
            {
                _getStrategyCalledTimes = new Dictionary<Type, int>();
            }

            public int GetCreatedTimes(Type type)
            {
                _getStrategyCalledTimes.TryGetValue(type, out var times);

                return times;
            }

            public IStorageValueSerializer TryGetSerializer(PropertyInfo property)
            {
                throw new InvalidOperationException();
            }

            public ValueTypeMergingStrategy TryGetValueTypeMergingStrategy(Type type)
            {
                _getStrategyCalledTimes.TryGetValue(type, out var times);
                _getStrategyCalledTimes[type] = ++times;

                if (type == TestEntityWithValueTypeMergingStrategyType)
                {
                    return ValueTypeMergingStrategy.UpdateIfDirty;
                }
                if (type == TestEntityWithoutValueTypeMergingStrategyType)
                {
                    return ValueTypeMergingStrategy.None;
                }

                throw new InvalidOperationException("Unknown entity type");
            }
        }

        private class TestEntityWithValueTypeMergingStrategy : AzureTableEntity
        {
        }

        private class TestEntityWithoutValueTypeMergingStrategy : AzureTableEntity
        {
        }

        #endregion

        private MetamodelMock _metamodel;
        private static readonly Type TestEntityWithValueTypeMergingStrategyType = typeof(TestEntityWithValueTypeMergingStrategy);
        private static readonly Type TestEntityWithoutValueTypeMergingStrategyType = typeof(TestEntityWithoutValueTypeMergingStrategy);

        [TestInitialize]
        public void InitializeTest()
        {
            _metamodel = new MetamodelMock();
        }

        [TestMethod]
        public void Test_that_strategy_types_are_cached()
        {
            // Arrange
            var strategiesFactory = new ValueTypeMergingStrategiesFactory();
            var manager = new EntityValueTypeMergingStrategiesManager(_metamodel, strategiesFactory);

            // Act
            manager.GetStrategy(TestEntityWithValueTypeMergingStrategyType);
            manager.GetStrategy(TestEntityWithValueTypeMergingStrategyType);

            // Assert
            Assert.AreEqual(1, _metamodel.GetCreatedTimes(TestEntityWithValueTypeMergingStrategyType));
        }

        [TestMethod]
        public void Test_that_strategy_instances_are_always_different()
        {
            // Arrange
            var strategiesFactory = new ValueTypeMergingStrategiesFactory();
            var manager = new EntityValueTypeMergingStrategiesManager(_metamodel, strategiesFactory);

            // Act
            var strategy1 = manager.GetStrategy(TestEntityWithValueTypeMergingStrategyType);
            var strategy2 = manager.GetStrategy(TestEntityWithValueTypeMergingStrategyType);

            // Assert
            Assert.IsNotNull(strategy1);
            Assert.AreNotSame(strategy1, strategy2);
            Assert.AreEqual(strategy1.GetType(), strategy2.GetType());
        }

        [TestMethod]
        public void Test_that_default_strategy_is_forbid_value_type_merging()
        {
            // Arrange
            var strategiesFactory = new ValueTypeMergingStrategiesFactory();
            var manager = new EntityValueTypeMergingStrategiesManager(_metamodel, strategiesFactory);

            // Act
            var strategy = manager.GetStrategy(TestEntityWithoutValueTypeMergingStrategyType);

            // Assert
            Assert.IsNotNull(strategy);
            Assert.IsInstanceOfType(strategy, typeof(ForbidValueTypeMergingStrategy));
        }

        [TestMethod]
        public void Test_that_specified_in_the_metamodel_strategy_is_used_if_any()
        {
            // Arrange
            var strategiesFactory = new ValueTypeMergingStrategiesFactory();
            var manager = new EntityValueTypeMergingStrategiesManager(_metamodel, strategiesFactory);

            // Act
            var strategy = manager.GetStrategy(TestEntityWithValueTypeMergingStrategyType);

            // Assert
            Assert.IsNotNull(strategy);
            Assert.IsInstanceOfType(strategy, typeof(UpdateIfDirtyValueTypeMergingStrategy));
        }
    }
}

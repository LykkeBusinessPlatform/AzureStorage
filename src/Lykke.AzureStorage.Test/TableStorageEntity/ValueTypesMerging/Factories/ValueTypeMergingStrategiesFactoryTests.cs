using System;
using System.Linq;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.ValueTypesMerging.Factories
{
    [TestClass]
    public class ValueTypeMergingStrategiesFactoryTests
    {
        [TestMethod]
        public void Test_that_all_strategies_can_be_created()
        {
            // Arrange
            var allStrategyTypes = Enum
                .GetValues(typeof(ValueTypeMergingStrategy))
                .Cast<ValueTypeMergingStrategy>()
                .Where(v => v != ValueTypeMergingStrategy.None);
            var factory = new ValueTypeMergingStrategiesFactory();

            // Act
            var allStrategies = allStrategyTypes
                .Select(strategyType => factory.Create(strategyType))
                .ToArray();

            // Assert
            foreach (var strategy in allStrategies)
            {
                Assert.IsNotNull(strategy);
            }
        }

        [TestMethod]
        public void Test_that_invalid_code_will_throw()
        {
            // Arrange
            var factory = new ValueTypeMergingStrategiesFactory();

            // Act/Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => factory.Create((ValueTypeMergingStrategy) int.MaxValue));
        }
    }
}

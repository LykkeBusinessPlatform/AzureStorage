using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.ValueTypesMerging.Strategies
{
    [TestClass]
    public class ForbidValueTypeMergingStrategyTests
    {
        private class TestEntity : AzureTableEntity
        {
        }

        private ForbidValueTypeMergingStrategy _strategy;
        private TestEntity _entity;

        [TestInitialize]
        public void InitializeTest()
        {
            _strategy = new ForbidValueTypeMergingStrategy();
            _entity = new TestEntity();
        }

        [TestMethod]
        public void Test_that_value_type_merging_is_forbidden()
        {
            // Arrange
            var entityPropertyAccessor = new EntityPropertyAccessor(
                propertyName: "ValueTypeProperty",
                isValueType: true,
                getter: e => 123,
                setter: (e, v) => { },
                converter: new PassThroughStorageValueConverter());

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
                _strategy.GetEntityProperty(_entity, entityPropertyAccessor, isMergingOperation: true));
        }

        [TestMethod]
        public void Test_that_reference_type_merging_is_granted()
        {
            // Arrange
            var entityPropertyAccessor = new EntityPropertyAccessor(
                propertyName: "ReferenceTypeProperty",
                isValueType: false,
                getter: e => "123",
                setter: (e, v) => { },
                converter: new PassThroughStorageValueConverter());
            var expectedEntityProperty = entityPropertyAccessor.GetProperty(_entity);

            // Act
            var entityProperty = _strategy.GetEntityProperty(_entity, entityPropertyAccessor, isMergingOperation: true);

            // Assert
            Assert.AreEqual(expectedEntityProperty, entityProperty);
        }

        [TestMethod]
        public void Test_that_value_type_not_merging_operations_is_granted()
        {
            // Arrange
            var entityPropertyAccessor = new EntityPropertyAccessor(
                propertyName: "ValueTypeProperty",
                isValueType: true,
                getter: e => 123,
                setter: (e, v) => { },
                converter: new PassThroughStorageValueConverter());
            var expectedEntityProperty = entityPropertyAccessor.GetProperty(_entity);

            // Act
            var entityProperty = _strategy.GetEntityProperty(_entity, entityPropertyAccessor, isMergingOperation: false);

            // Assert
            Assert.AreEqual(expectedEntityProperty, entityProperty);
        }
    }
}

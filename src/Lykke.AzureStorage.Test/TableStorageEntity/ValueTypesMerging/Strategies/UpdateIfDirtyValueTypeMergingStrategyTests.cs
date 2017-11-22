using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.ValueTypesMerging.Strategies
{
    [TestClass]
    public class UpdateIfDirtyValueTypeMergingStrategyTests
    {
        private class TestEntity : AzureTableEntity
        {
        }

        private UpdateIfDirtyValueTypeMergingStrategy _strategy;
        private TestEntity _entity;

        [TestInitialize]
        public void InitializeTest()
        {
            _strategy = new UpdateIfDirtyValueTypeMergingStrategy();
            _entity = new TestEntity();
        }

        [TestMethod]
        public void Test_that_not_dirty_value_type_property_merging_is_returns_null_entity_property()
        {
            // Arrange
            var entityPropertyAccessor = new EntityPropertyAccessor(
                propertyName: "ValueTypeProperty",
                isValueType: true,
                getter: e => 123,
                setter: (e, v) => { },
                converter: new PassThroughStorageValueConverter());
            var originalEntityProperty = entityPropertyAccessor.GetProperty(_entity);

            // Act
            var entityProperty = _strategy.GetEntityProperty(_entity, entityPropertyAccessor, isMergingOperation: true);

            // Assert
            Assert.IsNotNull(entityProperty);
            Assert.IsNull(entityProperty.PropertyAsObject);
            Assert.IsNotNull(originalEntityProperty);
        }

        [TestMethod]
        public void Test_that_dirty_value_type_property_merging_is_passes_through()
        {
            // Arrange
            var entityPropertyAccessor = new EntityPropertyAccessor(
                propertyName: "ValueTypeProperty",
                isValueType: true,
                getter: e => 123,
                setter: (e, v) => { },
                converter: new PassThroughStorageValueConverter());
            var expectedEntityProperty = entityPropertyAccessor.GetProperty(_entity);

            _strategy.MarkValueTypePropertyAsDirty(entityPropertyAccessor.PropertyName);

            // Act
            var entityProperty = _strategy.GetEntityProperty(_entity, entityPropertyAccessor, isMergingOperation: true);

            // Assert
            Assert.AreEqual(expectedEntityProperty, entityProperty);
        }

        [TestMethod]
        public void Test_that_reference_type_merging_is_passes_entity_property_through()
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
        public void Test_that_value_type_not_merging_operations_is_passes_entity_property_through()
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

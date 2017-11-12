using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Factories;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging.Strategies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Test.TableStorageEntity
{
    [TestClass]
    public class AzureTableEntityTests
    {
        #region Mocks

        private class ValueTypeMergingStrategiesFactoryMock : IValueTypeMergingStrategiesFactory
        {
            private readonly Func<IValueTypeMergingStrategy> _createStrategyIsCalled;
            private readonly Action<string> _markValueTypePropertyAsDirtyIsCalled;
            private readonly Action _notifyEntityWasReadIsCalled;
            private readonly Action _notifyEntityWasWrittenIsCalled;
            private readonly Func<AzureTableEntity, EntityPropertyAccessor, bool, EntityProperty> _getEntityPropertyIsCalled;

            public ValueTypeMergingStrategiesFactoryMock(
                Func<IValueTypeMergingStrategy> createStrategyIsCalled = null,
                Action<string> markValueTypePropertyAsDirtyIsCalled = null,
                Action notifyEntityWasReadIsCalled = null,
                Action notifyEntityWasWrittenIsCalled = null,
                Func<AzureTableEntity, EntityPropertyAccessor, bool, EntityProperty> getEntityPropertyIsCalled = null)
            {
                _createStrategyIsCalled = createStrategyIsCalled;
                _markValueTypePropertyAsDirtyIsCalled = markValueTypePropertyAsDirtyIsCalled;
                _notifyEntityWasReadIsCalled = notifyEntityWasReadIsCalled;
                _notifyEntityWasWrittenIsCalled = notifyEntityWasWrittenIsCalled;
                _getEntityPropertyIsCalled = getEntityPropertyIsCalled;
            }

            public IValueTypeMergingStrategy Create(ValueTypeMergingStrategy strategyType)
            {
                return _createStrategyIsCalled?.Invoke() ??
                       new ValueTypeMergingStrategyMock(
                           _markValueTypePropertyAsDirtyIsCalled,
                           _notifyEntityWasReadIsCalled,
                           _notifyEntityWasWrittenIsCalled,
                           _getEntityPropertyIsCalled);
            }
        }

        private class ValueTypeMergingStrategyMock : IValueTypeMergingStrategy
        {
            private readonly Action<string> _markValueTypePropertyAsDirtyIsCalled;
            private readonly Action _notifyEntityWasReadIsCalled;
            private readonly Action _notifyEntityWasWrittenIsCalled;
            private readonly Func<AzureTableEntity, EntityPropertyAccessor, bool, EntityProperty> _getEntityPropertyIsCalled;

            public ValueTypeMergingStrategyMock(
                Action<string> markValueTypePropertyAsDirtyIsCalled, 
                Action notifyEntityWasReadIsCalled,
                Action notifyEntityWasWrittenIsCalled, 
                Func<AzureTableEntity, EntityPropertyAccessor, bool, EntityProperty> getEntityPropertyIsCalled)
            {
                _markValueTypePropertyAsDirtyIsCalled = markValueTypePropertyAsDirtyIsCalled;
                _notifyEntityWasReadIsCalled = notifyEntityWasReadIsCalled;
                _notifyEntityWasWrittenIsCalled = notifyEntityWasWrittenIsCalled;
                _getEntityPropertyIsCalled = getEntityPropertyIsCalled;
            }

            public void MarkValueTypePropertyAsDirty(string propertyName)
            {
                _markValueTypePropertyAsDirtyIsCalled?.Invoke(propertyName);
            }

            public void NotifyEntityWasRead()
            {
                _notifyEntityWasReadIsCalled?.Invoke();
            }

            public void NotifyEntityWasWritten()
            {
                _notifyEntityWasWrittenIsCalled?.Invoke();
            }

            public EntityProperty GetEntityProperty(
                AzureTableEntity entity, 
                EntityPropertyAccessor propertyAccessor,
                bool isMergingOperation)
            {
                return _getEntityPropertyIsCalled?.Invoke(entity, propertyAccessor, isMergingOperation);
            }
        }

        private class TestEntity : AzureTableEntity
        {
            public string StringProperty { get; set; }
            public int MissedInStorageProperty { get; set; }
            public string OneMoreProperty { get; set; }

            public decimal ValueTypeProperty
            {
                get => _valueTypeProperty;
                set
                {
                    _valueTypeProperty = value;
                    MarkValueTypePropertyAsDirty(nameof(ValueTypeProperty));
                }
            }

            private decimal _valueTypeProperty;

        }

        [UsedImplicitly]
        private class ComplexType
        {
        }
        
        private class TestEntityWithUnknownTypeProperties : AzureTableEntity
        {
            public string StringProperty { get; set; }
            public ComplexType InvalidTypeProperty { get; set; }
        }

        #endregion


        #region Creating

        [TestMethod]
        public void Test_that_new_value_type_merging_strategy_is_created_for_every_entity_instance()
        {
            // Arrange
            var strategiesCreatedCount = 0;

            EntityValueTypeMergingStrategiesManager.Configure(
                EntityMetamodelImpl.Empty,
                new ValueTypeMergingStrategiesFactoryMock(
                    createStrategyIsCalled: () =>
                    {
                        strategiesCreatedCount++;

                        return null;
                    }));

            // Act
            // ReSharper disable UnusedVariable
            var entity1 = new TestEntity();
            var entity2 = new TestEntity();
            // ReSharper restore UnusedVariable

            // Assert
            Assert.AreEqual(2, strategiesCreatedCount);
        }

        #endregion

        
        #region Reading

        [TestMethod]
        public void Test_that_not_existing_in_entity_properties_are_ignored()
        {
            // Arrange
            var entity = new TestEntity();
            ITableEntity itableEntity = entity;
            var storageProperties = new Dictionary<string, EntityProperty>
            {
                {"MissedProperty", EntityProperty.GeneratePropertyForString("some value")}
            };

            // Act
            itableEntity.ReadEntity(storageProperties, new OperationContext());

            // Assert
            // Nothing to assert, it should just not throws
        }

        [TestMethod]
        public void Test_that_properties_are_read()
        {
            // Arrange
            var entity = new TestEntity();
            ITableEntity itableEntity = entity;
            var storageValue = "some value";
            var storageProperties = new Dictionary<string, EntityProperty>
            {
                {nameof(TestEntity.StringProperty), EntityProperty.GeneratePropertyForString(storageValue)}
            };

            // Act
            itableEntity.ReadEntity(storageProperties, new OperationContext());

            // Assert
            Assert.AreEqual(storageValue, entity.StringProperty);
        }

        [TestMethod]
        public void Test_that_property_type_mismatch_throws()
        {
            // Arrange
            var entity = new TestEntity();
            ITableEntity itableEntity = entity;
            var storageValue = 123;
            var storageProperties = new Dictionary<string, EntityProperty>
            {
                {nameof(TestEntity.StringProperty), EntityProperty.GeneratePropertyForInt(storageValue)}
            };

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() => itableEntity.ReadEntity(storageProperties, new OperationContext()));
        }

        [TestMethod]
        public void Test_that_value_type_merging_strategy_is_notified_about_entity_reading()
        {
            // Arrange
            var wasNotified = false;

            EntityValueTypeMergingStrategiesManager.Configure(
                EntityMetamodelImpl.Empty,
                new ValueTypeMergingStrategiesFactoryMock(
                    notifyEntityWasReadIsCalled: () => { wasNotified = true; }));

            var entity = new TestEntity();
            ITableEntity itableEntity = entity;

            // Act
            itableEntity.ReadEntity(new Dictionary<string, EntityProperty>(), new OperationContext());

            // Assert
            Assert.IsTrue(wasNotified);
        }

        #endregion


        #region Writing

        [TestMethod]
        public void Test_that_properties_are_writed()
        {
            // Arrange
            var entity = new TestEntity
            {
                StringProperty = "some value"
            };
            ITableEntity itableEntity = entity;

            // Act
            var storageProperties = itableEntity.WriteEntity(new OperationContext());

            // Assert
            Assert.AreEqual(3, storageProperties.Count);
            Assert.IsTrue(storageProperties.ContainsKey(nameof(TestEntity.StringProperty)));
            Assert.IsTrue(storageProperties.ContainsKey(nameof(TestEntity.MissedInStorageProperty)));
            Assert.IsTrue(storageProperties.ContainsKey(nameof(TestEntity.OneMoreProperty)));
            Assert.AreEqual(entity.StringProperty, storageProperties[nameof(TestEntity.StringProperty)].PropertyAsObject);
            Assert.AreEqual(default(int), storageProperties[nameof(TestEntity.MissedInStorageProperty)].PropertyAsObject);
            Assert.IsNull(storageProperties[nameof(TestEntity.OneMoreProperty)].PropertyAsObject);
        }

        [TestMethod]
        public void Test_that_entity_with_invalid_type_properties_are_throws()
        {
            // Arrange
            var entity = new TestEntityWithUnknownTypeProperties
            {
                StringProperty = "some value"
            };
            ITableEntity itableEntity = entity;

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() => itableEntity.WriteEntity(new OperationContext()));
        }

        [TestMethod]
        public void Test_that_merging_operation_is_detected_and_marker_header_is_removed()
        {
            // Arrange
            var itWasMergingOperation = false;

            EntityValueTypeMergingStrategiesManager.Configure(
                EntityMetamodelImpl.Empty,
                new ValueTypeMergingStrategiesFactoryMock(
                    getEntityPropertyIsCalled: (e, a, isMergingOperation) =>
                    {
                        itWasMergingOperation = isMergingOperation;

                        return null;
                    }));

            var entity = new TestEntity();
            ITableEntity itableEntity = entity;

            var operationContext = new OperationContext
            {
                UserHeaders = new Dictionary<string, string>
                {
                    {AzureTableEntity.MergingOperationContextHeader, string.Empty}
                }
            };

            // Act
            itableEntity.WriteEntity(operationContext);

            // Assert
            Assert.IsTrue(itWasMergingOperation);
            Assert.IsFalse(operationContext.UserHeaders.ContainsKey(AzureTableEntity.MergingOperationContextHeader));
        }

        [TestMethod]
        public void Test_that_value_type_merging_strategy_is_notified_about_entity_writing()
        {
            // Arrange
            var wasNotified = false;

            EntityValueTypeMergingStrategiesManager.Configure(
                EntityMetamodelImpl.Empty,
                new ValueTypeMergingStrategiesFactoryMock(
                    notifyEntityWasWrittenIsCalled: () => { wasNotified = true; }));

            var entity = new TestEntity();
            ITableEntity itableEntity = entity;

            // Act
            itableEntity.WriteEntity(new OperationContext());
            
            // Assert
            Assert.IsTrue(wasNotified);
        }

        #endregion


        #region Value type properties merging

        [TestMethod]
        public void Test_that_value_type_merging_strategy_is_notified_about_property_changing()
        {
            // Arrange
            var wasNotified = false;

            EntityValueTypeMergingStrategiesManager.Configure(
                EntityMetamodelImpl.Empty,
                new ValueTypeMergingStrategiesFactoryMock(
                    markValueTypePropertyAsDirtyIsCalled: p => { wasNotified = true; }));

            // ReSharper disable once UseObjectOrCollectionInitializer
            var entity = new TestEntity();

            // Act
            entity.ValueTypeProperty = 0m;

            // Assert
            Assert.IsTrue(wasNotified);
        }

        #endregion
    }
}

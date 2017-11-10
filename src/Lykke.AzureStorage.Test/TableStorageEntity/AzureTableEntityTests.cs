using System;
using System.Collections.Generic;
using Lykke.AzureStorage.Tables;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Test.TableStorageEntity
{
    [TestClass]
    public class AzureTableEntityTests
    {
        private class TestEntity : AzureTableEntity
        {
            public string StringProperty { get; set; }
            public int MissedInStorageProperty { get; set; }
            public string OneMoreProperty { get; set; }
        }

        private class ComplexType
        {
        }
        
        private class TestEntityWithUnknownTypeProperties : AzureTableEntity
        {
            public string StringProperty { get; set; }
            public ComplexType InvalidTypeProperty { get; set; }
        }

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

        #endregion
    }
}

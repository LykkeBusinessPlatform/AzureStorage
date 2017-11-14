using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Test.TableStorageEntity
{
    [TestClass]
    public class EntityPropertyAccessorTests
    {
        private class TestEntity : AzureTableEntity
        {
            public string Property { get; set; }
        }

        private class StorageValueConverterMock : IStorageValueConverter
        {
            private readonly Func<object, object> _convertFromStorage;
            private readonly Func<object, object> _convertToStorage;

            public StorageValueConverterMock(Func<object, object> convertFromStorage, Func<object, object> convertToStorage)
            {
                _convertFromStorage = convertFromStorage;
                _convertToStorage = convertToStorage;
            }

            public object ConvertFromStorage(object value)
            {
                return _convertFromStorage(value);
            }

            public object ConvertFromEntity(object value)
            {
                return _convertToStorage(value);
            }
        }

        [TestMethod]
        public void Test_that_converter_is_not_used_while_null_value_setting()
        {
            // Arrange
            var isConvertedFromStorageEverCalled = false;
            var converter = new StorageValueConverterMock(
                v =>
                {
                    isConvertedFromStorageEverCalled = true;
                    return v;
                },
                v => v);
            var accessor = new EntityPropertyAccessor(
                nameof(TestEntity.Property),
                e => null,
                (e, v) => ((TestEntity)e).Property = (string)v,
                converter);
            var entity = new TestEntity
            {
                Property = "Not null"
            };

            // Act
            accessor.SetProperty(entity, EntityProperty.GeneratePropertyForString(null));

            // Assert
            Assert.IsNull(entity.Property);
            Assert.IsFalse(isConvertedFromStorageEverCalled);
        }

        [TestMethod]
        public void Test_that_converter_is_used_while_not_null_value_setting()
        {
            // Arrange
            var isConvertedFromStorageEverCalled = false;
            var converter = new StorageValueConverterMock(
                v =>
                {
                    isConvertedFromStorageEverCalled = true;

                    return v;
                },
                v => v);
            var accessor = new EntityPropertyAccessor(
                nameof(TestEntity.Property),
                e => null,
                (e, v) => ((TestEntity)e).Property = (string)v,
                converter);
            var entity = new TestEntity();
            var value = "Some value";

            // Act
            accessor.SetProperty(entity, EntityProperty.GeneratePropertyForString(value));

            // Assert
            Assert.AreEqual(value, entity.Property);
            Assert.IsTrue(isConvertedFromStorageEverCalled);
        }

        [TestMethod]
        public void Test_that_converter_is_not_used_while_null_value_getting()
        {
            // Arrange
            var isConvertedToStorageEverCalled = false;
            var converter = new StorageValueConverterMock(
                v => v,
                v =>
                {
                    isConvertedToStorageEverCalled = true;

                    return v;
                });
            var accessor = new EntityPropertyAccessor(
                nameof(TestEntity.Property),
                e => ((TestEntity)e).Property,
                (e, v) => ((TestEntity)e).Property = (string)v,
                converter);
            var entity = new TestEntity
            {
                Property = null
            };

            // Act
            var entityProperty = accessor.GetProperty(entity);

            // Assert
            Assert.IsNotNull(entityProperty);
            Assert.IsNull(entityProperty.StringValue);
            Assert.IsFalse(isConvertedToStorageEverCalled);
        }

        [TestMethod]
        public void Test_that_converter_is_used_while_not_null_value_getting()
        {
            // Arrange
            var isConvertedToStorageEverCalled = false;
            var converter = new StorageValueConverterMock(
                v => v,
                v =>
                {
                    isConvertedToStorageEverCalled = true;

                    return v;
                });
            var accessor = new EntityPropertyAccessor(
                nameof(TestEntity.Property),
                e => ((TestEntity)e).Property,
                (e, v) => ((TestEntity)e).Property = (string)v,
                converter);
            var entity = new TestEntity
            {
                Property = "Some value"
            };

            // Act
            var entityProperty = accessor.GetProperty(entity);

            // Assert
            Assert.IsNotNull(entityProperty);
            Assert.AreEqual(entity.Property, entityProperty.StringValue);
            Assert.IsTrue(isConvertedToStorageEverCalled);
        }
    }
}

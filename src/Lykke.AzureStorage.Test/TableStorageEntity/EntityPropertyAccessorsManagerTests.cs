using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.AzureStorage.Test.TableStorageEntity
{
    [TestClass]
    public class EntityPropertyAccessorsManagerTests
    {
        private class PropertyAccessorsFactoryMock : IEntityPropertyAccessorsFactory
        {
            private readonly Func<PropertyInfo, EntityPropertyAccessor> _create;

            public PropertyAccessorsFactoryMock(Func<PropertyInfo, EntityPropertyAccessor> create)
            {
                _create = create;
            }

            public EntityPropertyAccessor Create(PropertyInfo property)
            {
                return _create(property);
            }
        }

        private class TestEntity : AzureTableEntity
        {
            public string StringProperty { get; set; }

            public virtual int VirtualProperty { get; set; }

            public int ReadOnlyProperty { get; }

            public decimal WriteOnlyProperty { set => _writeOnlyProperty = value; }

            [IgnoreProperty]
            public DateTimeKind IgnoredProperty { get; set; }

            private string PrivateProperty { get; set; }

            internal string InternalProperty { get; set; }

            private decimal _writeOnlyProperty;
        }

        private class DescendantTestEntity : TestEntity
        {
            public int IntProperty { get; set; }
            public override int VirtualProperty { get; set; }
        }

        private static readonly Type TestEntityType = typeof(TestEntity);
        private static readonly Type DescendantTestEntityType = typeof(DescendantTestEntity);

        [TestMethod]
        public void Test_that_property_accessors_is_cached()
        {
            // Arrange
            var createCalledTimes = new Dictionary<PropertyInfo, int>();
            var propertyAccessorsFactory = new PropertyAccessorsFactoryMock(p =>
            {
                createCalledTimes.TryGetValue(p, out var times);
                createCalledTimes[p] = ++times;

                return null;
            });
            var manager = new EntityPropertyAccessorsManager(propertyAccessorsFactory);

            // Act
            manager.GetPropertyAccessors(TestEntityType);
            manager.GetPropertyAccessors(TestEntityType);

            // Assert
            Assert.IsTrue(createCalledTimes.ContainsKey(TestEntityType.GetProperty(nameof(TestEntity.StringProperty))));
            Assert.AreEqual(1, createCalledTimes[TestEntityType.GetProperty(nameof(TestEntity.StringProperty))]);
        }

        [TestMethod]
        public void Test_that_not_applicable_properties_are_skipped()
        {
            // Arrange
            var propertyAccessorsFactory = new PropertyAccessorsFactoryMock(p =>
            {
                return new EntityPropertyAccessor(p.Name, e => null, (e, v) => { }, new PassThroughStorageValueConverter());
            });
            var manager = new EntityPropertyAccessorsManager(propertyAccessorsFactory);

            // Act
            var propertyAccessors = manager.GetPropertyAccessors(TestEntityType).ToArray();

            // Assert
            Assert.AreEqual(2, propertyAccessors.Length);
            Assert.AreEqual(nameof(TestEntity.StringProperty), propertyAccessors[0].PropertyName);
            Assert.AreEqual(nameof(TestEntity.VirtualProperty), propertyAccessors[1].PropertyName);
        }

        [TestMethod]
        public void Test_that_inherited_properties_are_included()
        {
            // Arrange
            var propertyAccessorsFactory = new PropertyAccessorsFactoryMock(p =>
            {
                return new EntityPropertyAccessor(p.Name, e => null, (e, v) => { }, new PassThroughStorageValueConverter());
            });
            var manager = new EntityPropertyAccessorsManager(propertyAccessorsFactory);

            // Act
            var propertyAccessors = manager.GetPropertyAccessors(DescendantTestEntityType).ToArray();

            // Assert
            Assert.AreEqual(3, propertyAccessors.Length);
            Assert.AreEqual(1, propertyAccessors.Count(a => a.PropertyName == nameof(TestEntity.StringProperty)));
            Assert.AreEqual(1, propertyAccessors.Count(a => a.PropertyName == nameof(DescendantTestEntity.IntProperty)));
            Assert.AreEqual(1, propertyAccessors.Count(a => a.PropertyName == nameof(DescendantTestEntity.VirtualProperty)));
        }
    }
}

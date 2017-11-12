using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.PropertyAccess.Factories
{
    [TestClass]
    public class PropertyGettersFactoryTests
    {
        private class TestComplexType
        {
        }

        private class TestEntity
        {
            public int ValueTypeProperty { get; set; }
            public TestComplexType ReferenceTypeProperty { get; set; }
            [UsedImplicitly]
            public string NullProperty { get; set; }
            [UsedImplicitly]
            public virtual string VirtualProperty { get; set; }
        }

        private class DerivedTestEntity : TestEntity
        {
            public int PropertyInDerivedEntity { get; set; }
            public override string VirtualProperty { get; set; }
        }

        private static readonly Type TestEntityType = typeof(TestEntity);
        private static readonly Type DerivedTestEntityType = typeof(DerivedTestEntity);

        [TestMethod]
        public void Test_that_getter_is_created_well_and_works()
        {
            // Arrange
            var factory = new PropertyGettersFactory();
            var entity = new TestEntity
            {
                ValueTypeProperty = 123,
                ReferenceTypeProperty = new TestComplexType(),
                NullProperty = null
            };

            // Act
            var valueTypePropertyGetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.ValueTypeProperty)));
            var referenceTypePropertyGetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.ReferenceTypeProperty)));
            var nullPropertyGetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.NullProperty)));

            var valueTypeValue = valueTypePropertyGetter.Invoke(entity);
            var referenceTypeValue = referenceTypePropertyGetter.Invoke(entity);
            var nullValue = nullPropertyGetter.Invoke(entity);

            // Assert
            Assert.AreEqual(entity.ValueTypeProperty, valueTypeValue);
            Assert.AreEqual(entity.ReferenceTypeProperty, referenceTypeValue);
            Assert.IsNull(nullValue);
        }

        [TestMethod]
        public void Test_that_inherited_properties_can_be_get()
        {
            // Arrange
            var factory = new PropertyGettersFactory();
            var entity = new DerivedTestEntity
            {
                ValueTypeProperty = 123,
                PropertyInDerivedEntity = 555,
                VirtualProperty = "333"
            };

            // Act
            var valueTypePropertyGetter = factory.Create(DerivedTestEntityType.GetProperty(nameof(DerivedTestEntity.ValueTypeProperty)));
            var derivedPropertyGetter = factory.Create(DerivedTestEntityType.GetProperty(nameof(DerivedTestEntity.PropertyInDerivedEntity)));
            var virtualPropertyGetter = factory.Create(DerivedTestEntityType.GetProperty(nameof(DerivedTestEntity.VirtualProperty)));

            var valueTypeValue = valueTypePropertyGetter.Invoke(entity);
            var derivedValue = derivedPropertyGetter.Invoke(entity);
            var virtualValue = virtualPropertyGetter.Invoke(entity);

            // Assert
            Assert.AreEqual(entity.ValueTypeProperty, valueTypeValue);
            Assert.AreEqual(entity.PropertyInDerivedEntity, derivedValue);
            Assert.AreEqual(entity.VirtualProperty, virtualValue);
        }
    }
}

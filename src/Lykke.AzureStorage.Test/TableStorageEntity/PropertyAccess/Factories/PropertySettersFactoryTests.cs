using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.PropertyAccess.Factories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.PropertyAccess.Factories
{
    [TestClass]
    public class PropertySettersFactoryTests
    {
        private class TestComplexType
        {
        }

        private class TestEntity
        {
            [UsedImplicitly]
            public int ValueTypeProperty { get; set; }
            [UsedImplicitly]
            public TestComplexType ReferenceTypeProperty { get; set; }
            public string NullProperty { get; set; }
            [UsedImplicitly]
            public DateTimeKind EnumProperty { get; set; }
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
        public void Test_that_setter_is_created_well_and_works()
        {
            // Arrange
            var factory = new PropertySettersFactory();
            var valueTypeValue = 123;
            var referenceTypeValue = new TestComplexType();
            var entity = new TestEntity
            {
                NullProperty = "not null"
            };

            // Act
            var valueTypePropertySetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.ValueTypeProperty)));
            var referenceTypePropertySetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.ReferenceTypeProperty)));
            var nullPropertySetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.NullProperty)));

            valueTypePropertySetter.Invoke(entity, valueTypeValue);
            referenceTypePropertySetter.Invoke(entity, referenceTypeValue);
            nullPropertySetter.Invoke(entity, null);

            // Assert
            Assert.AreEqual(valueTypeValue, entity.ValueTypeProperty);
            Assert.AreEqual(referenceTypeValue, entity.ReferenceTypeProperty);
            Assert.IsNull(entity.NullProperty);
        }

        [TestMethod]
        public void Test_that_null_value_setting_interpreted_as_null_for_reference_type_property_and_as_default_value_for_value_type_property()
        {
            // Arrange
            var factory = new PropertySettersFactory();
            var entity = new TestEntity
            {
                ReferenceTypeProperty = new TestComplexType()
            };

            // Act
            var valueTypePropertySetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.ValueTypeProperty)));
            var enumPropertySetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.EnumProperty)));
            var referenceTypePropertySetter = factory.Create(TestEntityType.GetProperty(nameof(TestEntity.ReferenceTypeProperty)));

            valueTypePropertySetter.Invoke(entity, null);
            enumPropertySetter.Invoke(entity, null);
            referenceTypePropertySetter.Invoke(entity, null);

            // Assert
            Assert.AreEqual(default(int), entity.ValueTypeProperty);
            Assert.AreEqual(default(DateTimeKind), entity.EnumProperty);
            Assert.AreEqual(null, entity.ReferenceTypeProperty);
        }

        [TestMethod]
        public void Test_that_inherited_properties_can_be_get()
        {
            // Arrange
            var factory = new PropertySettersFactory();
            var valueTypeValue = 123;
            var derivedVaue = 555;
            var virtualValue = "333";
            var entity = new DerivedTestEntity();

            // Act
            var valueTypePropertySetter = factory.Create(DerivedTestEntityType.GetProperty(nameof(DerivedTestEntity.ValueTypeProperty)));
            var derivedPropertySetter = factory.Create(DerivedTestEntityType.GetProperty(nameof(DerivedTestEntity.PropertyInDerivedEntity)));
            var virtualPropertySetter = factory.Create(DerivedTestEntityType.GetProperty(nameof(DerivedTestEntity.VirtualProperty)));

            valueTypePropertySetter.Invoke(entity, valueTypeValue);
            derivedPropertySetter.Invoke(entity, derivedVaue);
            virtualPropertySetter.Invoke(entity, virtualValue);

            // Assert
            Assert.AreEqual(valueTypeValue, entity.ValueTypeProperty);
            Assert.AreEqual(derivedVaue, entity.PropertyInDerivedEntity);
            Assert.AreEqual(virtualValue, entity.VirtualProperty);
        }
    }
}

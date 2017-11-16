using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Serializers;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Metamodel
{
    [TestClass]
    public class EntityMetamodelImplTests
    {
        #region Mocks

        private class ValueSerializerMock : IStorageValueSerializer
        {
            public string Serialize(object value)
            {
                throw new InvalidOperationException();
            }

            public object Deserialize(string serialized)
            {
                throw new InvalidOperationException();
            }
        }

        private class AnotherValueSerializerMock : IStorageValueSerializer
        {
            public string Serialize(object value)
            {
                throw new InvalidOperationException();
            }

            public object Deserialize(string serialized)
            {
                throw new InvalidOperationException();
            }
        }

        private class TestType
        {
        }

        private struct TestStruct
        {
        }

        private class CompositeTestType
        {
            [UsedImplicitly]
            public TestType ComplexProperty { get; set; }

            [UsedImplicitly]
            public TestType[] ComplexPropertyArray { get; set; }

            [UsedImplicitly]
            public IEnumerable<TestType> ComplexPropertyEnumerable{ get; set; }

            [UsedImplicitly]
            public IList<TestType> ComplexPropertyList{ get; set; }

            [UsedImplicitly]
            public TestStruct? NullableStructProperty { get; set; } 
        }

        #endregion

        [TestMethod]
        public void Test_that_no_serializer_is_returned_when_there_are_no_serializers_for_property_and_type()
        {
            // Arrange
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.ComplexProperty));

            var providerMock = new Mock<IMetamodelProvider>();

            providerMock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(p => null);

            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(p => null);

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_property_serializer_overrides_type_serializer()
        {
            // Arrange
            var type = typeof(TestType);
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.ComplexProperty));

            var providerMock = new Mock<IMetamodelProvider>();

            providerMock
                .Setup(x => x.TryGetPropertySerializer(It.Is<PropertyInfo>(p => p == property)))
                .Returns<PropertyInfo>(p => new ValueSerializerMock());

            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.Is<Type>(t => t == type)))
                .Returns<Type>(p => new AnotherValueSerializerMock());

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_type_serializer_is_used_when_there_is_no_property_serializer()
        {
            // Arrange
            var type = typeof(TestType);
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.ComplexProperty));

            var providerMock = new Mock<IMetamodelProvider>();

            providerMock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(p => null);

            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.Is<Type>(t => t == type)))
                .Returns<Type>(p => new AnotherValueSerializerMock());

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(AnotherValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_type_serializer_is_propogated_to_the_arrays()
        {
            // Arrange
            var type = typeof(TestType);
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.ComplexPropertyArray));

            var providerMock = new Mock<IMetamodelProvider>();
            
            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.Is<Type>(t => t == type)))
                .Returns<Type>(p => new ValueSerializerMock());

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_type_serializer_is_propogated_to_the_enumerables()
        {
            // Arrange
            var type = typeof(TestType);
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.ComplexPropertyEnumerable));

            var providerMock = new Mock<IMetamodelProvider>();

            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.Is<Type>(t => t == type)))
                .Returns<Type>(p => new ValueSerializerMock());

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_type_serializer_is_propogated_to_the_lists()
        {
            // Arrange
            var type = typeof(TestType);
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.ComplexPropertyList));

            var providerMock = new Mock<IMetamodelProvider>();

            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.Is<Type>(t => t == type)))
                .Returns<Type>(p => new ValueSerializerMock());

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_structure_serializer_is_propogated_to_the_nullable()
        {
            // Arrange
            var type = typeof(TestStruct);
            var property = typeof(CompositeTestType).GetProperty(nameof(CompositeTestType.NullableStructProperty));

            var providerMock = new Mock<IMetamodelProvider>();

            providerMock
                .Setup(x => x.TryGetTypeSerializer(It.Is<Type>(t => t == type)))
                .Returns<Type>(p => new ValueSerializerMock());

            var metamodelImpl = new EntityMetamodelImpl(providerMock.Object);

            // Act
            var serializer = metamodelImpl.TryGetSerializer(property);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }
    }
}

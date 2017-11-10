using System;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Metamodel.Providers
{
    [TestClass]
    public class AnnotationsBasedMetamodelProviderTests
    {
        #region Mocks

        private class ValueSerializerMock : IStorageValueSerializer
        {
            public string Serialize(object value)
            {
                throw new NotImplementedException();
            }

            public object Deserialize(string serialized)
            {
                throw new NotImplementedException();
            }
        }

        private class OneMoreValueSerializerMock : IStorageValueSerializer
        {
            public string Serialize(object value)
            {
                throw new NotImplementedException();
            }

            public object Deserialize(string serialized)
            {
                throw new NotImplementedException();
            }
        }

        private class AnotherValueSerializerMock : IStorageValueSerializer
        {
            public string Serialize(object value)
            {
                throw new NotImplementedException();
            }

            public object Deserialize(string serialized)
            {
                throw new NotImplementedException();
            }
        }

        [ValueSerializer(typeof(AnnotationsBasedMetamodelProviderTests))]
        private class SerializableTestTypeWithInvalidSerializerType
        {
        }

        [ValueSerializer(typeof(ValueSerializerMock))]
        private class SerializableTestType
        {
        }

        private class DescendantSerializableTestType : SerializableTestType
        {
        }

        [ValueSerializer(typeof(OneMoreValueSerializerMock))]
        private class DescendantSerializableTestTypeWithOverride : SerializableTestType
        {
        }

        private class SecondLevelDescendantSerializableTestType : DescendantSerializableTestTypeWithOverride
        {
        }

        private class NotSerializableTestType
        {
        }

        private class TestTypeWithProperties
        {
            [ValueSerializer(typeof(OneMoreValueSerializerMock))]
            public virtual NotSerializableTestType AnnotatedNotSerializableType { get; set; }

            [ValueSerializer(typeof(OneMoreValueSerializerMock))]
            public virtual SerializableTestType AnnotatedSerializableType { get; set; }

            public virtual NotSerializableTestType NotAnnotatedNotSerializableType { get; set; }

            public virtual SerializableTestType NotAnnotatedSerializableType { get; set; }
        }

        private class DescendantTestTypeWithProperties : TestTypeWithProperties
        {
        }

        private class DescendatnTestTypeWithOverridenProperties : TestTypeWithProperties
        {
            [ValueSerializer(typeof(AnotherValueSerializerMock))]
            public override NotSerializableTestType AnnotatedNotSerializableType { get; set; }

            [ValueSerializer(typeof(AnotherValueSerializerMock))]
            public override SerializableTestType AnnotatedSerializableType { get; set; }
        }

        #endregion


        private IMetamodelProvider _metamodelProvider;
        
        [TestInitialize]
        public void InitializeTest()
        {
            _metamodelProvider = new AnnotationsBasedMetamodelProvider();
        }


        #region Exceptional cases

        [TestMethod]
        public void Test_that_type_specified_in_ValueSerializerAttribute_should_implements_IStorageValueSerializer()
        {
            // Arrange
            var type = typeof(SerializableTestTypeWithInvalidSerializerType);

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() => _metamodelProvider.TryGetTypeSerializer(type));
        }

        [TestMethod]
        public void Test_that_custom_service_provider_should_returns_not_null()
        {
            // Arrange
            var type = typeof(SerializableTestType);
            var property = typeof(TestTypeWithProperties).GetProperty(nameof(TestTypeWithProperties.AnnotatedSerializableType));
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns<Type>(t => null);

            IMetamodelProvider metamodelProvider = new AnnotationsBasedMetamodelProvider(serviceProviderMock.Object);

            // Act
            Assert.ThrowsException<InvalidOperationException>(() => metamodelProvider.TryGetTypeSerializer(type));
            Assert.ThrowsException<InvalidOperationException>(() => metamodelProvider.TryGetPropertySerializer(property));
        }

        [TestMethod]
        public void Test_that_custom_service_provider_should_creates_object_which_implements_IStorageValueSerializer()
        {
            // Arrange
            var type = typeof(SerializableTestType);
            var property = typeof(TestTypeWithProperties).GetProperty(nameof(TestTypeWithProperties.AnnotatedSerializableType));
            var serviceProviderMock = new Mock<IServiceProvider>();

            serviceProviderMock
                .Setup(x => x.GetService(It.IsAny<Type>()))
                .Returns<Type>(t => new AnnotationsBasedMetamodelProviderTests());

            IMetamodelProvider metamodelProvider = new AnnotationsBasedMetamodelProvider(serviceProviderMock.Object);

            // Act
            Assert.ThrowsException<InvalidOperationException>(() => metamodelProvider.TryGetTypeSerializer(type));
            Assert.ThrowsException<InvalidOperationException>(() => metamodelProvider.TryGetPropertySerializer(property));
        }

        #endregion


        #region Type annotation

        [TestMethod]
        public void Test_that_not_annotated_type_is_not_exists_in_metamodel()
        {
            // Arrange
            var type = typeof(NotSerializableTestType);

            // Act
            var serializer = _metamodelProvider.TryGetTypeSerializer(type);

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_simple_type_annotations_works()
        {
            // Arrange
            var type = typeof(SerializableTestType);
            var serializerType = typeof(ValueSerializerMock);
            
            // Act
            var serializer = _metamodelProvider.TryGetTypeSerializer(type);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, serializerType);
        }

        [TestMethod]
        public void Test_that_type_annotation_inherits()
        {
            // Arrange
            var type = typeof(DescendantSerializableTestType);
            var serializerType = typeof(ValueSerializerMock);

            // Act
            var serializer = _metamodelProvider.TryGetTypeSerializer(type);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, serializerType);
        }

        [TestMethod]
        public void Test_that_inherited_type_annotation_can_be_overrided()
        {
            // Arrange
            var type = typeof(DescendantSerializableTestTypeWithOverride);
            var expectedType = typeof(OneMoreValueSerializerMock);

            // Act
            var serializer = _metamodelProvider.TryGetTypeSerializer(type);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, expectedType);
        }

        [TestMethod]
        public void Test_that_overriden_inherited_type_annotation_inherits()
        {
            // Arrange
            var type = typeof(SecondLevelDescendantSerializableTestType);
            var expectedType = typeof(OneMoreValueSerializerMock);

            // Act
            var serializer = _metamodelProvider.TryGetTypeSerializer(type);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, expectedType);
        }

        #endregion


        #region Property annotation

        [TestMethod]
        public void Test_that_not_annotated_property_is_not_exists_in_metamodel()
        {
            // Arrange
            var notSerializableTypeProperty = typeof(TestTypeWithProperties).GetProperty("NotAnnotatedNotSerializableType");
            var serializableTypeProperty = typeof(TestTypeWithProperties).GetProperty("NotAnnotatedSerializableType");

            // Act
            var notSerializablePropertySerializer = _metamodelProvider.TryGetPropertySerializer(notSerializableTypeProperty);
            var serializablePropertySerializer = _metamodelProvider.TryGetPropertySerializer(serializableTypeProperty);

            // Assert
            Assert.IsNull(notSerializablePropertySerializer);
            Assert.IsNull(serializablePropertySerializer);
        }

        [TestMethod]
        public void Test_that_simple_property_annotations_works()
        {
            // Arrange
            var notSerializableTypeProperty = typeof(TestTypeWithProperties).GetProperty("AnnotatedNotSerializableType");
            var serializableTypeProperty = typeof(TestTypeWithProperties).GetProperty("AnnotatedSerializableType");
            var serializerType = typeof(OneMoreValueSerializerMock);

            // Act
            var notSerializableTypePropertySerializer = _metamodelProvider.TryGetPropertySerializer(notSerializableTypeProperty);
            var serializableTypePropertySerializer = _metamodelProvider.TryGetPropertySerializer(serializableTypeProperty);
            
            // Assert
            Assert.IsNotNull(notSerializableTypePropertySerializer);
            Assert.IsInstanceOfType(notSerializableTypePropertySerializer, serializerType);

            Assert.IsNotNull(serializableTypePropertySerializer);
            Assert.IsInstanceOfType(serializableTypePropertySerializer, serializerType);
        }

        [TestMethod]
        public void Test_that_property_annotations_inherits()
        {
            // Arrange
            var notSerializableTypeProperty = typeof(DescendantTestTypeWithProperties).GetProperty("AnnotatedNotSerializableType");
            var serializableTypeProperty = typeof(DescendantTestTypeWithProperties).GetProperty("AnnotatedSerializableType");
            var serializerType = typeof(OneMoreValueSerializerMock);

            // Act
            var notSerializableTypePropertySerializer = _metamodelProvider.TryGetPropertySerializer(notSerializableTypeProperty);
            var serializableTypePropertySerializer = _metamodelProvider.TryGetPropertySerializer(serializableTypeProperty);

            // Assert
            Assert.IsNotNull(notSerializableTypePropertySerializer);
            Assert.IsInstanceOfType(notSerializableTypePropertySerializer, serializerType);

            Assert.IsNotNull(serializableTypePropertySerializer);
            Assert.IsInstanceOfType(serializableTypePropertySerializer, serializerType);
        }

        [TestMethod]
        public void Test_that_inherited_property_annotations_overrides()
        {
            // Arrange
            var notSerializableTypeProperty = typeof(DescendatnTestTypeWithOverridenProperties).GetProperty("AnnotatedNotSerializableType");
            var serializableTypeProperty = typeof(DescendatnTestTypeWithOverridenProperties).GetProperty("AnnotatedSerializableType");
            var serializerType = typeof(AnotherValueSerializerMock);

            // Act
            var notSerializableTypePropertySerializer = _metamodelProvider.TryGetPropertySerializer(notSerializableTypeProperty);
            var serializableTypePropertySerializer = _metamodelProvider.TryGetPropertySerializer(serializableTypeProperty);

            // Assert
            Assert.IsNotNull(notSerializableTypePropertySerializer);
            Assert.IsInstanceOfType(notSerializableTypePropertySerializer, serializerType);

            Assert.IsNotNull(serializableTypePropertySerializer);
            Assert.IsInstanceOfType(serializableTypePropertySerializer, serializerType);
        }

        #endregion
    }
}

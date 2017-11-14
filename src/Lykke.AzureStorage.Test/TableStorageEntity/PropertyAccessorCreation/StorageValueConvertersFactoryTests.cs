using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.PropertyAccessorCreation;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.AzureStorage.Test.TableStorageEntity.PropertyAccessorCreation
{
    [TestClass]
    public class StorageValueConvertersFactoryTests
    {
        #region Mocks

        [UsedImplicitly]
        private class SerializableTestType
        {
        }

        [UsedImplicitly]
        private class NotSerializableTestType
        {
        }

        private class TestEntity
        {
            [UsedImplicitly]
            public int AzureKnownTypeProperty { get; set; }
            [UsedImplicitly]
            public SerializableTestType SerializableTypeProperty { get; set; }
            [UsedImplicitly]
            public TimeSpan ConvertableTypeProperty { get; set; }
            [UsedImplicitly]
            public NotSerializableTestType NotSerializableTypeProperty { get; set; }
        }

        #endregion

        private static readonly Type EntityType = typeof(TestEntity);
        private IEntityMetamodel _metamodel;

        [TestInitialize]
        public void InitializeTest()
        {
            var metamodelProvider = new ImperativeMetamodelProvider();
            var serializerMock = new Mock<IStorageValueSerializer>();

            metamodelProvider.Register<SerializableTestType>(serializerMock.Object);

            _metamodel = new EntityMetamodelImpl(metamodelProvider);
        }

        [TestMethod]
        public void Test_that_for_azure_known_type_the_pass_through_converter_is_used()
        {
            // Arrange
            var factory = new StorageValueConvertersFactory(_metamodel);

            // Act
            var converter = factory.Create(EntityType.GetProperty(nameof(TestEntity.AzureKnownTypeProperty)));

            // Assert
            Assert.IsNotNull(converter);
            Assert.IsInstanceOfType(converter, typeof(PassThroughStorageValueConverter));
        }

        [TestMethod]
        public void Test_that_for_not_azure_known_but_serializable_type_the_serializer_converter_is_used()
        {
            // Arrange
            var factory = new StorageValueConvertersFactory(_metamodel);

            // Act
            var converter = factory.Create(EntityType.GetProperty(nameof(TestEntity.SerializableTypeProperty)));
           
            // Assert
            Assert.IsNotNull(converter);
            Assert.IsInstanceOfType(converter, typeof(SerializerStorageValueConverter));
        }

        [TestMethod]
        public void Test_that_for_not_azure_known_and_not_serializable_but_convertible_type_the_type_converter_is_used()
        {
            // Arrange
            var factory = new StorageValueConvertersFactory(_metamodel);

            // Act
            var converter = factory.Create(EntityType.GetProperty(nameof(TestEntity.ConvertableTypeProperty)));

            // Assert
            Assert.IsNotNull(converter);
            Assert.IsInstanceOfType(converter, typeof(TypeDescriptorStringStorageValueConverter));
        }

        [TestMethod]
        public void Test_that_for_not_azure_known_and_not_serializable_and_not_convertible_types_converter_cant_be_creates()
        {
            // Arrange
            var factory = new StorageValueConvertersFactory(_metamodel);

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() => factory.Create(EntityType.GetProperty(nameof(TestEntity.NotSerializableTypeProperty))));
        }
    }
}

using System;
using System.Reflection;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Metamodel.Providers
{
    [TestClass]
    public class CompositeMetamodelProviderTest
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
            [UsedImplicitly]
            public int Property { get; set; }
        }

        #endregion


        #region Type serializer

        [TestMethod]
        public void Test_that_null_is_returned_if_all_providers_return_no_serializer_when_getting_type_serializer()
        {
            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t => null);

            nestedProvider2Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t => null);

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object);

            // Act
            var serializer = metamodelProvider.TryGetTypeSerializer(typeof(TestType));

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_provider_used_in_the_order_which_they_was_registered_when_getting_type_serializer()
        {
            // Arrange
            var counter = 0;
            var mock1Order = 0;
            var mock2Order = 0;

            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t =>
                {
                    ++counter;
                    mock1Order = counter;

                    return null;
                });

            nestedProvider2Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t =>
                {
                    ++counter;
                    mock2Order = counter;

                    return null;
                });

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object);

            // Act
            metamodelProvider.TryGetTypeSerializer(typeof(TestType));

            // Assert
            nestedProvider1Mock.Verify(x => x.TryGetTypeSerializer(It.IsAny<Type>()), Times.Once);
            nestedProvider2Mock.Verify(x => x.TryGetTypeSerializer(It.IsAny<Type>()), Times.Once);

            Assert.AreEqual(1, mock1Order);
            Assert.AreEqual(2, mock2Order);
        }

        [TestMethod]
        public void Test_that_serializer_returned_by_provider_which_registered_before_is_used_and_no_registered_later_providers_are_used_when_getting_type_serializer()
        {
            // Arrange
            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();
            var nestedProvider3Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t => null);

            nestedProvider2Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t => new ValueSerializerMock());

            nestedProvider3Mock
                .Setup(x => x.TryGetTypeSerializer(It.IsAny<Type>()))
                .Returns<Type>(t => new AnotherValueSerializerMock());

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object)
                .AddProvider(nestedProvider3Mock.Object);

            // Act
            var serializer = metamodelProvider.TryGetTypeSerializer(typeof(TestType));

            // Assert
            nestedProvider1Mock.Verify(x => x.TryGetTypeSerializer(It.IsAny<Type>()), Times.Once);
            nestedProvider2Mock.Verify(x => x.TryGetTypeSerializer(It.IsAny<Type>()), Times.Once);
            nestedProvider3Mock.Verify(x => x.TryGetTypeSerializer(It.IsAny<Type>()), Times.Never);

            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }

        #endregion


        #region Property serializer

        [TestMethod]
        public void Test_that_null_is_returned_if_all_providers_return_no_serializer_when_getting_property_serializer()
        {
            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t => null);

            nestedProvider2Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t => null);

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object);

            // Act
            var serializer = metamodelProvider.TryGetPropertySerializer(typeof(TestType).GetProperty(nameof(TestType.Property)));

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_provider_used_in_the_order_which_they_was_registered_when_getting_property_serializer()
        {
            // Arrange
            var counter = 0;
            var mock1Order = 0;
            var mock2Order = 0;

            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t =>
                {
                    ++counter;
                    mock1Order = counter;

                    return null;
                });

            nestedProvider2Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t =>
                {
                    ++counter;
                    mock2Order = counter;

                    return null;
                });

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object);

            // Act
            metamodelProvider.TryGetPropertySerializer(typeof(TestType).GetProperty(nameof(TestType.Property)));

            // Assert
            nestedProvider1Mock.Verify(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()), Times.Once);
            nestedProvider2Mock.Verify(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()), Times.Once);

            Assert.AreEqual(1, mock1Order);
            Assert.AreEqual(2, mock2Order);
        }

        [TestMethod]
        public void Test_that_serializer_returned_by_provider_which_registered_before_is_used_and_no_registered_later_providers_are_used_when_getting_property_erializer()
        {
            // Arrange
            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();
            var nestedProvider3Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t => null);

            nestedProvider2Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t => new ValueSerializerMock());

            nestedProvider3Mock
                .Setup(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()))
                .Returns<PropertyInfo>(t => new AnotherValueSerializerMock());

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object)
                .AddProvider(nestedProvider3Mock.Object);

            // Act
            var serializer = metamodelProvider.TryGetPropertySerializer(typeof(TestType).GetProperty(nameof(TestType.Property)));

            // Assert
            nestedProvider1Mock.Verify(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()), Times.Once);
            nestedProvider2Mock.Verify(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()), Times.Once);
            nestedProvider3Mock.Verify(x => x.TryGetPropertySerializer(It.IsAny<PropertyInfo>()), Times.Never);

            Assert.IsNotNull(serializer);
            Assert.IsInstanceOfType(serializer, typeof(ValueSerializerMock));
        }

        #endregion


        #region Type value type merging strategy

        [TestMethod]
        public void Test_that_null_is_returned_if_all_providers_return_no_value_type_merging_strategy()
        {
            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t => ValueTypeMergingStrategy.None);

            nestedProvider2Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t => ValueTypeMergingStrategy.None);

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object);

            // Act
            var serializer = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(TestType));

            // Assert
            Assert.AreEqual(ValueTypeMergingStrategy.None, serializer);
        }

        [TestMethod]
        public void Test_that_provider_used_in_the_order_which_they_was_registered_when_getting_type_value_type_merging_strategy()
        {
            // Arrange
            var counter = 0;
            var mock1Order = 0;
            var mock2Order = 0;

            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t =>
                {
                    ++counter;
                    mock1Order = counter;

                    return ValueTypeMergingStrategy.None;
                });

            nestedProvider2Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t =>
                {
                    ++counter;
                    mock2Order = counter;

                    return ValueTypeMergingStrategy.None;
                });

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object);

            // Act
            metamodelProvider.TryGetValueTypeMergingStrategy(typeof(TestType));

            // Assert
            nestedProvider1Mock.Verify(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()), Times.Once);
            nestedProvider2Mock.Verify(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()), Times.Once);

            Assert.AreEqual(1, mock1Order);
            Assert.AreEqual(2, mock2Order);
        }

        [TestMethod]
        public void Test_that_value_type_merging_strategy_returned_by_provider_which_registered_before_is_used_and_no_registered_later_providers_are_used()
        {
            // Arrange
            var nestedProvider1Mock = new Mock<IMetamodelProvider>();
            var nestedProvider2Mock = new Mock<IMetamodelProvider>();
            var nestedProvider3Mock = new Mock<IMetamodelProvider>();

            nestedProvider1Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t => ValueTypeMergingStrategy.None);

            nestedProvider2Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t => ValueTypeMergingStrategy.UpdateIfDirty);

            nestedProvider3Mock
                .Setup(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()))
                .Returns<Type>(t => ValueTypeMergingStrategy.UpdateAlways);

            var provider = new CompositeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;
            provider
                .AddProvider(nestedProvider1Mock.Object)
                .AddProvider(nestedProvider2Mock.Object)
                .AddProvider(nestedProvider3Mock.Object);

            // Act
            var strategy = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(TestType));

            // Assert
            nestedProvider1Mock.Verify(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()), Times.Once);
            nestedProvider2Mock.Verify(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()), Times.Once);
            nestedProvider3Mock.Verify(x => x.TryGetValueTypeMergingStrategy(It.IsAny<Type>()), Times.Never);

            Assert.AreEqual(ValueTypeMergingStrategy.UpdateIfDirty, strategy);
        }

        #endregion
    }
}

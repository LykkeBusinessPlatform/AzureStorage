using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Metamodel.Providers
{
    [TestClass]
    public class ConventionBaseMetamodelProviderTests
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

        private class TestType
        {
            [UsedImplicitly]
            public decimal Property { get; set; }
        }

        private class AnotherTestType
        {
            [UsedImplicitly]
            public string Property { get; set; }
        }

        #endregion


        #region Type rules

        [TestMethod]
        public void Test_that_for_no_matched_type_null_serializer_is_returned()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeRule(
                    t => t.Name == "FictionalTipe",
                    t => new ValueSerializerMock());
            // Act
            var serializer = metamodelProvider.TryGetTypeSerializer(typeof(TestType));

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_type_rules_applied_in_the_same_order_as_they_was_registered1()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeRule(
                    t => t.Name.EndsWith("TestType"),
                    t => new ValueSerializerMock())
                .AddTypeRule(
                    t => t.Name == "AnotherTestType",
                    t => new AnotherValueSerializerMock());
            // Act
            var testTypeSerializer = metamodelProvider.TryGetTypeSerializer(typeof(TestType));
            var anotherTestTypeSerializer = metamodelProvider.TryGetTypeSerializer(typeof(AnotherTestType));

            // Assert
            Assert.IsNotNull(testTypeSerializer);
            Assert.IsNotNull(anotherTestTypeSerializer);
            Assert.IsInstanceOfType(testTypeSerializer, typeof(ValueSerializerMock));
            Assert.IsInstanceOfType(anotherTestTypeSerializer, typeof(ValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_type_rules_applied_in_the_same_order_as_they_was_registered2()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeRule(
                    t => t.Name == "AnotherTestType",
                    t => new AnotherValueSerializerMock())
                .AddTypeRule(
                    t => t.Name.EndsWith("TestType"),
                    t => new ValueSerializerMock());
            // Act
            var testTypeSerializer = metamodelProvider.TryGetTypeSerializer(typeof(TestType));
            var anotherTestTypeSerializer = metamodelProvider.TryGetTypeSerializer(typeof(AnotherTestType));

            // Assert
            Assert.IsNotNull(testTypeSerializer);
            Assert.IsNotNull(anotherTestTypeSerializer);
            Assert.IsInstanceOfType(testTypeSerializer, typeof(ValueSerializerMock));
            Assert.IsInstanceOfType(anotherTestTypeSerializer, typeof(AnotherValueSerializerMock));
        }

        #endregion


        #region Property rules

        [TestMethod]
        public void Test_that_for_no_matched_property_null_serializer_is_returned()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddPropertyRule(
                    p => p.Name == "FictionalProperty",
                    p => new ValueSerializerMock());
            // Act
            var serializer = metamodelProvider.TryGetPropertySerializer(typeof(TestType).GetProperty(nameof(TestType.Property)));

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_property_rules_applied_in_the_same_order_as_they_was_registered1()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddPropertyRule(
                    p => p.Name == "Property",
                    p => new ValueSerializerMock())
                .AddPropertyRule(
                    p => p.PropertyType == typeof(string),
                    p => new AnotherValueSerializerMock());
            // Act
            var testTypeSerializer = metamodelProvider.TryGetPropertySerializer(typeof(TestType).GetProperty(nameof(TestType.Property)));
            var anotherTestTypeSerializer = metamodelProvider.TryGetPropertySerializer(typeof(AnotherTestType).GetProperty(nameof(AnotherTestType.Property)));

            // Assert
            Assert.IsNotNull(testTypeSerializer);
            Assert.IsNotNull(anotherTestTypeSerializer);
            Assert.IsInstanceOfType(testTypeSerializer, typeof(ValueSerializerMock));
            Assert.IsInstanceOfType(anotherTestTypeSerializer, typeof(ValueSerializerMock));
        }

        [TestMethod]
        public void Test_that_property_rules_applied_in_the_same_order_as_they_was_registered2()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddPropertyRule(
                    p => p.PropertyType == typeof(string),
                    p => new AnotherValueSerializerMock())
                .AddPropertyRule(
                    p => p.Name == "Property",
                    p => new ValueSerializerMock());
            // Act
            var testTypeSerializer = metamodelProvider.TryGetPropertySerializer(typeof(TestType).GetProperty(nameof(TestType.Property)));
            var anotherTestTypeSerializer = metamodelProvider.TryGetPropertySerializer(typeof(AnotherTestType).GetProperty(nameof(AnotherTestType.Property)));

            // Assert
            Assert.IsNotNull(testTypeSerializer);
            Assert.IsNotNull(anotherTestTypeSerializer);
            Assert.IsInstanceOfType(testTypeSerializer, typeof(ValueSerializerMock));
            Assert.IsInstanceOfType(anotherTestTypeSerializer, typeof(AnotherValueSerializerMock));
        }

        #endregion
    }
}

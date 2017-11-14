using System;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Metamodel.Providers
{
    [TestClass]
    public class ConventionBasedMetamodelProviderTests
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
            public decimal Property { get; set; }
        }

        private class AnotherTestType
        {
            [UsedImplicitly]
            public string Property { get; set; }
        }

        #endregion


        #region Type serializer rules

        [TestMethod]
        public void Test_that_for_no_matched_type_null_serializer_is_returned()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeSerializerRule(
                    t => t.Name == "FictionalTipe",
                    t => new ValueSerializerMock());
            // Act
            var serializer = metamodelProvider.TryGetTypeSerializer(typeof(TestType));

            // Assert
            Assert.IsNull(serializer);
        }

        [TestMethod]
        public void Test_that_type_serializer_rules_applied_in_the_same_order_as_they_was_registered1()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeSerializerRule(
                    t => t.Name.EndsWith("TestType"),
                    t => new ValueSerializerMock())
                .AddTypeSerializerRule(
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
        public void Test_that_type_serializer_rules_applied_in_the_same_order_as_they_was_registered2()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeSerializerRule(
                    t => t.Name == "AnotherTestType",
                    t => new AnotherValueSerializerMock())
                .AddTypeSerializerRule(
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


        #region Property serializer rules

        [TestMethod]
        public void Test_that_for_no_matched_property_null_serializer_is_returned()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddPropertySerializerRule(
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
                .AddPropertySerializerRule(
                    p => p.Name == "Property",
                    p => new ValueSerializerMock())
                .AddPropertySerializerRule(
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
                .AddPropertySerializerRule(
                    p => p.PropertyType == typeof(string),
                    p => new AnotherValueSerializerMock())
                .AddPropertySerializerRule(
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


        #region Type value type merging strategy rules

        [TestMethod]
        public void Test_that_for_no_matched_type_null_value_type_merging_strategy_is_returned()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider.AddTypeValueTypesMergingStrategyRule(t => t.Name == "FictionalTipe", ValueTypeMergingStrategy.UpdateIfDirty);

            // Act
            var strategy = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(TestType));

            // Assert
            Assert.IsNull(strategy);
        }

        [TestMethod]
        public void Test_that_type_value_type_merging_strategy_rules_applied_in_the_same_order_as_they_was_registered1()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeValueTypesMergingStrategyRule(t => t.Name.EndsWith("TestType"), ValueTypeMergingStrategy.UpdateIfDirty)
                .AddTypeValueTypesMergingStrategyRule(t => t.Name == "AnotherTestType", ValueTypeMergingStrategy.UpdateAlways);

            // Act
            var testTypeStrategy = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(TestType));
            var anotherTestTypeStrategy = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(AnotherTestType));

            // Assert
            Assert.AreEqual(ValueTypeMergingStrategy.UpdateIfDirty, testTypeStrategy);
            Assert.AreEqual(ValueTypeMergingStrategy.UpdateIfDirty, anotherTestTypeStrategy);
        }

        [TestMethod]
        public void Test_that_type_value_type_merging_strategy_rules_applied_in_the_same_order_as_they_was_registered2()
        {
            // Arrange
            var provider = new ConventionBasedMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            provider
                .AddTypeValueTypesMergingStrategyRule(t => t.Name == "AnotherTestType", ValueTypeMergingStrategy.UpdateAlways)
                .AddTypeValueTypesMergingStrategyRule(t => t.Name.EndsWith("TestType"), ValueTypeMergingStrategy.UpdateIfDirty);
            // Act
            var testTypeStrategy = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(TestType));
            var anotherTestTypeStrategy = metamodelProvider.TryGetValueTypeMergingStrategy(typeof(AnotherTestType));

            // Assert
            Assert.AreEqual(ValueTypeMergingStrategy.UpdateIfDirty, testTypeStrategy);
            Assert.AreEqual(ValueTypeMergingStrategy.UpdateAlways, anotherTestTypeStrategy);
        }

        #endregion
    }
}

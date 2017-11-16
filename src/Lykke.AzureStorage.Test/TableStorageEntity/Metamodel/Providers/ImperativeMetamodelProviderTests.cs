using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.AzureStorage.Tables.Entity.Serializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Metamodel.Providers
{
    [TestClass]
    public class ImperativeMetamodelProviderTests
    {
        private class TestEntity : AzureTableEntity
        {
            public string StringProperty { get; set; }
        }

        private static readonly Type TestEntityType = typeof(TestEntity);

        [TestMethod]
        public void Test_that_property_expression_works1()
        {
            // Arrange
            var provider = new ImperativeMetamodelProvider();
            IMetamodelProvider metamodelProvider = provider;

            // Act
            provider.UseSerializer((TestEntity e) => e.StringProperty, new JsonStorageValueSerializer());

            // Assert
            var serialier = metamodelProvider.TryGetPropertySerializer(TestEntityType.GetProperty(nameof(TestEntity.StringProperty)));

            Assert.IsNotNull(serialier);
            Assert.IsInstanceOfType(serialier, typeof(JsonStorageValueSerializer));
        }
    }
}

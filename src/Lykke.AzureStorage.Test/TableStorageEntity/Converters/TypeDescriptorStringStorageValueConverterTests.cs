using System;
using System.ComponentModel;
using Lykke.AzureStorage.Tables.Entity.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.TableStorageEntity.Converters
{
    [TestClass]
    public class TypeDescriptorStringStorageValueConverterTests
    {
        [TestMethod]
        public void Test_that_builtin_types_are_converted_from_entity_and_backward()
        {
            // Arrange
            var decimalConverter = new TypeDescriptorStringStorageValueConverter(TypeDescriptor.GetConverter(typeof(decimal)));
            var timeSpanConverter = new TypeDescriptorStringStorageValueConverter(TypeDescriptor.GetConverter(typeof(TimeSpan)));

            var decimalValue = 100.123m;
            var timeSpanValue = TimeSpan.FromSeconds(95);

            // Act
            var convertedDecimal = decimalConverter.ConvertFromEntity(decimalValue);
            var convertedTimeSpan = timeSpanConverter.ConvertFromEntity(timeSpanValue);
            var convertedBackDecimal = decimalConverter.ConvertFromStorage(convertedDecimal);
            var convertedBackTimeSpan = timeSpanConverter.ConvertFromStorage(convertedTimeSpan);

            // Assert
            Assert.AreEqual("100.123", convertedDecimal);
            Assert.AreEqual("00:01:35", convertedTimeSpan);
            Assert.AreEqual(decimalValue, convertedBackDecimal);
            Assert.AreEqual(timeSpanValue, convertedBackTimeSpan);
        }
    }
}

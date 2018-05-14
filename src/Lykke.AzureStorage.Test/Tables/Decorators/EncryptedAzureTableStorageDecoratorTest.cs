using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using AzureStorage.Tables.Decorators;
using Lykke.AzureStorage.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.Tables.Decorators
{

    [TestClass]
    public class EncryptedAzureTableStorageDecoratorTest : AzureTableStorageDecoratorTest
    {
        protected readonly ICryptographicSerializer _cryptoSerializer = new AesSerializer("MTIzNDU2Nzg5MDEyMzQ1Njc4OTAxMjM0NTY3ODkwMTI=");
        private readonly INoSQLTableStorage<TestEntity> _innerStorage;

        public EncryptedAzureTableStorageDecoratorTest()
        {
            _innerStorage = new NoSqlTableInMemory<TestEntity>();
            Storage = new EncryptedTableStorageDecorator<TestEntity>(_innerStorage, _cryptoSerializer);
        }

        [TestMethod]
        public async Task InsertAndGetEncryptedValue()
        {
            var entity = new TestEntity(1, "hello", "p1", "r1") { PlainProperty = "hola", SecondPropertyAsEncrypted = "hi" };

            await Storage.InsertAsync(entity);

            await CheckIsEncrypted(entity);
        }

        [TestMethod]
        public async Task TransparentEncryption()
        {
            var entity = new TestEntity(1, "hello", "p1", "r1") { PlainProperty = "hola", SecondPropertyAsEncrypted = _cryptoSerializer.Serialize("hi") };

            await _innerStorage.InsertAsync(entity);

            await CheckIsEncrypted(entity);

            var result = await Storage.GetDataAsync("p1", "r1");
            Assert.AreEqual("hello", result.PropertyAsEncrypted);
            Assert.AreEqual("hola", result.PlainProperty);
            Assert.AreEqual("hi", result.SecondPropertyAsEncrypted);
        }

        private async Task CheckIsEncrypted(TestEntity entity)
        {
            var plain = await Storage.GetDataAsync(entity.PartitionKey, entity.RowKey);
            var encrypted = await _innerStorage.GetDataAsync(entity.PartitionKey, entity.RowKey);

            Assert.IsTrue(_cryptoSerializer.IsEncrypted(encrypted.PropertyAsEncrypted));
            Assert.AreEqual(plain.PropertyAsEncrypted, _cryptoSerializer.Deserialize(encrypted.PropertyAsEncrypted));

            Assert.IsFalse(_cryptoSerializer.IsEncrypted(encrypted.PlainProperty));
            Assert.AreEqual(plain.PlainProperty, encrypted.PlainProperty);

            if (plain.SecondPropertyAsEncrypted != null)
            {
                Assert.IsTrue(_cryptoSerializer.IsEncrypted(encrypted.SecondPropertyAsEncrypted));
                Assert.AreEqual(plain.SecondPropertyAsEncrypted, _cryptoSerializer.Deserialize(encrypted.SecondPropertyAsEncrypted));
            }
        }
    }
}

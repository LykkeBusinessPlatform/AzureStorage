using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureStorage.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using System.Linq;
using AzureStorage;
using Common.Log;
using Lykke.AzureStorage.Test.Mocks;

namespace Lykke.AzureStorage.Test
{
    public class TestEntity : TableEntity
    {
        public string FakeField { get; set; }
        public int Counter { get; set; }
    }

    [TestClass]
    public class AzureTableStorageTest
    {
        private INoSQLTableStorage<TestEntity> _testEntityStorage;

        [TestInitialize]
        public void TestInit()
        {
            _testEntityStorage = new NoSqlTableInMemory<TestEntity>();
        }

        [TestCleanup]
        public async Task TestClean()
        {
            var items = await _testEntityStorage.GetDataAsync();
            await _testEntityStorage.DeleteAsync(items);
        }

        [TestMethod]
        public async Task AzureStorage_CheckInsert()
        {
            TestEntity testEntity = GetTestEntity();

            await _testEntityStorage.InsertAsync(testEntity);
            var createdEntity = await _testEntityStorage.GetDataAsync(testEntity.PartitionKey, testEntity.RowKey);

            Assert.IsNotNull(createdEntity);
        }

        [TestMethod]
        public async Task AzureStorage_CheckParallelInsert()
        {
            var testEntity = GetTestEntity();

            Parallel.For(1, 10, i =>
            {
                _testEntityStorage.CreateIfNotExistsAsync(testEntity).Wait();
            });

            var createdEntity = await _testEntityStorage.GetDataAsync(testEntity.PartitionKey, testEntity.RowKey);

            Assert.IsNotNull(createdEntity);
        }

        private TestEntity GetTestEntity()
        {
            TestEntity testEntity = new TestEntity
            {
                PartitionKey = "TestEntity",
                FakeField = "Test",
                RowKey = Guid.NewGuid().ToString()
            };

            return testEntity;
        }

        [TestMethod]
        public async Task AzureStorage_WithCache_Test()
        {
            var testEntity = GetTestEntity();

            var storage1 = new NoSqlTableInMemory<TestEntity>();

            Parallel.For(1, 10, i =>
            {
                storage1.CreateIfNotExistsAsync(testEntity).Wait();
            });

            var createdEntity = await storage1.GetDataAsync(testEntity.PartitionKey, testEntity.RowKey);

            Assert.IsNotNull(createdEntity);
        }

        [TestMethod]
        public void Test_that_invalid_table_name_throws()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "_asd",
                    new LogToMemory()));

            Assert.ThrowsException<ArgumentException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "1asd",
                    new LogToMemory()));

            Assert.ThrowsException<ArgumentException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "a_sd",
                    new LogToMemory()));

            Assert.ThrowsException<ArgumentException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "a-sd",
                    new LogToMemory()));

            Assert.ThrowsException<ArgumentException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "as",
                    new LogToMemory()));

            Assert.ThrowsException<ArgumentException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), new string('a', 64), 
                    new LogToMemory()));
        }

        [TestMethod]
        public async Task Test_that_valid_table_name_doesnt_throw()
        {
            AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "a1S", new LogToMemory());
            AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), new string('a', 63), new LogToMemory());
        }

        [TestMethod]
        public async Task Verify_InsertAndGenerate_DateTime_RowKey_Increments()
        {
            var testEntity = GetTestEntity();

            var storage1 = new NoSqlTableInMemory<TestEntity>();

            for (int i = 0; i < 1000; ++i)
            {
                testEntity.Counter = i;
                await storage1.InsertAndGenerateRowKeyAsDateTimeAsync(testEntity, DateTime.UtcNow);
            }

            int ct = 0;
            var allEntities = (await storage1.GetDataAsync()).OrderBy(x => x.RowKey);
            foreach (var entity in allEntities)
            {
                Assert.AreEqual<int>(ct, entity.Counter);
                ++ct;
            }
        }
    }
}

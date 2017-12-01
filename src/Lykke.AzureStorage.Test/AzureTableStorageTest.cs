using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AzureStorage.Tables;
using Microsoft.WindowsAzure.Storage.Table;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Lykke.AzureStorage.Test.Mocks;

namespace Lykke.AzureStorage.Test
{
    public class TestEntity : TableEntity
    {
        public string FakeField { get; set; }
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
            Assert.ThrowsException<InvalidOperationException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "_asd",
                    new LogToMemory()));

            Assert.ThrowsException<InvalidOperationException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "1asd",
                    new LogToMemory()));

            Assert.ThrowsException<InvalidOperationException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "a_sd",
                    new LogToMemory()));

            Assert.ThrowsException<InvalidOperationException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "a-sd",
                    new LogToMemory()));

            Assert.ThrowsException<InvalidOperationException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "as",
                    new LogToMemory()));

            Assert.ThrowsException<InvalidOperationException>(() =>
                AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), new string('a', 64), 
                    new LogToMemory()));
        }

        [TestMethod]
        public void Test_that_valid_table_name_doesnt_throw()
        {
            AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), "a1S", new LogToMemory());
            AzureTableStorage<TestEntity>.Create(new ConnStringReloadingManagerMock(""), new string('a', 63), new LogToMemory());
        }
    }
}

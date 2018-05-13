using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lykke.AzureStorage.Test.Tables.Decorators
{

    public abstract class AzureTableStorageDecoratorTest
    {
        protected INoSQLTableStorage<TestEntity> Storage;

        [TestMethod]
        public async Task InsertAndGetValue()
        {
            var entity = new TestEntity(1, "hello", "p1", "r1") { PlainProperty = "hola", SecondPropertyAsEncrypted = "hi" };

            await Storage.InsertAsync(entity);

            var result = await Storage.GetDataAsync("p1", "r1");
            Assert.AreEqual("hello", result.PropertyAsEncrypted);
            Assert.AreEqual("hola", result.PlainProperty);
            Assert.AreEqual("hi", result.SecondPropertyAsEncrypted);
        }

        [TestMethod]
        public async Task InsertRange()
        {
            var dataList = new List<TestEntity>
            {
                new TestEntity(1, "hello 1", "p1", "r1"),
                new TestEntity(2, "hello 2", "p2", "r2"),
                new TestEntity(3, "hello 3", "p3", "r1")
            };

            await Storage.InsertAsync(dataList);
            var resultList = Storage.OrderBy(e => e.Id).ToList();

            Assert.AreEqual(3, resultList.Count);

            Assert.IsTrue(Equals(dataList[0], resultList[0]), "not equal index 0");
            Assert.IsTrue(Equals(dataList[1], resultList[1]), "not equal index 1");
            Assert.IsTrue(Equals(dataList[2], resultList[2]), "not equal index 2");
        }
        // InsertOrMergeAsync(T item)
        // InsertOrMergeBatchAsync(IEnumerable<T> items)

        [TestMethod]
        public async Task Replace()
        {
            var data1 = new TestEntity(15, "hello 1", "p1", "r1");

            await Storage.InsertAsync(data1);

            var resultReplase = await Storage.ReplaceAsync("p1", "r1",
                e => { e.PropertyAsEncrypted = "new message"; return e; });

            var result = await Storage.GetDataAsync("p1", "r1");

            Assert.IsTrue(Equals(result, resultReplase), "results not equals");
            Assert.IsFalse(Equals(data1, result));
            Assert.AreEqual(15, result.Id);
            Assert.AreEqual("new message", result.PropertyAsEncrypted);
        }
        // ReplaceAsync(T entity)

        [TestMethod]
        public async Task Merge()
        {
            var data1 = new TestEntity(15, "hello 1", "p1", "r1");

            await Storage.InsertAsync(data1);

            var resultMerge = await Storage.MergeAsync("p1", "r1",
                e => { e.PropertyAsEncrypted = "new message"; return e; });

            var result = await Storage.GetDataAsync("p1", "r1");

            Assert.IsTrue(Equals(result, resultMerge), "results not equals");
            Assert.IsFalse(Equals(data1, result));
            Assert.AreEqual(15, result.Id);
            Assert.AreEqual("new message", result.PropertyAsEncrypted);
        }

        // InsertOrReplaceBatchAsync(IEnumerable<T> entities)
        [TestMethod]
        public async Task InsertOrReplase()
        {
            var data1 = new TestEntity(1, "hello 1", "p1", "r1");
            var data2 = new TestEntity(2, "hello 2", "p1", "r1");
            var data3 = new TestEntity(3, "hello 3", "p3", "r3");

            await Storage.InsertAsync(data1);
            await Storage.InsertOrReplaceAsync(data2);
            await Storage.InsertOrReplaceAsync(data3);

            var result1 = await Storage.GetDataAsync("p1", "r1");
            var result3 = await Storage.GetDataAsync("p3", "r3");

            Assert.IsTrue(Equals(data2, result1), "not equal data 2 and result 1");
            Assert.IsTrue(Equals(data3, result3), "not equal data 3 and result 3");
        }
        // InsertOrReplaceAsync(IEnumerable<T> items)
        // InsertOrReplaceAsync(T entity, Func<T, bool> condition)
        // InsertOrModifyAsync(string partitionKey, string rowKey, Func<T> create, Func<T, bool> condition)

        [TestMethod]
        public async Task Delete()
        {
            var data1 = new TestEntity(1, "hello 1", "p1", "r1");
            var data2 = new TestEntity(2, "hello 2", "p2", "r2");

            await Storage.InsertAsync(data1);
            await Storage.InsertAsync(data2);

            var deleted = await Storage.DeleteAsync("p2", "r2");

            Assert.IsTrue(Equals(deleted, data2), "not equals deleted and data2");
            Assert.AreEqual(1, Storage.Count());

            await Storage.InsertAsync(data2);
            Assert.AreEqual(2, Storage.Count());

            await Storage.DeleteAsync(data1);
            Assert.AreEqual(1, Storage.Count());
            Assert.IsTrue(Equals(data2, await Storage.GetDataAsync("p2", "r2")), "not equals after delete by entity");

            await Storage.InsertAsync(data1);
            await Storage.DeleteIfExistAsync("p1", "r1");
            await Storage.DeleteIfExistAsync("p1", "r1");
            Assert.AreEqual(1, Storage.Count());
            Assert.IsTrue(Equals(data2, await Storage.GetDataAsync("p2", "r2")), "not equals after delete by entity");
        }
        // DeleteIfExistAsync(string partitionKey, string rowKey)
        // DeleteIfExistAsync(string partitionKey, string rowKey, Func<T, bool> condition)
        // DeleteAsync(IEnumerable<T> items)
        // CreateIfNotExistsAsync(T item)
        // RecordExists(T item)
        // RecordExistsAsync(T item)

        // GetDataAsync(Func<T, bool> filter = null)
        [TestMethod]
        public async Task GetDataEnumerable()
        {
            var data11 = new TestEntity(11, "hello", "p1", "r1");
            var data12 = new TestEntity(12, "hello", "p1", "r2");
            var data13 = new TestEntity(13, "hello 2", "p1", "r3");
            var data21 = new TestEntity(21, "hello", "p2", "r1");

            await Storage.InsertAsync(data11);
            await Storage.InsertAsync(data12);
            await Storage.InsertAsync(data13);
            await Storage.InsertAsync(data21);

            var result = await Storage.GetDataAsync("p1", new List<string> { "r1", "r2" }, 10, x => x.PropertyAsEncrypted == "hello");

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(data11, result.ElementAt(0));
            Assert.AreEqual(data12, result.ElementAt(1));
        }
        // GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        // GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        // GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        // GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        [TestMethod]
        public async Task GetDataByChunksAsync()
        {
            var data11 = new TestEntity(11, "hello", "p1", "r1");
            var data12 = new TestEntity(12, "hello", "p1", "r2");
            var data13 = new TestEntity(13, "hello", "p1", "r3");
            var data21 = new TestEntity(21, "hello", "p2", "r1");

            await Storage.InsertAsync(data11);
            await Storage.InsertAsync(data12);
            await Storage.InsertAsync(data13);
            await Storage.InsertAsync(data21);

            await Storage.GetDataByChunksAsync("p1", chunk =>
            {
                Assert.AreEqual(3, chunk.Count());
                Assert.AreEqual(3, chunk.Count(x => x.PropertyAsEncrypted == "hello"));
            });
        }
        // ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk)
        [TestMethod]
        public async Task ScanDataAsync()
        {
            var data11 = new TestEntity(11, "hello", "p1", "r1");
            var data12 = new TestEntity(12, "hello", "p1", "r2");
            var data13 = new TestEntity(13, "hello", "p1", "r3");
            var data21 = new TestEntity(21, "hello", "p2", "r1");

            await Storage.InsertAsync(data11);
            await Storage.InsertAsync(data12);
            await Storage.InsertAsync(data13);
            await Storage.InsertAsync(data21);

            var result = await Storage.FirstOrNullViaScanAsync("p1", items => items.FirstOrDefault(x => x.PropertyAsEncrypted == "hello"));
            Assert.AreEqual("hello", result.PropertyAsEncrypted);
        }
        // GetDataAsync(string partition, Func<T, bool> filter = null)
        // GetTopRecordAsync(string partition)
        // GetTopRecordsAsync(string partition, int n)
        // GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        // DeleteAsync()
        // CreateTableIfNotExistsAsync()
        // Name
        // this[string partition, string row]
        // this[string partition]
    }
}

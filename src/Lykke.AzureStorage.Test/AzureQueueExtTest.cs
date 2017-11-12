using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using AzureStorage.Queue;

namespace Lykke.AzureStorage.Test
{
    [TestClass]
    public class AzureQueueExtTest
    {
        private IQueueExt _testQueue;

        [TestInitialize]
        public void TestInit()
        {
            _testQueue = new QueueExtInMemory();
        }

        [TestCleanup]
        public async Task TestClean()
        {
            await _testQueue.ClearAsync();
        }

        [TestMethod]
        public async Task AzureQueue_CheckInsert()
        {
            await _testQueue.PutRawMessageAsync("test");

            Assert.AreEqual(1, await _testQueue.Count() ?? 0);
        }

        [TestMethod]
        public async Task AzureQueue_CheckParallelInsert()
        {
            Parallel.For(1, 11, i =>
            {
                _testQueue.PutRawMessageAsync(i.ToString()).Wait();
            });

            Assert.AreEqual(10, await _testQueue.Count() ?? 0);
        }
    }
}

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Blob;

namespace Lykke.AzureStorage.Test
{
    [TestClass]
    public class AzureBlobTest
    {
        private IBlobStorage _testBlob;
        private string _blobContainer = "LykkeAzureBlobTest";


        [TestInitialize]
        public void TestInit()
        {
            _testBlob = new AzureBlobInMemory();
        }

        [TestCleanup]
        public async Task TestClean()
        {
            var items = (await _testBlob.GetListOfBlobKeysAsync(_blobContainer)).ToArray();
            foreach (var item in items)
            {
                await _testBlob.DelBlobAsync(_blobContainer, item);
            }
        }

        [TestMethod]
        public async Task AzureBlob_CheckInsert()
        {
            const string blobName = "Key";

            var data = new byte[] { 0x0, 0xff };

            await _testBlob.SaveBlobAsync(_blobContainer, blobName, new MemoryStream(data));

            using (var result = await _testBlob.GetAsync(_blobContainer, blobName))
            using (var ms = new MemoryStream())
            {
                result.CopyTo(ms);

                CollectionAssert.AreEquivalent(data, ms.ToArray());
            }
        }

        [TestMethod]
        public async Task AzureBlob_CheckParallelInsert()
        {
            Parallel.For(1, 11, i =>
            {
                _testBlob.SaveBlobAsync(_blobContainer, Guid.NewGuid().ToString(), new MemoryStream(new[] { (byte)i })).Wait();
            });

            var items = await _testBlob.GetListOfBlobsAsync(_blobContainer);

            Assert.AreEqual(10, items.Count());
        }
    }
}

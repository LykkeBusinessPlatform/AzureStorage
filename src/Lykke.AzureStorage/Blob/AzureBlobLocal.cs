﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureStorage.Blob
{
    public class AzureBlobLocal : IBlobStorage
    {
        private readonly string _host;

        public AzureBlobLocal(string host)
        {
            _host = host;
        }

        public Stream this[string container, string key] => GetAsync(container, key).GetAwaiter().GetResult();       

		public async Task<string> SaveBlobAsync(string container, string key, Stream bloblStream, bool anonymousAccess = false)
		{
			await PostHttpReqest(container, key, bloblStream.ToBytes());
			return key;
		}

        public Task SaveBlobAsync(string container, string key, byte[] blob)
        {
            return PostHttpReqest(container, key, blob);
        }
        public Task SaveBlobAsync(string container, string key, byte[] blob, IReadOnlyDictionary<string, string> metadata)
        {
            return PostHttpReqest(container, key, blob);
        }

        public Task<bool> HasBlobAsync(string container, string key)
        {
            throw new NotImplementedException();
        }

        public Task<DateTime> GetBlobsLastModifiedAsync(string container)
        {
            return Task.Run(() => DateTime.UtcNow);
        }

        public async Task<Stream> GetAsync(string container, string key)
        {
            return await GetHttpReqestAsync(container, key);
        }

        public Task<string> GetAsTextAsync(string container, string key)
        {
            throw new NotImplementedException();
        }

        public string GetBlobUrl(string container, string key)
        {
            return string.Empty;
        }

        public Task<IEnumerable<string>> FindNamesByPrefixAsync(string container, string prefix)
        {
            throw new NotImplementedException();
        }

        public Task DeleteBlobsByPrefixAsync(string container, string prefix)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetListOfBlobKeysByPrefixAsync(string container, string prefix,
            int? maxResultsCount = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetListOfBlobsAsync(string container)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetListOfBlobKeysAsync(string container, int? maxResultsCount = null)
        {
            throw new NotImplementedException();
        }

        public Task DelBlobAsync(string container, string key)
        {
            throw new NotImplementedException();
        }

        private string CompileRequestString(string container, string id)
        {
            return _host + "/b/" + container + "/" + id;
        }

        private async Task<MemoryStream> GetHttpReqestAsync(string container, string id)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));


                var oWebResponse = await client.GetAsync(CompileRequestString(container, id));


                if ((int) oWebResponse.StatusCode == 201)
                    return null;

                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return ms;
            }
        }

        private async Task<MemoryStream> PostHttpReqest(string container, string id, byte[] data)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                var byteContent = new ByteArrayContent(data);

                var oWebResponse = await client.PostAsync(CompileRequestString(container, id), byteContent);
                var receiveStream = await oWebResponse.Content.ReadAsStreamAsync();

                if (receiveStream == null)
                    throw new Exception("ReceiveStream == null");

                var ms = new MemoryStream();
                receiveStream.CopyTo(ms);
                return ms;
            }
        }


        public void SaveBlob(string container, string key, Stream bloblStream)
        {
            SaveBlobAsync(container, key, bloblStream).Wait();
        }

        public void DelBlob(string container, string key)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CreateContainerIfNotExistsAsync(string container)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMetadataAsync(string container, string key, string metaDataKey)
        {
            return Task.FromResult<string>(null);
        }

        public Task<IDictionary<string, string>> GetMetadataAsync(string container, string key)
        {
            return Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());
        }

        public Task<List<string>> ListBlobsAsync(string container, string path)
        {
            throw new NotImplementedException();
        }

        public Task<BlobProperties> GetPropertiesAsync(string container, string key)
        {
            return Task.FromResult(new BlobProperties());
        }

        public Task<string> AcquireLeaseAsync(string container, string key, TimeSpan? leaseTime, string proposedLeaseId = null)
        {
            throw new NotImplementedException();
        }

        public Task ReleaseLeaseAsync(string container, string key, string leaseId)
        {
            throw new NotImplementedException();
        }

        public Task RenewLeaseAsync(string container, string key, string leaseId)
        {
            throw new NotImplementedException();
        }
    }
}

using AzureStorage.Blob.Decorators;

namespace AzureStorage.Blob
{
    public static class AzureBlobExtensions
    {
        public static IBlobStorage UseExplicitAppInsightsSubmit(this IBlobStorage blobStorage)
        {
            return new ExplicitAppInsightsAzureBlobDecorator(blobStorage);
        }
    }
}

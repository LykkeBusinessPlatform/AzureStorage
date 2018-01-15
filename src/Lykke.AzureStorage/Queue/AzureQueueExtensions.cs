using AzureStorage.Queue.Decorators;

namespace AzureStorage.Queue
{
    public static class AzureQueueExtensions
    {
        public static IQueueExt UseExplicitAppInsightsSubmit(this IQueueExt queue)
        {
            return new ExplicitAppInsightsAzureQueueDecorator(queue);
        }
    }
}

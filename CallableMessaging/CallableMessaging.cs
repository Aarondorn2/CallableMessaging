using Noogadev.CallableMessaging.QueueProviders;

namespace Noogadev.CallableMessaging
{
    public static class CallableMessaging
    {
        public static void Init(Config config)
        {
            config.QueueProvider?.Init(config.DefaultQueueName);
            Publisher.QueueProvider = config.QueueProvider;
            Consumer.QueueProvider = config.QueueProvider;
        }

        public class Config
        {
            public IQueueProvider? QueueProvider { get; set; }
            public string? DefaultQueueName { get; set; }
        }
    }
}

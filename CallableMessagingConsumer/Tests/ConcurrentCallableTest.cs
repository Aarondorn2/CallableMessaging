using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.Tests
{
    /// <summary>
    /// This class is provided as a means to test concurrent callables.
    /// The message on the queue should contain the following serialized Callable Message:
    /// Noogadev.CallableMessagingConsumer.Tests.ConcurrentCallableTest, CallableMessagingConsumer::{}
    /// 
    /// When consumed, this message will spawn 10 concurrent messages; each message will take 5 seconds to process
    /// and will be consumed according to the Concurrency parameter (set to `2`).
    /// You should observe that all messages take ~25s to complete (5s per message, 2 messages at a time). You
    /// should also observe that only 2 messages print "Starting Id" and "Completing Id" in the logs at a time.
    /// </summary>
    public class ConcurrentCallableTest : ICallable
    {
        public async Task CallAsync()
        {
            var callable = new ConcurrentCallableTestInternal();

            for (int i = 0; i < 10; i++)
            {
                callable.Id = i;
                await callable.Publish();
            }
        }
    }

    public class ConcurrentCallableTestInternal : ILoggingCallable, IConcurrentCallable
    {
        public int Concurrency { get; set; } = 2;
        public int Id { get; set; } = 0;

        public async Task CallAsync()
        {
            Logger!.LogInformation($"Starting Id: {Id}");

            await Task.Delay(5000);

            Logger!.LogInformation($"Completing Id: {Id}");
        }

        public string ConcurrentTypeKey() => "test";
        public int ConcurrencyCount() => Concurrency;

        public ILogger? Logger { get; set; } // set by Callable framework

        Task ILoggingCallable.InitLogger(ILogger logger)
        {
	        Logger = logger;
            return Task.CompletedTask;
        }
	}
}

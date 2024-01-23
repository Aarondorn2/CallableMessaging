using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.Tests
{
    /// <summary>
    /// This class is provided as a means to test debounce callables.
    /// The message on the queue should contain the following serialized Callable Message:
    /// Noogadev.CallableMessagingConsumer.Tests.DebounceCallableTest, CallableMessagingConsumer::{}
    /// 
    /// When consumed, this message will spawn 5 debounce messages with a 2 second delay between each one.
    /// In logs, you should observe only the last message logging "Debounce message #5 completed!". It should
    /// take roughly 18s for this message to write as the debounce is 10 seconds and each of the 5 messages
    /// will reset the 10 second timer. 10 + ((5 - 1) * 2) = 18
    /// </summary>
    public class DebounceCallableTest : ICallable
    {
        public async Task CallAsync()
        {
            var callable = new DebounceCallableTestInternal();

            for (int i = 1; i <= 5; i++)
            {
                callable.Id = i;
                await callable.Publish();

                await Task.Delay(2000);
            }
        }
    }

    public class DebounceCallableTestInternal : ILoggingCallable, IDebounceCallable
    {
        public int Id { get; set; } = 0;

        public Task CallAsync()
        {
            Logger!.LogInformation($"Debounce message #{Id} completed!");
            return Task.CompletedTask;
        }

        public TimeSpan DebounceInterval() => TimeSpan.FromSeconds(10);
        public string DebounceTypeKey() => "DebounceTest";

        public string? DebounceInstanceKey { get; set; } // set by Callable framework
        public ILogger? Logger { get; set; } // set by Callable framework

        Task ILoggingCallable.InitLogger(ILogger logger)
        {
	        Logger = logger;
	        return Task.CompletedTask;
        }
    }
}

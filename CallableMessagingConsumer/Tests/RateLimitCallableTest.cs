using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.Tests
{
    /// <summary>
    /// This class is provided as a means to test rate limit callables.
    /// The message on the queue should contain the following serialized Callable Message:
    /// Noogadev.CallableMessagingConsumer.Tests.RateLimitCallableTest, CallableMessagingConsumer::{}
    ///
    /// When consumed, this message will place 9 more messages on the queue with a 1s delay between each.
    /// You should observe that the first 3 process within 3 seconds, but the next 3 consume 10s after the
    /// first group and the final 3 process 20 seconds after the first group.
    /// 
    /// The messages should consume up to 3 messages in a rolling 10s window.
    /// 
    /// Note: since this callable is time-based, running this test multiple times in succession may lead
    /// to observations slightly different than expectations on subsequent runs. In all cases, this callable
    /// should only be consumed at a rate of 3 per 10 seconds.
    /// </summary>
    public class RateLimitCallableTest : ICallable
    {
        public async Task CallAsync()
        {
            var callable = new RateLimitCallableTestInternal();

            for (int i = 0; i < 9; i++)
            {
                callable.Id = i;
                await callable.Publish();
                await Task.Delay(1000);
            }
        }
    }

    public class RateLimitCallableTestInternal : ILoggingCallable, IRateLimitCallable
    {
        public int Id { get; set; } = 0;

        public Task CallAsync()
        {
            Logger!.LogInformation($"Consumed Id: {Id}");
            return Task.CompletedTask;
        }

        public TimeSpan RateLimitPeriod() => TimeSpan.FromSeconds(10);
        public int RateLimitPerPeriod() => 3;
        public string RateLimitTypeKey() => "hi mom";

        public ILogger? Logger { get; set; } // set by Callable framework
    }
}

using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.Tests
{
    /// <summary>
    /// This class is provided as a means to test this lambda by placing a message directly on a queue associated to this Lambda.
    /// The message on the queue should contain the following serialized Callable Message:
    /// Noogadev.CallableMessagingConsumer.Tests.LoggingCallableTest, CallableMessagingConsumer::{\"message\":\"hi mom\"}
    /// </summary>
    public class LoggingCallableTest : ILoggingCallable
    {
        public string Message { get; set; } = "No Message Provided";

        public ILogger? Logger { get; set; } // set by Callable framework

        public Task CallAsync()
        {
            Logger!.LogInformation(Message);
            return Task.CompletedTask;
        }

        Task ILoggingCallable.InitLogger(ILogger logger)
        {
	        Logger = logger;
	        return Task.CompletedTask;
        }
	}
}

using Microsoft.Extensions.DependencyInjection;
using Noogadev.CallableMessaging;
using Noogadev.CallableMessagingConsumer.Services;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.Tests
{
    /// <summary>
    /// This class is provided as a means to test dependency callables.
    /// The message on the queue should contain the following serialized Callable Message:
    /// Noogadev.CallableMessagingConsumer.Tests.DependencyCallableTest, CallableMessagingConsumer::{\"message\":\"hi mom\"}
    /// </summary>
    public class DependencyCallableTest : IDependencyCallable
    {
        public string Message { get; set; } = "No Message Provided";

        public Task CallAsync()
        {
            _testService?.RunTest(Message);
            return Task.CompletedTask;
        }

        private ITestService? _testService;
        public Task InitDependencies(IServiceProvider serviceProvider)
        {
            _testService = serviceProvider.GetService<ITestService>();
            return Task.CompletedTask;
        }
    }
}

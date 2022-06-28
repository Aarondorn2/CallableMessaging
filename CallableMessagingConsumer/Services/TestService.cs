using Microsoft.Extensions.Logging;

namespace Noogadev.CallableMessagingConsumer.Services
{
    public interface ITestService
    {
        public void RunTest(string message);
    }

    /// <summary>
    /// This class provides a simple dependency that can be injected into a callable
    /// message for testing dependency callables.
    /// </summary>
    public class TestService : ITestService
    {
        private readonly ILogger<TestService> _logger;
        public TestService(ILogger<TestService> logger)
        {
            _logger = logger;
        }

        public void RunTest(string message)
        {
            _logger.LogInformation($"TestService RunTest - {message}");
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging.ConsumerContext
{
    /// <summary>
    /// This implementation of <see cref="IConsumerContext"/> provides functionality
    /// for running specialized callable messages locally. Note: all messages will run
    /// synchronously and immediately, so functions like "debounce" and "rate limit"
    /// simply consume each message immediately.
    /// </summary>
    public class LocalConsumerContext : IConsumerContext
    {
        public LocalConsumerContext(ILogger? logger, IServiceProvider? serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider ?? new ServiceCollection().BuildServiceProvider();
        }

        private readonly ILogger? _logger;
        private readonly IServiceProvider _serviceProvider;

        public ILogger? GetLogger()
        {
            return _logger;
        }

        public IServiceProvider GetServiceProvider()
        {
            return _serviceProvider;
        }

        public IConcurrentCallableContext GetConcurrentCallableContext()
        {
            return new LocalConcurrentCallableContext();
        }

        public IDebounceCallableContext GetDebounceCallableContext()
        {
            return new LocalDebounceCallableContext();
        }

        public IRateLimitCallableContext GetRateLimitCallableContext()
        {
            return new LocalRateLimitCallableContext();
        }

        public class LocalConcurrentCallableContext : IConcurrentCallableContext
        {
            public Task ReleaseLock(string typeKey, string? instanceKey)
            {
                return Task.CompletedTask;
            }

            public Task<(bool didLock, string? instanceKey)> TrySetLock(string typeKey, int concurrencyLimit)
            {
                return Task.FromResult((true, (string?)null));
            }
        }

        public class LocalDebounceCallableContext : IDebounceCallableContext
        {
            public Task SetReference(string typeKey, string instanceKey, TimeSpan debounceInterval)
            {
                return Task.CompletedTask;
            }

            public Task<bool> TryRemoveOwnReference(string typeKey, string instanceKey, TimeSpan debounceInterval)
            {
                return Task.FromResult(true);
            }
        }

        public class LocalRateLimitCallableContext : IRateLimitCallableContext
        {
            public Task<TimeSpan?> GetNextAvailableRunTime(string typeKey, int limitPerPeriod, TimeSpan limitPeriod)
            {
                return Task.FromResult((TimeSpan?)null);
            }
        }

        public Task ConsumerFinalizeCall(ICallable callable, string? queueName)
        {
	        return Task.CompletedTask;
        }

        public Task ConsumerPostCall(ICallable callable, string? queueName)
        {
	        return Task.CompletedTask;
        }

        public Task ConsumerPreCall(ICallable callable, string? queueName)
        {
	        return Task.CompletedTask;
        }
    }
}

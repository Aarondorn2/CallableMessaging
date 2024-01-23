using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This class provides a default implementation of the <see cref="IConsumerContext"/>.
    /// If the <see cref="IServiceProvider"/> provided in the constructor has any of the
    /// callable type-specific contexts, this implementation will utilize those, otherwise
    /// it will throw <see cref="NotImplementedException"/> if one of those message types
    /// is consumed.
    /// </summary>
    public class DefaultConsumerContext : IConsumerContext
    {
        public DefaultConsumerContext(ILogger? logger, IServiceProvider? serviceProvider)
        {
            // Do not strictly require DI for logger implementation
            _logger = logger ?? serviceProvider?.GetService<ILogger>();

            _serviceProvider = serviceProvider;
            _concurrentCallableContext = serviceProvider?.GetService<IConcurrentCallableContext>();
            _debounceCallableContext = serviceProvider?.GetService<IDebounceCallableContext>();
            _rateLimitCallableContext = serviceProvider?.GetService<IRateLimitCallableContext>();
        }

        private readonly ILogger? _logger;
        private readonly IServiceProvider? _serviceProvider;
        private readonly IConcurrentCallableContext? _concurrentCallableContext;
        private readonly IDebounceCallableContext? _debounceCallableContext;
        private readonly IRateLimitCallableContext? _rateLimitCallableContext;

        public ILogger? GetLogger()
        {
            return _logger;
        }

        public IServiceProvider GetServiceProvider()
        {
            if (_serviceProvider == null)
            {
                throw new NotImplementedException("Must provide IServiceProvider in ConsumerContext to process IDependencyCallable messages");
            }

            return _serviceProvider;
        }

        public IConcurrentCallableContext GetConcurrentCallableContext()
        {
            if (_concurrentCallableContext == null)
            {
                throw new NotImplementedException("Must provide IConcurrentCallableContext in ConsumerContext to process IConcurrentCallable messages");
            }

            return _concurrentCallableContext;
        }

        public IDebounceCallableContext GetDebounceCallableContext()
        {
            if (_debounceCallableContext == null)
            {
                throw new NotImplementedException("Must provide IDebounceCallableContext in ConsumerContext to process IDebounceCallable messages");
            }

            return _debounceCallableContext;
        }

        public IRateLimitCallableContext GetRateLimitCallableContext()
        {
            if (_rateLimitCallableContext == null)
            {
                throw new NotImplementedException("Must provide IRateLimitCallableContext in ConsumerContext to process IRateLimitCallable messages");
            }

            return _rateLimitCallableContext;
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

using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging.ConsumerContext;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging.QueueProviders
{
    /// <summary>
    /// This provider facilitates running CallableMessages locally without a queueing solution.
    /// This will run messages in serial and will not provide most of the benefits of queueing,
    /// but will be useful when attempting to debug messages during development.
    /// </summary>
    public class LocalQueueProvider : IQueueProvider
    {
        public LocalQueueProvider(ILogger? logger)
        {
            _logger = logger;
        }

        private readonly ILogger? _logger;

        public async Task Enqueue(string messageBody, string? queueName)
        {
            await Consumer.Consume(messageBody, queueName, new LocalConsumerContext(_logger, null));
        }

        public async Task EnqueueBulk(IEnumerable<string> messageBodies, string? queueName)
        {
            foreach (var messageBody in messageBodies)
            {
                await Enqueue(messageBody, queueName);
            }
        }

        public async Task EnqueueDelayed(string messageBody, TimeSpan delay, string? queueName)
        {
            await Task.Delay(delay);
            await Enqueue(messageBody, queueName);
        }

        private Task<bool> TrySetLock(string key) => Task.FromResult(true);
        private Task ReleaseLock(string key) => Task.CompletedTask;
    }
}

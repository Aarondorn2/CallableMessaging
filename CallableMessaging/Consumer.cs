using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging.QueueProviders;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    public static class Consumer
    {
        internal static IQueueProvider? QueueProvider;

        public static async Task Consume(string serializedCallable, ILogger? logger = null)
        {
            logger?.LogInformation($"Consuming: {serializedCallable}");
            var deserialized = Serialization.DeserializeCallable(serializedCallable);
            if (deserialized == null) throw new SerializationException($"Cannot deserialize string as an Callable: {serializedCallable}");

            if (deserialized is ICallable callable)
            {
                logger?.LogInformation($"Calling: {callable.GetType()}");
                await callable.CallAsync();
            }
            else if (deserialized is ILoggingCallable lambdaCallable)
            {
                if (logger == null) throw new Exception("Must pass a logger to `Consume` in order to process LambdaCallable messages");
                logger.LogInformation($"Calling: {lambdaCallable.GetType()}");
                await lambdaCallable.CallAsync(logger);
            }
            else
            {
                logger?.LogError($"Unknown Callable Type: {deserialized.GetType()}");
                throw new Exception($"The callable type {deserialized.GetType()} cannot be processed by the consumer");
            }
        }
    }
}

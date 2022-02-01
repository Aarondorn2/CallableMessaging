using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    public static class Consumer
    {
        /// <summary>
        /// This function should be called in a consumer application that processes queued messages.
        /// It should be passed a serialized callable message that will then be deserialized and invoked.
        /// </summary>
        /// <param name="serializedCallable">The serialized callable message to be invoked.</param>
        /// <param name="logger">An optional logger for use with <see cref="ILoggingCallable"/> messages.</param>
        /// <returns>Task</returns>
        /// <exception cref="SerializationException">Thrown if the provided serializedCallable is not actually a callable message type.</exception>
        /// <exception cref="Exception">Thrown if provided an unknown callable type or a logger is not provided for an <see cref="ILoggingCallable"/> message.</exception>
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

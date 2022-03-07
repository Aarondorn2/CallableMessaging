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
        /// <param name="lockMethods">
        ///     Optional lock functions that accept a string 'key' and attempts to secure/release an exclusive lock.
        ///     The trySetLock function should specify whether the given key was able to obtain an exclusive lock for synchronous processing.
        ///     The releaseLock function should release the exclusive lock placed by trySetLock
        ///     These functions are required to process <see cref="ISynchronousCallable"/> messages.
        /// </param>
        /// <returns>Task</returns>
        /// <exception cref="SerializationException">Thrown if the provided serializedCallable is not actually a callable message type.</exception>
        /// <exception cref="Exception">Thrown if provided an unknown callable type or a consume parameter is not provided for a message type that requires it.</exception>
        public static async Task Consume(
            string serializedCallable,
            ILogger? logger = null,
            (Func<string, Task<bool>> trySetLock, Func<string, Task> releaseLock)? lockMethods = null
        )
        {
            logger?.LogInformation($"Consuming: {serializedCallable}");
            var deserialized = Serialization.DeserializeCallable(serializedCallable);
            if (deserialized == null) throw new SerializationException($"Cannot deserialize string as an Callable: {serializedCallable}");

            string? lockKey = null;
            try
            {
                if (deserialized is ISynchronousCallable syncCallable)
                {
                    if (lockMethods == null) throw new Exception("Must pass a canLock function to `Consume` in order to process SynchronousCallable messages");

                    lockKey = $"{syncCallable.GetType()}+{syncCallable.TypeKey}";
                    if (!await lockMethods.Value.trySetLock(lockKey))
                    {
                        // if we can't get an exclusive lock, something else is processing a message with the same key; retry after 1 second
                        await syncCallable.Publish(TimeSpan.FromSeconds(1));
                        return;
                    }
                }

                if (deserialized is ILoggingCallable loggingCallable)
                {
                    if (logger == null) throw new Exception("Must pass a logger to `Consume` in order to process LoggingCallable messages");
                    logger.LogInformation($"Calling: {loggingCallable.GetType()}");
                    await loggingCallable.CallAsync(logger);
                }
                else if (deserialized is ICallable callable)
                {
                    logger?.LogInformation($"Calling: {callable.GetType()}");
                    await callable.CallAsync();
                }
                else
                {
                    logger?.LogError($"Unknown Callable Type: {deserialized.GetType()}");
                    throw new Exception($"The callable type {deserialized.GetType()} cannot be processed by the consumer");
                }
            }
            finally
            {
                if (lockKey != null) await lockMethods!.Value.releaseLock(lockKey);
            }            
        }
    }
}

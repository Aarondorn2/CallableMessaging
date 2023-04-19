using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="queueName">The name of the queue processing the message. Used for re-queuing messages when rate limited, etc.</param>
        /// <param name="context">An optional <see cref="IConsumerContext"/> object holding methods required to process specific Callable types.</param>
        /// <returns>Task</returns>
        /// <exception cref="SerializationException">Thrown if the provided serializedCallable is not actually a callable message type.</exception>
        /// <exception cref="NotImplementedException">Thrown if provided an unknown callable or a known callable with an unimplemented context requirement.</exception>
        /// <exception cref="Exception"></exception>
        public static async Task Consume(string serializedCallable, string? queueName, Dictionary<string, string>? messageMetadata, IConsumerContext? context = null)
        {
            context ??= new DefaultConsumerContext(null, null);
            var logger = context.GetLogger();

            logger?.LogInformation($"Consuming: {serializedCallable}");
            var deserialized = Serialization.DeserializeCallable(serializedCallable);
            if (deserialized == null) throw new SerializationException($"Cannot deserialize string as an ICallable: {serializedCallable}");

            var messageTypeName = new Lazy<string>(Serialization.GetFullSerializedType(deserialized.GetType()));

            string? concurrentTypeKey = null;
            string? concurrentInstanceKey = null;
            try
            {
                var repeatedCallable = deserialized as IRepeatedCallable;
                if (repeatedCallable != null) Validator.Validate(repeatedCallable);

                if (deserialized is IDebounceCallable debounceCallable)
                {
                    logger?.LogDebug("Processing as debounce callable.");
                    Validator.Validate(debounceCallable);

                    var debounceTypeKey = $"{messageTypeName.Value}+{debounceCallable.DebounceTypeKey()}";
                    var didRemove = await context.GetDebounceCallableContext().TryRemoveOwnReference(
                        debounceTypeKey,
                        debounceCallable.DebounceInstanceKey!,
                        debounceCallable.DebounceInterval());

                    if (!didRemove)
                    {
                        logger?.LogInformation(
                            "DebounceCallable did not remove own reference. Another message of the same type was " +
                            "added prior to the debounce period elapsing and this message has been debounced.");
                        return;
                    }
                }

                if (deserialized is IConcurrentCallable concurrentCallable)
                {
                    logger?.LogDebug("Processing as concurrent callable.");
                    Validator.Validate(concurrentCallable);

                    var concurrentCallableContext = context.GetConcurrentCallableContext();

                    concurrentTypeKey = $"{messageTypeName.Value}+{concurrentCallable.ConcurrentTypeKey()}";
                    var (didLock, instanceKey) =
                        await concurrentCallableContext.TrySetLock(concurrentTypeKey,
                            concurrentCallable.ConcurrencyCount());
                    concurrentInstanceKey = instanceKey;

                    if (!didLock)
                    {
                        // if we can't get an exclusive lock, something else is processing a message with the same key; retry after 1 second
                        await concurrentCallable.Publish(TimeSpan.FromSeconds(1), queueName, messageMetadata);
                        return;
                    }
                }

                if (deserialized is IRateLimitCallable limitCallable)
                {
                    Validator.Validate(limitCallable);

                    logger?.LogDebug("Processing as rate limit callable.");
                    var nextRun = await context.GetRateLimitCallableContext().GetNextAvailableRunTime(
                        $"{messageTypeName.Value}+{limitCallable.RateLimitTypeKey()}",
                        limitCallable.RateLimitPerPeriod(),
                        limitCallable.RateLimitPeriod());

                    if (nextRun != null)
                    {
                        // since the method responded with a delay time, we cannot run yet; we'll need to put this message back on the queue.
                        await limitCallable.Publish(nextRun, queueName, messageMetadata);
                        return;
                    }
                }

                if (deserialized is ILoggingCallable loggingCallable)
                {
                    if (logger == null)
                    {
                        throw new NotImplementedException(
                            "Must provide a logger in IConsumerContext to process LoggingCallable messages");
                    }

                    logger.LogDebug("Processing as logging callable.");
                    await loggingCallable.InitLogger(logger);
                }

                if (deserialized is IDependencyCallable dependencyCallable)
                {
                    logger?.LogDebug("Processing as dependency callable.");
                    await dependencyCallable.InitDependencies(context.GetServiceProvider());
                }

                await context.ConsumerPreCall(deserialized, queueName);

                logger?.LogInformation($"Calling: {deserialized.GetType()}");
                await deserialized.CallAsync();

                await context.ConsumerPostCall(deserialized, queueName);

                if (repeatedCallable?.RepeatedShouldContinueCalling == true)
                {
                    logger?.LogDebug("Post-processing as repeated callable.");
                    repeatedCallable.RepeatedCurrentCall = (repeatedCallable.RepeatedCurrentCall ?? 0) + 1;

                    if (repeatedCallable.RepeatedCurrentCall >= repeatedCallable.RepeatedMaxCalls())
                    {
                        // We reached the max number of times this message can be repeated,
                        // so call CompletedCall with reachedMaxCall set to true
                        await repeatedCallable.CompletedCall(true);
                    }
                    else
                    {
                        logger?.LogInformation(
                            $"Polling Callable retrying after {repeatedCallable.RepeatedTimeBetweenCalls().TotalSeconds} seconds");
                        await repeatedCallable.Publish(repeatedCallable.RepeatedTimeBetweenCalls(), queueName,
                            messageMetadata);
                    }
                }
                else if (repeatedCallable?.RepeatedShouldContinueCalling == false)
                {
                    // We reached the completed state for this message,
                    // so call CompletedCall with reachedMaxCall set to false
                    await repeatedCallable.CompletedCall(false);
                }

                logger?.LogInformation($"Completed: {serializedCallable}");
            }
            catch (Exception e)
            {
                try
                {
                    await deserialized.OnErrorAsync();
                }
                catch (Exception e2)
                {
                    logger?.LogError(e2, "Exception thrown from Callable's OnError function.");
                }

                const string exceptionMessage =
                    "An Exception occured while consuming a callable. See InnerException for details.";
                throw new CallableException(deserialized, exceptionMessage, e);
            }
            finally
            {
                if (concurrentTypeKey != null)
                {
                    await context.GetConcurrentCallableContext().ReleaseLock(concurrentTypeKey, concurrentInstanceKey);
                }

                await context.ConsumerFinalizeCall(deserialized, queueName);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    public static class Publisher
    {
        /// <summary>
        /// Publish a callable message to a queue.
        /// If a queueName is not provided, the <see cref="QueueProviders.IQueueProvider"/> default will be used.
        /// </summary>
        /// <param name="callable">The message to place on a queue.</param>
        /// <param name="delay">The delay to wait prior to delivering the message. `null` signifies immediate delivery.</param>
        /// <param name="queueName">The queue to place the message on. `null` signifies the default queue should be used.</param>
        /// <returns>Task</returns>
        public static async Task Publish(this ICallable callable, TimeSpan? delay = null, string? queueName = null, Dictionary<string, string>? messageMetadata = null)
        {
            if (callable is IDebounceCallable debounceCallable)
            {
                // reuse of instanceKeys is not supported; we're safe to reset this
                // even if the caller has provided a value here.
                debounceCallable.DebounceInstanceKey = Guid.NewGuid().ToString();

                var context = CallableMessaging.GetDebounceContext();
                var debounceTypeKey = $"{Serialization.GetFullSerializedType(debounceCallable.GetType())}+{debounceCallable.DebounceTypeKey()}";
                await context.SetReference(debounceTypeKey, debounceCallable.DebounceInstanceKey, debounceCallable.DebounceInterval());

                delay = debounceCallable.DebounceInterval();
            }

            var serialized = Serialization.SerializeCallable(callable);

            if (delay == null)
            {
                await CallableMessaging.GetQueueProvider().Enqueue(serialized, queueName, messageMetadata);
            }
            else
            {
                await CallableMessaging.GetQueueProvider().EnqueueDelayed(serialized, delay.Value, queueName, messageMetadata);
            }
        }

        /// <summary>
        /// Publish one or more callable message to a queue in batch.
        /// If a queueName is not provided, the <see cref="QueueProviders.IQueueProvider"/> default will be used.
        /// </summary>
        /// <param name="callable">The messages to place on a queue.</param>
        /// <param name="queueName">The queue to place the message on. `null` signifies the default queue should be used.</param>
        /// <returns>Task</returns>
        public static async Task PublishBatch(this IEnumerable<ICallable> callables, string? queueName = null)
        {
            // Handle debounce messages one by one
            foreach (var debounceMessage in callables.Where(x => x is IDebounceCallable))
            {
                await Publish(debounceMessage, null, queueName);
            }

            var messages = callables
                .Where(x => x is not IDebounceCallable)
                .Select(Serialization.SerializeCallable);
            await CallableMessaging.GetQueueProvider().EnqueueBulk(messages, queueName);
        }
    }
}

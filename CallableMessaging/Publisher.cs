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
        public static Task Publish(this ICallableMessagingBase callable, TimeSpan? delay = null, string? queueName = null)
        {
            var serialized = Serialization.SerializeCallable(callable);
            return delay == null
                ? CallableMessaging.GetQueueProvider().Enqueue(serialized, queueName)
                : CallableMessaging.GetQueueProvider().EnqueueDelayed(serialized, delay.Value, queueName);
        }

        /// <summary>
        /// Publish one or more callable message to a queue in batch.
        /// If a queueName is not provided, the <see cref="QueueProviders.IQueueProvider"/> default will be used.
        /// </summary>
        /// <param name="callable">The messages to place on a queue.</param>
        /// <param name="queueName">The queue to place the message on. `null` signifies the default queue should be used.</param>
        /// <returns>Task</returns>
        public static Task PublishBatch(this IEnumerable<ICallableMessagingBase> callables, string? queueName = null)
        {
            var messages = callables.Select(Serialization.SerializeCallable);
            return CallableMessaging.GetQueueProvider().EnqueueBulk(messages, queueName);
        }
    }
}

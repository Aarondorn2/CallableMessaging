using Noogadev.CallableMessaging.QueueProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    public static class Publisher
    {
        internal static IQueueProvider? QueueProvider;

        public static Task Publish(this ICallable callable, string? queueName = null)
        {
            if (QueueProvider == null) throw new Exception("QueueProvider is null; Invoke CallableMessaging.Init() before use.");

            var serialized = Serialization.SerializeCallable(callable);
            return QueueProvider.Enqueue(queueName!, serialized);
        }

        public static Task PublishBatch(this IEnumerable<ICallable> callables, string? queueName = null)
        {
            if (QueueProvider == null) throw new Exception("QueueProvider is null; Invoke CallableMessaging.Init() before use.");

            var messages = callables.Select(Serialization.SerializeCallable);
            return QueueProvider.EnqueueBulk(messages, queueName);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging.QueueProviders
{
    /// <summary>
    /// This interface defines the methods needed to implement a queue provider.
    /// A default AWS SQS implementation is provided (<see cref="AwsQueueProvider"/>) and additional implementations
    /// can be used by implementing this interface
    /// </summary>
    public interface IQueueProvider
    {
        /// <summary>
        /// Add a message to a queue for immediate consumption.
        /// </summary>
        /// <param name="messageBody">The body of the message.</param>
        /// <param name="queueUrl">The URL of the queue to place the message on. `null` implies that a default queue should be used.</param>
        /// <returns>Task</returns>
        public Task Enqueue(string messageBody, string? queueName);

        /// <summary>
        /// Add one or more messages to a queue for immediate consumption.
        /// </summary>
        /// <param name="messageBodies">The bodies of each message.</param>
        /// <param name="queueUrl">The URL of the queue to place the message on. `null` implies that a default queue should be used.</param>
        /// <returns>Task</returns>
        public Task EnqueueBulk(IEnumerable<string> messageBodies, string? queueName);

        /// <summary>
        /// Add a message to a queue for consumption after a specified timespan.
        /// </summary>
        /// <param name="messageBody">The body of the message.</param>
        /// <param name="delay">The delay to wait prior to delivering the message.</param>
        /// <param name="queueUrl">The URL of the queue to place the message on. `null` implies that a default queue should be used.</param>
        /// <returns>Task</returns>
        public Task EnqueueDelayed(string messageBody, TimeSpan delay, string? queueName);
    }
}

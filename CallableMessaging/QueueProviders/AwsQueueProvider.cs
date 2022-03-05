using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging.QueueProviders
{
    /// <summary>
    /// This class is an implementation of <see cref="IQueueProvider"/> that uses AWS SQS for message queues.
    /// It faciliates placing messages on an SQS queue one at a time or in bulk. Additionally, it allows for
    /// delayed message delivery and optional message attributes.
    /// A default queue URL can be configured with the `Init` function and/or a queueUrl can be provided when
    /// queueing a message.
    /// </summary>
    public class AwsQueueProvider : IQueueProvider
    {
        public AwsQueueProvider(string defaultQueueUrl) { DefaultQueueName = defaultQueueUrl; }
        private string? DefaultQueueName { get; }

        /// <summary>
        /// Add a message to a queue for immediate consumption.
        /// </summary>
        /// <param name="messageBody">The body of the message.</param>
        /// <param name="queueUrl">The URL of the queue to place the message on. `null` implies that the initialized default queue URL should be used.</param>
        /// <returns>Task</returns>
        /// <exception cref="Exception">Throws if a queue url is not provided and a default is not configured.</exception>
        public Task Enqueue(string messageBody, string? queueUrl = null) => Enqueue(messageBody, queueUrl, 0);

        /// <summary>
        /// Add a message to a queue with an optional delay and optional attributes.
        /// </summary>
        /// <param name="messageBody">The body of the message.</param>
        /// <param name="queueUrl">The URL of the queue to place the message on. `null` implies that the initialized default queue URL should be used.</param>
        /// <param name="delaySeconds">The number of seconds to delay delivery of the message for consumption. Must be between 0 and 900 (SQS limitation).</param>
        /// <param name="messageAttributes">A dictionary of attributes/metadata to associate to the message.</param>
        /// <returns>Task</returns>
        /// <exception cref="Exception">Throws if a queue url is not provided and a default is not configured or if the delay is outside of boundaries.</exception>
        public async Task Enqueue(string messageBody, string? queueUrl = null, int delaySeconds = 0, Dictionary<string, string>? messageAttributes = null)
        {
            queueUrl ??= DefaultQueueName;
            if (queueUrl == null) throw new Exception("DefaultQueueUrl is null; Please configure before use.");
            if (delaySeconds < 0 || delaySeconds > 900) throw new Exception("Cannot delay less than 0 or more than 900 seconds");

            var message = new SendMessageRequest
            {
                DelaySeconds = delaySeconds,
                MessageBody = messageBody,
                QueueUrl = queueUrl,
                MessageAttributes = messageAttributes?
                    .ToDictionary(x => x.Key, x => new MessageAttributeValue { StringValue = x.Value, DataType = "String" })
                    ?? new()
            };

            using var client = new AmazonSQSClient();
            await client.SendMessageAsync(message);
        }

        /// <summary>
        /// Add one or more messages to a queue for immediate consumption.
        /// </summary>
        /// <param name="messageBodies">The bodies of each message.</param>
        /// <param name="queueUrl">The URL of the queue to place the message on. `null` implies that the initialized default queue URL should be used.</param>
        /// <returns>Task</returns>
        /// <exception cref="Exception">Throws if a queue url is not provided and a default is not configured.</exception>
        public async Task EnqueueBulk(IEnumerable<string> messageBodies, string? queueUrl = null)
        {
            queueUrl ??= DefaultQueueName;
            if (queueUrl == null) throw new Exception("DefaultQueueUrl is null; Please configure before use.");

            var messages = messageBodies
                .Select(x => new SendMessageBatchRequestEntry
                {
                    MessageBody = x
                })
                .ToList();

            using var client = new AmazonSQSClient();
            await client.SendMessageBatchAsync(queueUrl, messages);
        }
    }
}

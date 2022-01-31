using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging.QueueProviders
{
    public class AwsQueueProvider : IQueueProvider
    {
        public string? DefaultQueueName { get; set; }

        public void Init(string? defaultQueueName)
        {
            DefaultQueueName = defaultQueueName;
        }

        public Task Enqueue(string messageBody, string? queueUrl = null) => Enqueue(messageBody, queueUrl, 0);
        public Task Enqueue(string messageBody, string? queueUrl = null, int delaySeconds = 0, Dictionary<string, string>? messageAttributes = null)
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
                    .ToDictionary(x => x.Key, x => new MessageAttributeValue { StringValue = x.Value })
                    ?? new()
            };

            using var client = new AmazonSQSClient();
            return client.SendMessageAsync(message);
        }

        public Task EnqueueBulk(IEnumerable<string> messageBodies, string? queueUrl = null)
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
            return client.SendMessageBatchAsync(queueUrl, messages);
        }
    }
}

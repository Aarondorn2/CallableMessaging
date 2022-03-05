using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Noogadev.CallableMessagingConsumer
{
    public class Function
    {
        private readonly ILogger _logger;
        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function() {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();
            services.AddLogging(x => x.AddLambdaLogger(configuration, "Logger"));

            var provider = services.BuildServiceProvider();
            _logger = (ILogger)provider.GetService(typeof(ILogger<>).MakeGenericType(new[] { this.GetType() }));
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt">the SQS event including a batch of messages to process.</param>
        /// <param name="context">The lambda context (unused in this implementation)</param>
        /// <returns>Task</returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext _)
        {
            foreach(var message in evnt.Records)
            {
                try
                {
                    await Consumer.Consume(message.Body, _logger);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to consume callable message");

                    // if we can't deserialize the message, there is no point in retrying
                    if (e is SerializationException)
                    {
                        await Dlq(message);
                        return;
                    }

                    // if this throws, the message will follow the queue's current Redrive Policy
                    await RetryOrDlq(message);
                }
            }
        }

        /// <summary>
        /// Send the message to a Dead Letter Queue if one is defined for the SQS queue that invokes this lambda.
        /// </summary>
        /// <param name="message">The SQSMessage to place on the DLQ.</param>
        /// <returns>Task</returns>
        private async Task Dlq(SQSEvent.SQSMessage message)
        {
            var currentQueue = GetQueueUrl(message.EventSourceArn);
            var dlq = await GetDlqUrl(currentQueue);
            if (dlq == null)
            {
                _logger.LogError($"No DLQ configured for {currentQueue}");
                return;
            }

            // using queue provider directly since we already have a serialized callable
            var provider = new CallableMessaging.QueueProviders.AwsQueueProvider(dlq);
            await provider.Enqueue(message.Body, dlq);
        }

        /// <summary>
        /// This configuration is used to retry failed messages at increasing intervals.
        /// </summary>
        private static readonly int[] RetryIntervals = new[] { 15, 60, 120, 240 };

        /// <summary>
        /// Retry a failed message a number of times (based on <see cref="RetryIntervals"/>) before sending the message
        /// to a Dead Letter Queue (if one is configured for the SQS queue that invokes this lambda)
        /// </summary>
        /// <param name="message">The SQSMessage to retry or send to the DLQ.</param>
        /// <returns>Task</returns>
        private async Task RetryOrDlq(SQSEvent.SQSMessage message)
        {
            const string retryKey = "callable-retry-count";

            var retryCount = 0;
            if (message.MessageAttributes.TryGetValue(retryKey, out var attribute) && attribute != null)
            {
                int.TryParse(attribute.StringValue, out retryCount);
            }

            if (retryCount >= RetryIntervals.Length)
            {
                _logger.LogInformation($"Message has been retried {retryCount} time(s); Transferring to DLQ.");
                await Dlq(message);
                return;
            }

            var interval = RetryIntervals[retryCount];
            _logger.LogInformation($"Message has been retried {retryCount} time(s); Delaying {interval} seconds and then trying message again.");

            // requeue the message with a delay
            var currentQueue = GetQueueUrl(message.EventSourceArn);
            var messageAttributes = new Dictionary<string, string> { { retryKey, (retryCount + 1).ToString() } };

            // using queue provider directly since we already have a serialized callable
            var provider = new CallableMessaging.QueueProviders.AwsQueueProvider(currentQueue);
            await provider.Enqueue(message.Body, delaySeconds: interval, messageAttributes: messageAttributes);
        }

        /// <summary>
        /// Gets a Queue URL from a Queue ARN.
        /// https://docs.aws.amazon.com/service-authorization/latest/reference/list_amazonsqs.html#amazonsqs-resources-for-iam-policies
        /// </summary>
        /// <param name="arn">The ARN of the Queue.</param>
        /// <returns>string - the URL of the Queue.</returns>
        /// <exception cref="FormatException">thrown if the ARN cannot be parsed.</exception>
        private static string GetQueueUrl(string arn)
        {
            if (!Amazon.Arn.TryParse(arn, out var arnParsed) || !"sqs".Equals(arnParsed.Service, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException("Arn is not in the expected format for an SQS Queue: " + arn);
            }

            return $"https://sqs.{arnParsed.Region}.amazonaws.com/{arnParsed.AccountId}/{arnParsed.Resource}";
        }

        /// <summary>
        /// Gets the Dead Letter Queue associated to a provided Queue if one is configured.
        /// </summary>
        /// <param name="queueUrl">The URL of the Queue used to determine if a DLQ is configured.</param>
        /// <returns>Task<string?> - The URL of the DLQ; `null` if a DLQ is not configured. </returns>
        private static async Task<string?> GetDlqUrl(string queueUrl)
        {
            const string RedriveAttribute = "RedrivePolicy";

            using var client = new AmazonSQSClient();
            var attributeResp = await client.GetQueueAttributesAsync(queueUrl, new[] { RedriveAttribute }.ToList());

            var attribute = attributeResp.Attributes.FirstOrDefault().Value;
            if (string.IsNullOrWhiteSpace(attribute)) return null;

            var attributeDictionary = JsonSerializer.Deserialize<Dictionary<string, object>?>(attribute);
            var dlqArn = (attributeDictionary?.TryGetValue("deadLetterTargetArn", out var val) == true) ? val?.ToString() : null;
            if (string.IsNullOrWhiteSpace(dlqArn)) return null;

            return GetQueueUrl(dlqArn);
        }

        /// <summary>
        /// This class is provided as a means to test this lambda by placing a message directly on a queue associated to this Lambda.
        /// The message on the queue should contain the following serialized Callable Message:
        /// Noogadev.CallableMessagingConsumer.Function+TestConsumer, CallableMessagingConsumer::{\"message\":\"hi mom\"}
        /// </summary>
        public class TestConsumer : ILoggingCallable
        {
            public string? Message { get; set; }

            public Task CallAsync(ILogger logger)
            {
                logger.LogInformation(Message);

                return Task.CompletedTask;
            }
        }
    }
}

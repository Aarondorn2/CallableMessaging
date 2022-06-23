using Amazon.DynamoDBv2;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using Noogadev.CallableMessaging.QueueProviders;
using Noogadev.CallableMessagingConsumer.ConsumerContext;
using Noogadev.CallableMessagingConsumer.Services;
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
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// 
        /// We need to set up our DI container with logging, AWS resources, services, and callable contexts.
        /// </summary>
        public Function() {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .Build();

            var services = new ServiceCollection();
            services
                .AddLogging(x => x.AddLambdaLogger(configuration, "Logger"))
                .AddAWSService<IAmazonDynamoDB>(configuration.GetAWSOptions("DynamoDB"))
                .AddSingleton<IDynamoDbService, DynamoDbService>()
                .AddSingleton<ITestService, TestService>()
                .AddSingleton<IConcurrentCallableContext, ConcurrentCallableContext>()
                .AddSingleton<IDebounceCallableContext, DebounceCallableContext>()
                .AddSingleton<IRateLimitCallableContext, RateLimitCallableContext>();

            _serviceProvider = services.BuildServiceProvider();
            _logger = _serviceProvider.GetService<ILogger<Function>>()!;

            var debounceContext = _serviceProvider.GetService<IDebounceCallableContext>();
            var queueProvider = new AwsQueueProvider(configuration.GetValue<string>("QueueUrl"));
            CallableMessaging.CallableMessaging.Init(queueProvider, debounceContext);
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
        /// to respond to SQS messages.
        /// </summary>
        /// <param name="evnt">the SQS event including a batch of messages to process.</param>
        /// <param name="_">The lambda context (unused in this implementation).</param>
        /// <returns>Task</returns>
        public async Task FunctionHandler(SQSEvent evnt, ILambdaContext _)
        {
            foreach (var message in evnt.Records)
            {
                try
                {
                    using var __ = _logger.BeginScope($"Callable {message.MessageId}");
                    var currentQueueUrl = GetQueueUrl(message.EventSourceArn);

                    try
                    {
                        var consumerContext = new DefaultConsumerContext(_logger, _serviceProvider);
                        await Consumer.Consume(message.Body, currentQueueUrl, consumerContext);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Failed to consume callable message. {e.Message}");

                        // if we can't deserialize the message, there is no point in retrying
                        if (e is SerializationException || !e.CanRetry())
                        {
                            await Dlq(message, currentQueueUrl);
                            continue;
                        }

                        // if this throws, the message will follow the queue's current Redrive Policy
                        await RetryOrDlq(message, currentQueueUrl);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"An unhandled exception occurred while consuming message: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Send the message to a Dead Letter Queue if one is defined for the SQS queue that invokes this lambda.
        /// </summary>
        /// <param name="message">The SQSMessage to place on the DLQ.</param>
        /// <param name="currentQueueUrl">The URL of the queue that the message is being processed from.</param>
        /// <returns>Task</returns>
        private async Task Dlq(SQSEvent.SQSMessage message, string currentQueueUrl)
        {
            var dlq = await GetDlqUrl(currentQueueUrl);
            if (dlq == null)
            {
                _logger.LogError($"No DLQ configured for {currentQueueUrl}");
                return;
            }

            // using queue provider directly since we already have a serialized callable
            var provider = new AwsQueueProvider(dlq);
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
        /// <param name="currentQueueUrl">The URL of the queue that the message is being processed from.</param>
        /// <returns>Task</returns>
        private async Task RetryOrDlq(SQSEvent.SQSMessage message, string currentQueueUrl)
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
                await Dlq(message, currentQueueUrl);
                return;
            }

            var interval = RetryIntervals[retryCount];
            _logger.LogInformation($"Message has been retried {retryCount} time(s); Delaying {interval} seconds and then trying message again.");

            // requeue the message with a delay
            var messageAttributes = new Dictionary<string, string> { { retryKey, (retryCount + 1).ToString() } };

            // using queue provider directly since we already have a serialized callable
            var provider = new AwsQueueProvider(currentQueueUrl);
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
            var dlqArn = attributeDictionary is null
                ? null
                : attributeDictionary.TryGetValue("deadLetterTargetArn", out var val)
                ? val?.ToString()
                : null;
            if (string.IsNullOrWhiteSpace(dlqArn)) return null;

            return GetQueueUrl(dlqArn);
        }
    }

    /// <summary>
    /// This class facilitates an exception being thrown from a callable message without the framework attempting
    /// to retry the failed message. It is recommended that you move this class to a lower-level package so that
    /// the <see cref="WithNoRetry"/> extension can be referenced by other projects.
    /// </summary>
    public static class CallableMessagingConsumerExtensions
    {
        public const string NoRetryKey = "callable-no-retry";

        /// <summary>
        /// Calling this on a thrown exception prevents CallableMessages from attempting to retry the message.
        /// A common use-case for this is when properties in a message are not correctly set.
        /// </summary>
        /// <param name="e">The Exception that is being thrown</param>
        /// <returns>Excpetion - the original exceptions (allows call chaining)</returns>
        public static Exception WithNoRetry(this Exception e)
        {
            e.Data.Add(NoRetryKey, true);
            return e;
        }

        /// <summary>
        /// This method checks to see if an Exception can be retried.
        /// </summary>
        /// <param name="e">The exception to check</param>
        /// <returns>bool - whether or not the exception can be retried</returns>
        public static bool CanRetry(this Exception e)
        {
            return !e.Data.Contains(NoRetryKey) || !bool.TryParse(e.Data[NoRetryKey]?.ToString(), out var val) || !val;
        }
    }
}

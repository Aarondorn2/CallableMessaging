# Callable Messaging Consumer

## Introduction

CallableMessaging is a pattern for generic messaging where the message (instead of a queue/consumer) defines its own processing.

There are a few terms used heavily in this pattern:
* Consumer - an application that listens to and processes messages from the queue(s)
* Producer - an application (or multiple applications) that create messages and place them on the queue(s)
* Callable - an implementation of the `ICallable` interface. This is an object that can be placed on a queue and can be consumed
* Specialty Callable - an implementation of `ICallable` that also implements another callable interface; These other interfaces
provide additional repeatable functionality, such as "debounce" or "rate limit"

Each message will have two parts when serialized; 1) A serialized Type and 2) The serialized object data. When deserialized, the
Type is used to determine what code will be invoked and the object data can be used during that invocation. This means that a 
shared library containing the logic that needs to be run should be included as a dependency in both the Consumer and the Producer;
In order for the Consumer to deserialize the appropriate type, the code that defines the function (the code that extends `ICallable`)
must be resolvable by the Consumer, and in order for it to be placed on the queue, the code must be referenced in the Producer.


# AWS Lambda Simple SQS Function Project

This project consists of:
* appsettings.sample.json - this file should be filled out with your configuration and renamed to `appsettings.json`
* aws-lambda-tools-defaults.sample.json - this file should be filled out with your default argument settings for deployment to AWS (if deploying through VS/CLI) and renamed to `aws-lambda-tools-defaults.json`
* Function.cs - class file containing a single function handler method for processing Callable Messages
* ConsumerContext/ - folder containing context classes used while processing specialized callables
* Services/ - folder containing services used while consuming callable messages
* Tests/ - folder containing sample callables that can be placed on a queue to test various functionality

The function handler responds to events on an Amazon SQS queue.

After deploying your function you must configure an Amazon SQS queue as an event source to trigger your Lambda function (see Queue Configuration below).


## Here are some steps to follow from Visual Studio:

AWS Toolkit must be installed prior to using the following actions:
* To deploy your function to AWS Lambda, right click the project in Solution Explorer and select *Publish to AWS Lambda*.
* To view your deployed function open its Function View window by double-clicking the function name shown beneath the AWS Lambda node in the AWS Explorer tree.
* To perform testing against your deployed function use the Test Invoke tab in the opened Function View window.
* To configure event sources for your deployed function use the Event Sources tab in the opened Function View window.
* To update the runtime configuration of your deployed function use the Configuration tab in the opened Function View window.
* To view execution logs of invocations of your function use the Logs tab in the opened Function View window.


## Here are some steps to follow to get started from the command line:

You can deploy your application using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Deploy function to AWS Lambda
```
    cd "CallableMessagingConsumer/src/CallableMessagingConsumer"
    dotnet lambda deploy-function
```


# Lambda Configuration

Your lambda should be deployed and configured prior to setting up a Queue. After following one of the deployment options outlined above, you can
use AWS Management Console (or VS AWS Toolkit or CLI) to further configure and optimize your Lambda:

* Set Memory and Timeout to reasonable values for your application
* Set permissions / access policies:
    * Must be able to read/write to both the primary Queue and the DLQ (setup steps below)
    * Must be able to write to CloudWatch for logging
    * Must be able to read/write to DynamoDB table (setup steps below)
* Any other desired configurations


# Queue Configuration

Setting up a Queue for invoking this Lambda is a manual process. You can use the AWS Management Console, VS AWS Toolkit, or the CLI.
The following steps will use the AWS Management Console:

* Log in to your AWS Console and navigate to "Simple Queue Service (SQS)"
* First create your DLQ:
    * Click "Create Queue" in top right
    * Use a Standard Queue (FIFO has additional limitations)
    * Give it a name ("CallableMessagingDLQ")
    * Configure Properties
        * Recommended to set retention period as long as you can so the messages do not fall off the queue quickly
        * Leave rest default if unsure
    * Configure Access Policy if further processing these messages
    * Enable Encryption at Rest if desired
    * Leave Redrive Policy and Dead-Letter Queue Disabled
    * Save
* Create your primary Queue:
    * Click "Create Queue" in top right
    * Use a Standard Queue (FIFO has additional limitations)
    * Give it a name ("CallableMessagingQueue") - this must match the queue name in the QueueUrl property of `appsettings.json`
    * Configure Properties
        * Visibility timeout must be at least your Lambda Invocation Timeout (default 900s)
        * Recommended to set retention period as long as you can so the messages do not fall off the queue while being retried
        * Leave rest default if unsure
    * Configure Access Policy (your consumer Lambda and any application placing messages on this queue will need access)
    * Enable Encryption at Rest if desired
    * Leave Redrive Policy Disabled
    * Enable Dead-Letter Queue and choose the DLQ you just configured
    * Save
    * Copy the URL in the details of this Queue - that will be needed when implementing the CallableMessaging.AWSQueueProvider
    * Go to the Lambda Triggers tab and Configure Lambda Function Trigger
    * Select the CallableMessaging Lambda you previous deployed
    * Save


# DynamoDB Configuration

Setting up a Queue for invoking this Lambda is a manual process. You can use the AWS Management Console, VS AWS Toolkit, or the CLI.
The following steps will use the AWS Management Console:

* Log in to your AWS Console and navigate to "DynamoDB"
* Click "Create Table" on the right
* Give it a Table Name ("callable-exclusive-lock") - this name must match the `LockTableName` constant in `Services/DynamoDbService.cs`
* Give it a Partition Key ("type-key") with type `string` - this name must match the `PrimaryKeyName` constant in `Services/DynamoDbService.cs`
* Give it a Sort Key ("instance-key") with type `string` - this name must match the `InstanceKeyName` constant in `Services/DynamoDbService.cs`
* Use default settings (settings can be customized if desired)
* Create table
* Once DynamoDB has completed table creation, click into it and perform the following:
    * Navigate to "Additional Settings"
    * Enable "Time to Live (TTL)"
    * Enter Attribute Name for TTL ("expires-at") - this name must match the `ExpiresAtName` constant in `Services/DynamoDbService.cs`
    * Enable TTL


# Testing Configuration / Setup

Now that all the resources have been added into AWS, you can test your setup and start using CallableMessaging! To test, simply navigate to your
queue, click "Send and receive messages", and then follow the instructions in the summary of each of the `Tests/` classes to run those messages.
Alternatively, you can use VS AWS plugin or an alternative means to publish messages to the queue. You will need to review CloudWatch logs to 
ensure the tests execute as expected.


# Conclusion

That's it! Publish messages to this queue using the copied Queue URL and your Lambda consumer will process them. If you have any questions, issues, or
suggestions, please open an issue in Github.

IMPORTANT NOTE: the business logic that contains each implementation of an ICallable must also be included as a reference to the deployed
instance of `CallableMessagingProvider`. When a message is consumed, the consumer checks its own classpath for the implementation so that it
can invoke the correct `CallAsync()` logic. So both the producer and the consumer must have the ICallable implementation in their classpath
(the producer needs it to call `Publish()` and place it on the queue and the consumer needs it to consume the message).

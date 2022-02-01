# Callable Messaging Consumer

## Introduciton

CallableMessaging is a pattern for generic messaging where the message (instead of a queue/consumer) defines its own processing.

Each message will have two parts when serialized; 1) A serialized Type and 2) The serialized object data. When deserialized, the
Type is used to determine what code will be invoked and the object data can be used during that invokation. This means that a 
shared library containing the logic that needs to be run should be included as a dependency in both the Consumer and the Producer;
In order for the Consumer to deserialize the appropriate type, the code that defines the function (the code that extends `ICallable`)
must be resolvable by the Consumer and in order for it to be originally be placed on the queue, the code must be referenced in the Producer.

# AWS Lambda Simple SQS Function Project

This project consists of:
* Function.cs - class file containing a single function handler method for processing Callable Messages
* aws-lambda-tools-defaults.json - default argument settings for use with Visual Studio and command line deployment tools for AWS

The generated function handler responds to events on an Amazon SQS queue.

After deploying your function you must configure an Amazon SQS queue as an event source to trigger your Lambda function (see Queue Configuration below).


## Here are some steps to follow from Visual Studio:

* AWS Toolkit must be installed prior to using the following actions.
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
* Set permissions (must be able to write to the DLQ if configured and write to CloudWatch for logging)
* Any other desired configurations


# Queue Configuration

Currently, setting up a Queue for invoking this Lambda is a manual process. You can use the AWS Management Console, VS AWS Toolkit, or the CLI.
The following steps will use the AWS Management Console.

* Log in to your AWS Console and navigate to "Simple Queue Service (SQS)"
* First create your DLQ:
    * Click "Create Queue" in top right
    * Use a Standard Queue (FIFO has additional limitations)
    * Give it a name ("CallableMessagingDLQ")
    * Configure Properties
        * recommended to set retention period as long as you can so the messages do not fall off the queue quickly
        * Leave rest default if unsure
    * Configure Access Policy if further processing these messages
    * Enable Ecryption at Rest if desired
    * Leave Redrive Policy and Dead-Letter Queue Disabled
    * Save
* Create your primary Queue:
    * Click "Create Queue" in top right
    * Use a Standard Queue (FIFO has additional limitations)
    * Give it a name ("CallableMessagingQueue")
    * Configure Properties
        * Visibility timeout must be at least your Lambda Invokation Timeout (default 900s)
        * recommended to set retention period as long as you can so the messages do not fall off the queue while being retried
        * Leave rest default if unsure
    * Configure Access Policy (your consumer Lambda and any application placing messages on this queue will need access)
    * Enable Ecryption at Rest if desired
    * Leave Redrive Policy Disabled
    * Enable Dead-Letter Queue and choose the DLQ you just configured
    * Save
    * Copy the URL in the details of this Queue - that will be needed when implementing the CallableMessaging.AWSQueueProvider
    * Go to the Lambda Triggers tab and Configure Lambda Function Trigger
    * Select the CallableMessaging Lambda you previous deployed
    * Save

That's it! Publish messages to this queue using the copied Queue URL and your consumer will try to process them.

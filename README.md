# Introduction

Welcome! Below is a brief description of what CallableMessaging is and how it works. At it's heart CallableMessaging seeks to
make all of the power of messaging more accessible so that it can provide more functionality to your application. As a consequence
of this improved accessibility, more powerful abstractions can be made to provide unique functionality in an easy-to-implement way.
For instance, you can use this project to debounce server-side functions, control concurrent async operations, rate limit a particular 
task, or repeat some desired code for a period of time.

More information on these projects can be found in `CallableMessaging/Readme.md` and `CallableMessagingConsumer/Readme.md`.

The Callable pattern has been a labor of love developed, refined, and used in enterprise applications over the past ~10 years.
Many contributors have added to and improved the concept over the years. It has been implemented in several languages on many different
platforms and is now available as open source.

If you have questions, suggestions, or feedback, please feel free to open a github issue.


# CallableMessaging

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


# Example Usage

Once the infrastructure is set up and the project initialized, using CallableMessaging is as simple as writing a few lines of code similar
to the below. This code would place a message on a queue and asynchronously process the CallAsync() method.

```
public class MyCallable : ICallable
{
    public string MessageToLog { get; set; }

    public Task CallAsync() {
        Console.WriteLine(MessageToLog);
        return Task.CompletedTask();
    }
}

public class MyBusinessLogic
{
    public async Task DoMyWork()
    {
        await new MyCallable
        {
            MessageToLog = "Hi mom"
        }.Publish();
    }
}
```


# Solution Overview

This solution is divided into two projects. There is a `CallableMessaging` project and a `CallableMessagingProvider` project. Both projects
contain their own `Readme.md` files with more details on purpose, setup, and usage.

`CallableMessaging` is a library available through Nuget. It's purpose is to be included in your own business logic projects that need to send
callable messages to a queue. It will provide all the needed interfaces and a `publish()` method for sending messages. It also provides a
`consume()` method that is used by a consumer that listens to the queue. A fully functional consumer is provided in the `CallableMessagingProvider`
project.

`CallableMessagingProvider` is a fully functional consumer that drives off of AWS Lambda, AWS SQS, AWS DynamoDB, and AWS CloudWatch. This
architecture allows use of a single queue with autoscaling and high availability. To deploy this into your environment, you should clone
this Github project, build the solution, and follow the instructions in `CallableMessagingConsumer/Readme.md`. Other than noted changes to
configuration files, no custom code is required to set up your consumer.

IMPORTANT NOTE: the business logic that contains each implementation of an ICallable must also be included as a reference to the deployed
instance of `CallableMessagingProvider`. When a message is consumed, the consumer checks its own classpath for the implementation so that it
can invoke the correct `CallAsync()` logic. So both the producer and the consumer must have the ICallable implementation in their classpath
(the producer needs it to call `Publish()` and place it on the queue and the consumer needs it to consume the message).

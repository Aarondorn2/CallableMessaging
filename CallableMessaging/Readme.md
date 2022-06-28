# Callable Messaging

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


## Usage

This library should be imported through Nuget to an application that is used to consume messages from a queue. It should also be imported
to any application that needs to place messages on a queue. For applications that are queuing messages, the `CallableMessage.Init()` method
should be called with an IQueueProvider implementation before any messages are placed on a queue. The `Init()` method also allows an optional
`IDebounceCallableContext` implementation, which is only required if using `IDebounceCallable` message types.

After initializing, you can publish messages through the Callable pattern using the `ICallable.Publish()` extensions. This will invoke the code
inside the IQueueProvider implementation to place the message on a queue for consumption.

In addition to publishing, you also must create a consumer application to pull the messages off the queue and consume them. This consumer should
similarly `Init()` the project. Once the consumer receives a message, it should pass the message to `Publisher.Publish()` along with an implementation
of IConsumerContext. If not using any specialty callable types (IDebounceCallable, IRateLimitCallable, etc.), then using the DefaultConsumerContext
is sufficient. It using specialty types, you must implement an IConsumerContext that interacts with a database or distributed cache.
Both a sample Consumer (using AWS SQS/Lambda) and a sample implementation of IConsumerContext (using AWS DynamoDB) are available in the
`CallableMessagingConsumer` project.

IMPORTANT NOTE: the business logic that contains each implementation of an ICallable must also be included as a reference to the deployed
instance of `CallableMessagingProvider`. When a message is consumed, the consumer checks its own classpath for the implementation so that it
can invoke the correct `CallAsync()` logic. So both the producer and the consumer must have the ICallable implementation in their classpath
(the producer needs it to call `Publish()` and place it on the queue and the consumer needs it to consume the message).


## IQueueProvider

The IQueueProvider interface is intended to allow for custom implementations of Queue Providers, such as RabbitMQ or AWS SQS. An AwsQueueProvider
is included in the project to demonstrate an integration with AWS SQS. A LocalQueueProvider is also provided as a means to execute callable messages
locally (without using a queue). The LocalQueueProvider is a convenience option - it runs all messages synchronously and ignores message type-specific
processing such as IDebounceCallable and IRateLimitCallable functionality; As such, the LocalQueueProvider may serve to quickly test primary functionality,
but does not represent a true end-to-end test scenario.

You can integrate this project with any queue provider by simply implementing the IQueueProvider interface and providing your own implementation when
calling `CallableMessage.Init()`.


## IConsumerContext

The IConsumerContext houses additional functionality for the `Consumer.Consume()` method related to specific specialty callable types. For instance, the
`GetServiceProvider()` function must be implemented in order to use the IDependencyCallable type and the `GetConcurrentCallableContext` function must be
implemented to use the IConcurrentCallable type. These functions typically require interaction with a synchronized data store, such as a database or distributed
cache. A DefaulConsumerContext is provided for use if no specialty types are required. A LocalConsumerContext is provided for use with LocalQueueProvider. Any 
specialty callable type that is processed by the LocalConsumerContext simply executes and does not process in a special fashion (i.e. debounce messages all execute
and do not actually debounce when running locally. An example of a real implementation (using AWS DynamoDB) is available in the `CallableMessagingConsumer` project.


## Callable Types

This project contains numerous Callable types that provide various specialized functionality. These types and brief descriptions of their usage can be found
in the ICallable class. Examples include DebounceCallable (used to perform a server-side debounce function) and RateLimitCallable (used to rate limit message
consumption).

Custom Callable types can be created by extending ICallabe with a new type and then adding custom logic for that type in the ConsumerPreCall, ConsumerPostCall,
and ConsumerFinalizeCall functions of IConsumer Context. If your type can provide value for others, consider creating a Pull Request for this project!

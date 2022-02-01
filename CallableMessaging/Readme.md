#Introduction

CallableMessaging is a pattern for generic messaging where the message (instead of a queue/consumer) defines its own processing.

Each message will have two parts when serialized; 1) A serialized Type and 2) The serialized object data. When deserialized, the
Type is used to determine what code will be invoked and the object data can be used during that invokation. This means that a 
shared library containing the logic that needs to be run should be included as a dependency in both the Consumer and the Producer;
In order for the Consumer to deserialize the appropriate type, the code that defines the function (the code that extends `ICallable`)
must be resolvable by the Consumer and in order for it to be originally be placed on the queue, the code must be referenced in the Producer.

#Usage

This library should be imported through Nuget to an application that is used to consume messages from a queue. It should also be imported
to any application that needs to place messages on a queue. For applications that are queueing messages, the `CallableMessage.Init()` method
should be called with an IQueueProvider implementation before any messages are placed on a queue. A default AWS SQS Queue Provider is included
in this library for easy use, but any queue provider can be used by implementing your own IQueueProvider and initiazling this library.

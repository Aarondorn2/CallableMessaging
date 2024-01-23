using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This interface houses the context methods needed to consume some specialized
    /// callable messages.
    /// </summary>
    public interface IConsumerContext
    {
        /// <summary>
        /// An optional logger for use with <see cref="ILoggingCallable"/> messages.
        /// </summary>
        /// <returns>ILogger</returns>
        public ILogger? GetLogger();

        /// <summary>
        /// An optional ServiceProvider for use with <see cref="IDependencyCallable"/> messages.
        /// </summary>
        /// <returns>IServiceProvider</returns>
        public IServiceProvider GetServiceProvider();

        /// <summary>
        /// An optional configuration for <see cref="IConcurrentCallable"/> messages.
        /// </summary>
        /// <returns>IConcurrentCallableContext</returns>
        public IConcurrentCallableContext GetConcurrentCallableContext();

        /// <summary>
        /// An optional configuration for <see cref="IDebounceCallable"/> messages.
        /// </summary>
        /// <returns>IDebounceCallableContext</returns>
        public IDebounceCallableContext GetDebounceCallableContext();

        /// <summary>
        /// An optional configuration for <see cref="IRateLimitCallable"/> messages.
        /// </summary>
        /// <returns>IRateLimitCallableContext</returns>
        public IRateLimitCallableContext GetRateLimitCallableContext();

        /// <summary>
        /// Used to provide a hook into the consume method for managing custom Callable types.
        /// If this functionality is not needed, this method can be implemented as a no-op.
        /// 
        /// This function is invoked in the consume method immediately prior to `CallAsync()`.
        /// It should be used to run any custom logic that needs to execute prior to consuming
        /// the callable.
        /// </summary>
        /// <param name="callable">The callable that is being processed.</param>
        /// <param name="queueName">The name of the queue that is processing the callable.</param>
        /// <returns>Task</returns>
        public Task ConsumerPreCall(ICallable callable, string? queueName);

        /// <summary>
        /// Used to provide a hook into the consume method for managing custom Callable types.
        /// If this functionality is not needed, this method can be implemented as a no-op.
        /// 
        /// This function is invoked in the consume method immediately after `CallAsync()`.
        /// It should be used to run any custom logic that needs to execute after consuming
        /// the callable.
        /// </summary>
        /// <param name="callable">The callable that is being processed.</param>
        /// <param name="queueName">The name of the queue that is processing the callable.</param>
        /// <returns>Task</returns>
        public Task ConsumerPostCall(ICallable callable, string? queueName);

        /// <summary>
        /// Used to provide a hook into the consume method for managing custom Callable types.
        /// If this functionality is not needed, this method can be implemented as a no-op.
        /// 
        /// This function is invoked in the consume method in the finally block (after all other
        /// processing). It should be used to run any custom logic that needs to execute after
        /// all other consuming logic.
        /// </summary>
        /// <param name="callable">The callable that is being processed.</param>
        /// <param name="queueName">The name of the queue that is processing the callable.</param>
        /// <returns>Task</returns>
        public Task ConsumerFinalizeCall(ICallable callable, string? queueName);
    }

    /// <summary>
    /// This context interface facilitates the methods needed to implement <see cref="IConcurrentCallable"/> messages.
    /// The implementations of this interface should use a synchronized data store (such as a database or distributed 
    /// cache) to keep track of how many messages of a given type are being executed over a given time period. 
    /// </summary>
    public interface IConcurrentCallableContext
    {
        /// <summary>
        /// Attempt to get an exclusive (or shared) lock for a given typeKey. The concurrencyLimit should control
        /// how many shared locks can be active at one time.
        /// 
        /// Best practice: locks should self-expire after a given time frame (for instance, Lambda invocations are limited to
        /// 15 minutes by default, a sensible self-expiration for locks in this case would be 15 minutes. This will prevent 
        /// locks from remaining due to errors in processing)
        /// </summary>
        /// <param name="typeKey">The type of message for which to release a lock</param>
        /// <param name="concurrencyLimit">How many messages can run concurrently</param>
        /// <returns>
        ///     bool didLock - whether a lock could be set or not. Should be `false` if the concurrrencyLimit was reached
        ///     string instanceKey - a unique key specific to the individual lock record added to the data store. Used to
        ///         release the lock.
        /// </returns>
        public Task<(bool didLock, string? instanceKey)> TrySetLock(string typeKey, int concurrencyLimit);

        /// <summary>
        /// Attempt to remove the exclusive (or shared) lock for a given typeKey.
        /// </summary>
        /// <param name="typeKey">The type of message for which to release a lock</param>
        /// <param name="instanceKey">The unique key of the specific lock instance to release</param>
        /// <returns></returns>
        public Task ReleaseLock(string typeKey, string? instanceKey);
    }

    /// <summary>
    /// This context interface facilitates the methods needed to implement <see cref="IDebounceCallable"/> messages.
    /// The implementations of this interface should use a synchronized data store (such as a database or distributed 
    /// cache) to keep track of the most recent massage and dismiss messages that have been debounced. 
    /// </summary>
    public interface IDebounceCallableContext
    {
        /// <summary>
        /// This method should create or update a record based on the typeKey and associate the instanceKey to that
        /// typeKey. This record represents a "debounce pointer" that will be referenced after the debounce interval
        /// has elapsed.
        /// 
        /// Best practice: references should self-expire after a given time frame (suggested 2x the debounceInterval and
        /// the time frame should be reset each time the instanceKey changes. This will prevent references from remaining
        /// due to errors in processing)
        /// </summary>
        /// <param name="typeKey">The type of debounce message - used to group messages that debounce off each other</param>
        /// <param name="instanceKey">The instance of this particular message - used to determine which message is the latest</param>
        /// <param name="debounceInterval">The interval a message with the given typeKey will wait before executing</param>
        /// <returns></returns>
        public Task SetReference(string typeKey, string instanceKey, TimeSpan debounceInterval);

        /// <summary>
        /// This method should attempt to delete a record for the given typeKey and instanceKey. If a record exists
        /// with the typeKey, but the instanceKey is different, this method should return `false` as the message has
        /// been debounced and should be discarded. If a record exists with the typeKey and instanceKey, this method
        /// should remove the record and return `true` so the message can execute.
        /// 
        /// Best practice: if a record does not exist for the given typeKey, then something went wrong. It is recommended
        /// that you return `true` from this method and add a record to the data store for this typeKey/instanceKey with
        /// an expiration of 2x the debounceInterval. This will prevent any backed-up messages from all executing due to 
        /// returning true.
        /// </summary>
        /// <param name="typeKey">The type of debounce message - used to group messages that debounce off each other</param>
        /// <param name="instanceKey">The instance of this particular message - used to determine which message is the latest</param>
        /// <param name="debounceInterval">The interval a message with the given typeKey will wait before executing</param>
        /// <returns></returns>
        public Task<bool> TryRemoveOwnReference(string typeKey, string instanceKey, TimeSpan debounceInterval);
    }

    /// <summary>
    /// This context interface facilitates the methods needed to implement <see cref="IRateLimitCallable"/> messages.
    /// The implementations of this interface should use a synchronized data store (such as a database or distributed 
    /// cache) to keep track of how many messages of a given type have been executed and over what time period. This
    /// data store should be used by the <see cref="GetNextAvailableRunTime(string, int, TimeSpan)"/> method to determine
    /// if a given message can be invoked or if it needs to be delayed due to reaching the rate limit.
    /// </summary>
    public interface IRateLimitCallableContext
    {
        /// <summary>
        /// This method gets the next available run time. If the limitPerPeriod has been exceeded, then this method 
        /// should return the next time a message can be run. If the limitPerPeriod has not yet been exceeded, this
        /// method should add this current run to the data store and return `null`.
        /// 
        /// Best practice: data store records should self-expire after a given time frame (suggested 1x the limitPeriod.
        /// This will prevent records from remaining due to errors in processing)
        /// 
        /// </summary>
        /// <param name="typeKey">The key to reference the type of <see cref="IRateLimitCallable"/> to rate limit</param>
        /// <param name="limitPerPeriod">The number of times this type of callable can execute in a given time period</param>
        /// <param name="limitPeriod">The time period in which to limit invocations of this callable</param>
        /// <returns>TimeSpan? - null if the callable can execute, otherwise the TimeSpan to wait before next execution</returns>
        public Task<TimeSpan?> GetNextAvailableRunTime(string typeKey, int limitPerPeriod, TimeSpan limitPeriod);
    }
}

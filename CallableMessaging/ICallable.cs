using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// Used to process generic logic.
    /// </summary>
    public interface ICallable
    {
        /// <summary>
        /// Called when the message is consumed.
        /// </summary>
        public Task CallAsync();

        /// <summary>
        /// Called when an error occurs while consuming the message.
        /// Default noop implementation so that this only needs to be
        /// implemented if desired.
        ///
        /// Note: this will not execute if the message fails serialization. 
        /// </summary>
        public Task OnErrorAsync()
        {
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Used to provide the callable with access to a logger.
    /// </summary>
    public interface ILoggingCallable : ICallable
    {
        /// <summary>
        /// A logger to be set by <see cref="InitLogger(ILogger)"/> and utilized in the
        /// CallAsync method of <see cref="ICallable"/>.
        /// 
        /// Should not be serialized/deserialized.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// A method for initializing the logger for a callable message.
        /// This method is invoked by the consumer.
        /// A default implementation is provided.
        /// </summary>
        /// <param name="logger">The logger to initialize</param>
        /// <returns>Task</returns>
        public Task InitLogger(ILogger logger);
    }

    /// <summary>
    /// Used to invoke Dependency Injection on a callable class during consumption.
    /// </summary>
    public interface IDependencyCallable : ICallable
    {
        /// <summary>
        /// A method for initializing dependencies from a ServiceProvider.
        /// This method is invoked by the consumer.
        /// 
        /// Reminder: dependencies set as properties in the Callable object should 
        /// use `[JsonIgnore]` so they are not inadvertently serialized
        /// </summary>
        /// <param name="serviceProvider">The service provider to configure dependencies with</param>
        /// <returns>Task</returns>
        public Task InitDependencies(IServiceProvider serviceProvider);
    }

    /// <summary>
    /// Used to debounce server-side functions. Each message that enters the queue will start a timer
    /// (<see cref="DebounceInterval"/>) before it executes. If another message of the same
    /// <see cref="DebounceTypeKey"/> enters the queue, the previous message is discarded and the timer is reset.
    /// Once the timer runs out, the latest message to enter the queue will be invoked.
    /// </summary>
    public interface IDebounceCallable : ICallable
    {
        /// <summary>
        /// A key to differentiate this message from others of the same <see cref="DebounceTypeKey"/>.
        /// This does not need to be explicitly set and will be overwritten to Guid.NewGuid().ToString().
        /// </summary>
        public string? DebounceInstanceKey { get; set; }

        /// <summary>
        /// A key that groups messages together. If a group of messages need to debounce off each other,
        /// they should have the same TypeKey.
        /// </summary>
        public string DebounceTypeKey();

        /// <summary>
        /// The time to wait for another message of the same <see cref="DebounceTypeKey"/> before this
        /// message is invoked. This message is discarded if another of the same type enters the queue
        /// before the DebounceInterval is over.
        /// </summary>
        public TimeSpan DebounceInterval();
    }

    /// <summary>
    /// Used to limit the number of invocations that occur over a given time period.
    /// For instance, a <see cref="RateLimitPerPeriod"/> of 10 with a <see cref="RateLimitPeriod"/>
    /// of 60 minutes means that the given Callable can be invoked 10 times over an hour before
    /// being invoked again.
    /// 
    /// <see cref="RateLimitPeriod"/> represents a "rolling time frame".
    /// </summary>
    public interface IRateLimitCallable : ICallable
    {
        /// <summary>
        /// The number of invocations that should occur in a given period
        /// </summary>
        public int RateLimitPerPeriod();

        /// <summary>
        /// The period over which to limit invocations.
        /// </summary>
        public TimeSpan RateLimitPeriod();

        /// <summary>
        /// A key that groups messages together. If a group of messages need to be limited together,
        /// they should have the same TypeKey. If an empty key is given, the ICallable message type
        /// is used to group messages.
        /// </summary>
        public string RateLimitTypeKey();
    }

    /// <summary>
    /// Used to process generic logic with controlled concurrency.
    /// IConcurrentCallables can be grouped together by <see cref="ConcurrentTypeKey"/> and will run concurrently
    /// based on the <see cref="ConcurrencyCount"/>.
    /// </summary>
    public interface IConcurrentCallable : ICallable
    {
        /// <summary>
        /// A key that groups messages together. If a group of messages need to run concurrently with each other,
        /// they should have the same TypeKey.
        /// </summary>
        public string ConcurrentTypeKey();

        /// <summary>
        /// The number of concurrent invocations allowed.
        /// </summary>
        public int ConcurrencyCount();
    }

    /// <summary>
    /// Used to repeat a callable message until a threshold is reached. The threshold will be met once the
    /// message has been repeated <see cref="RepeatedMaxCalls"/> number of times OR once 
    /// <see cref="RepeatedShouldContinueCalling"/> is set to false, whichever comes first.
    /// 
    /// This function can be used sparingly to repeat a task that should happen at an interval (not a good approach to
    /// handle cron jobs that should indefinitely be executed on a schedule). This can also be used to implement a 
    /// polling pattern.
    /// </summary>
    public interface IRepeatedCallable : ICallable
    {
        /// <summary>
        /// The current iteration of this message being repeated.
        /// This should not explicitly be set as it is managed internally
        /// by the consumer.
        /// </summary>
        public int? RepeatedCurrentCall { get; set; }
        /// <summary>
        /// Whether to continue repeating this message; Used to short-circuit the message
        /// prior to <see cref="RepeatedMaxCalls"/> being reached.
        /// </summary>
        public bool RepeatedShouldContinueCalling { get; set; }
        /// <summary>
        /// The max number of times this message should be repeated.
        /// </summary>
        public int RepeatedMaxCalls();
        /// <summary>
        /// The amount of time that should pass between repetitions of this message.
        /// </summary>
        public TimeSpan RepeatedTimeBetweenCalls();

        /// <summary>
        /// Called once the message has been repeated <see cref="RepeatedMaxCalls"/> number of times or <see cref="RepeatedShouldContinueCalling"/> is set to false.
        /// </summary>
        /// <param name="reachedMaxCalls">Specifies how this message reached this method. True when <see cref="RepeatedMaxCalls"/> is reached or false when <see cref="RepeatedShouldContinueCalling"/> is used.</param>
        public Task CompletedCall(bool reachedMaxCalls);
    }
}

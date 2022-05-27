using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This interface is used for internal serializtion and interal functions only. It should not be implemented directly
    /// and will fail to consume if implemented directly. Instead, use <see cref="ICallable"/>, <see cref="ILoggingCallable"/>,
    /// or another CallableMessaging interface that includes a `CallAsync` function.
    /// </summary>
    public interface ICallableMessagingBase { }

    /// <summary>
    /// Used to process generic logic.
    /// </summary>
    public interface ICallable : ICallableMessagingBase
    {
        /// <summary>
        /// Called when the message is consumed.
        /// </summary>
        public Task CallAsync();
    }

    /// <summary>
    /// Used to process generic logic with access to a logger.
    /// </summary>
    public interface ILoggingCallable : ICallableMessagingBase
    {
        /// <summary>
        /// Called when the message is consumed.
        /// </summary>
        /// <param name="logger">A logger provided by the consumer.</param>
        public Task CallAsync(ILogger logger);
    }

    /// <summary>
    /// Used to process generic logic in a synchronous fashion.
    /// Only one ISynchronousCallable can process at a time for each key.
    /// Order of message delivery / processing is not garunteed.
    /// 
    /// An ISynchronousCallable must also implement either ICallable or ILoggingCallable to process correctly.
    /// </summary>
    public interface ISynchronousCallable : ICallableMessagingBase
    {
        /// <summary>
        /// A key that groups messages together. If a group of messages need to run synchronously with each other,
        /// they should have the same TypeKey.
        /// </summary>
        public string TypeKey { get; set; }
    }

    /// <summary>
    /// Used to repeat a callable message until a threshold is reached. The threshold will be met once the
    /// message has been repeated <see cref="MaxCalls"/> number of times OR once <see cref="ShouldContinueCalling"/>
    /// is set to false, whichever comes first.
    /// 
    /// This function can be used sparingly to repeat a task that should happen at an interval (not a good approach to
    /// handle cron jobs that should indefinately be executed on a schedule). This can also be used to implement a 
    /// polling pattern.
    /// </summary>
    public interface IRepeatedCallable : ICallableMessagingBase
    {
        /// <summary>
        /// The max number of times this message should be repeated.
        /// </summary>
        public int MaxCalls { get; }
        /// <summary>
        /// The current iteration of this message being repeated.
        /// This should not explicitly be set as it is managed internally
        /// by the consumer.
        /// </summary>
        public int? CurrentCall { get; set; }
        /// <summary>
        /// The amount of time that shouls pass between repetitions of this message.
        /// </summary>
        public TimeSpan TimeBetweenCalls { get; }
        /// <summary>
        /// Whether to continue repeating this message; Used to short-circuit the message
        /// prior to <see cref="MaxCalls"/> being reached.
        /// </summary>
        public bool ShouldContinueCalling { get; set; }

        /// <summary>
        /// Called once the message has been repeated <see cref="MaxCalls"/> number of times or <see cref="ShouldContinueCalling"/> is set to false.
        /// </summary>
        /// <param name="reachedMaxCalls">Specifies how this message reached this method. True when <see cref="MaxCalls"/> is reached or false when <see cref="ShouldContinueCalling"/> is used.</param>
        /// <param name="logger">An optional logger that may be provided if the consumer uses a logger.</param>
        public Task CompletedCall(bool reachedMaxCalls, ILogger? logger);
    }
}

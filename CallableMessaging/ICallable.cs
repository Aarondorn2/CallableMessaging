using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This interface is used for internal serializtion and interal functions only. It should not be implemented directly
    /// and will fail to consume if implemented directly. Instead, use <see cref="ICallable"/>, <see cref="ILoggingCallable"/>,
    /// or another CallableMessaging interface that includes a `CallAsync` function 
    /// </summary>
    public interface ICallableMessagingBase { }

    /// <summary>
    /// Used to process generic logic
    /// </summary>
    public interface ICallable : ICallableMessagingBase
    {
        public Task CallAsync();
    }

    /// <summary>
    /// Used to process generic logic with access to a logger
    /// </summary>
    public interface ILoggingCallable : ICallableMessagingBase
    {
        public Task CallAsync(ILogger logger);
    }

    /// <summary>
    /// Used to process generic logic in a synchronous fashion
    /// Only one ISynchronousCallable can process at a time for each key
    /// Order of message delivery / processing is not garunteed
    /// 
    /// An ISynchronousCallable must also implement either ICallable or ILoggingCallable to process correctly
    /// </summary>
    public interface ISynchronousCallable : ICallableMessagingBase
    {
        /// <summary>
        /// A key that groups messages together. If a group of messages need to run synchronously with each other,
        /// they should have the same TypeKey
        /// </summary>
        public string TypeKey { get; set; }
    }
}

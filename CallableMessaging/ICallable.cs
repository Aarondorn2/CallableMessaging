using Microsoft.Extensions.Logging;
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
}

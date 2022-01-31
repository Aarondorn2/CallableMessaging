using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging
{
    public abstract class CallableMessagingBase { }

    public abstract class ICallable : CallableMessagingBase
    {
        public abstract Task CallAsync();
    }

    public abstract class ILoggingCallable : CallableMessagingBase
    {
        public abstract Task CallAsync(ILogger logger);
    }
}

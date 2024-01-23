using System;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This class facilitates throwing an exception with the preserved ICallable
    /// </summary>
    public class CallableException : Exception
    {
        public CallableException(ICallable callable, string message, Exception e) : base(message, e)
        {
            Callable = callable;
        }
        
        public ICallable Callable { get; set; }
        public string GetSerializedCallable() => Serialization.SerializeCallable(Callable);
    }
}

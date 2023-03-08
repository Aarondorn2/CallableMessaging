using Noogadev.CallableMessaging.QueueProviders;
using System;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This class is used as a central point for statically initializing/configuring the CallableMessaging library.
    /// </summary>
    public static class CallableMessaging
    {
        /// <summary>
        /// The <see cref="IQueueProvider"/> that will be used by the CallableMessaging library.
        /// </summary>
        private static IQueueProvider? QueueProvider { get; set; }

        /// <summary>
        /// A utility method to retrieve the currently configured <see cref="IQueueProvider"/> in use by the
        /// CallableMessaging library. This may be used to extend CallableMessaging functionality in your project.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IQueueProvider GetQueueProvider()
        {
            if (QueueProvider == null) throw new Exception("QueueProvider is null; Invoke `CallableMessaging.Init()` before use.");
            return QueueProvider;
        }

        /// <summary>
        /// The <see cref="IDebounceCallableContext"/> that will be used by the CallableMessaging library when sending
        /// <see cref="IDebounceCallable"/> messages.
        /// </summary>
        private static IDebounceCallableContext? DebounceContext { get; set; }


        /// <summary>
        /// A utility method to retrieve the currently configured <see cref="IDebounceCallableContext"/> in use by the
        /// CallableMessaging library. This may be used to extend CallableMessaging functionality in your project.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IDebounceCallableContext GetDebounceContext()
        {
            if (DebounceContext == null)
            {
                throw new Exception("DebounceContext is null; Set using `CallableMessaging.Init()` before publishing an IDebounceCallable message.");
            }

            return DebounceContext;
        }

        /// <summary>
        /// Initializes which <see cref="IQueueProvider"/> should be used by the CallableMessaging library.
        /// </summary>
        /// <param name="queueProvider">The <see cref="IQueueProvider"/> to use.</param>
        /// <param name="debounceCallableContext">Optional. The <see cref="IDebounceCallableContext"/> to 
        /// use when publishing <see cref="IDebounceCallable"/> messages.</param>
        public static void Init(IQueueProvider? queueProvider, IDebounceCallableContext? debounceCallableContext = null)
        {
            QueueProvider = queueProvider;
            DebounceContext = debounceCallableContext;
        }
    }
}

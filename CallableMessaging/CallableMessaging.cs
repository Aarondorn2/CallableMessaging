using Noogadev.CallableMessaging.QueueProviders;
using System;

namespace Noogadev.CallableMessaging
{
    /// <summary>
    /// This class is used as a central point for statically intializing/configuring the CallableMessaging library.
    /// </summary>
    public static class CallableMessaging
    {
        /// <summary>
        /// The <see cref="IQueueProvider"/> that will be used by the CallableMessaging library.
        /// </summary>
        private static IQueueProvider? QueueProvider { get; set; }
        internal static IQueueProvider GetQueueProvider()
        {
            if (QueueProvider == null) throw new Exception("QueueProvider is null; Invoke `CallableMessaging.Init()` before use.");
            return QueueProvider;
        }

        /// <summary>
        /// Initializes which <see cref="IQueueProvider"/> should be used by the CallableMessaging library.
        /// </summary>
        /// <param name="queueProvider">The <see cref="IQueueProvider"/> to use.</param>
        public static void Init(IQueueProvider? queueProvider)
        {
            QueueProvider = queueProvider;
        }
    }
}

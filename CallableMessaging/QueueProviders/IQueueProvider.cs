using System.Collections.Generic;
using System.Threading.Tasks;

namespace Noogadev.CallableMessaging.QueueProviders
{
    public interface IQueueProvider
    {
        public string? DefaultQueueName { get; set; }

        public void Init(string? defaultQueueName);
        public Task Enqueue(string messageBody, string? queueName);
        public Task EnqueueBulk(IEnumerable<string> messageBodies, string? queueName);
    }
}

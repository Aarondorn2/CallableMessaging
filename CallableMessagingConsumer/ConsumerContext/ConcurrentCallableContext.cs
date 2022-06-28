using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using Noogadev.CallableMessagingConsumer.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.ConsumerContext
{
    /// <summary>
    /// This class implements the methods needed to manage a concurrent callable.
    /// </summary>
    public class ConcurrentCallableContext : IConcurrentCallableContext
    {
        private readonly ILogger _logger;
        private readonly IDynamoDbService _dynamoDbService;

        public ConcurrentCallableContext(ILogger<ConcurrentCallableContext> logger, IDynamoDbService dynamoDbService)
        {
            _logger = logger;
            _dynamoDbService = dynamoDbService;
        }

        public async Task<(bool didLock, string? instanceKey)> TrySetLock(string typeKey, int concurrencyLimit)
        {
            var existing = await _dynamoDbService.GetByType(typeKey);
            if (existing.Count >= concurrencyLimit)
            {
                _logger.LogDebug($"Concurrency reached for typeKey: {typeKey}. Retrying later.");
                return (false, null);
            }

            var instanceKey = Guid.NewGuid().ToString();
            var expiration = TimeSpan.FromMinutes(15); // 15 minutes is the default max execution time of a lambda
            await _dynamoDbService.AddItem(typeKey, instanceKey, expiration);

            var newItems = await _dynamoDbService.GetByType(typeKey);
            if (newItems.Count <= concurrencyLimit)
            {
                _logger.LogDebug($"Completed setting lock for ConcurrentCallable. typeKey: {typeKey}, instanceKey: {instanceKey}");
                return (true, instanceKey);
            }

            // if we went over our limit, then we encountered a concurrency issue. Let's figure out if we were last.	
            var shouldDeleteSelf = newItems
              .Items
              .Select(x => new
              {
                  SetAt = DateTime.TryParse(x.GetValueOrDefault(DynamoDbService.SetAtName)?.S, out var d) ? d : (DateTime?)null,
                  InstanceKey = x.GetValueOrDefault(DynamoDbService.SortKeyName)?.S
              })
              .OrderBy(x => x.SetAt)
              .Skip(concurrencyLimit)
              .Any(x => x.InstanceKey == instanceKey);

            await ReleaseLock(typeKey, instanceKey);

            _logger.LogDebug($"Concurrency reached for typeKey: {typeKey}. Retrying later.");
            return (false, null);
        }

        public async Task ReleaseLock(string typeKey, string? instanceKey)
        {
            if (instanceKey == null) return;

            try
            {
                await _dynamoDbService.DeleteItem(typeKey, instanceKey);
                _logger.LogDebug($"Completed removing lock for ConcurrentCallable. typeKey: {typeKey}, instanceKey: {instanceKey}");
            }
            catch (Exception e){
                _logger.LogWarning($"Failed to remove lock for ConcurrentCallable. Lock should have a TTL. typeKey: {typeKey}, instanceKey: {instanceKey}");
                _logger.LogWarning(e.Message);
            } // if we fail here, the table should have a TTL to eventually remove the lock
        }
    }
}

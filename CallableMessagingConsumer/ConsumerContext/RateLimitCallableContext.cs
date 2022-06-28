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
    /// This class implements the methods needed to manage a rate limit callable.
    /// </summary>
    public class RateLimitCallableContext : IRateLimitCallableContext
    {
        private readonly ILogger _logger;
        private readonly IDynamoDbService _dynamoDbService;

        public RateLimitCallableContext(ILogger<RateLimitCallableContext> logger, IDynamoDbService dynamoDbService)
        {
            _logger = logger;
            _dynamoDbService = dynamoDbService;
        }

        public async Task<TimeSpan?> GetNextAvailableRunTime(string typeKey, int limitPerPeriod, TimeSpan limitPeriod)
        {
            var expiredDate = DateTime.UtcNow.Add(-limitPeriod);
            var existing = (await _dynamoDbService.GetByType(typeKey))
                .Items
                .Select(x => DateTime.TryParse(x.GetValueOrDefault(DynamoDbService.SetAtName)?.S, out var d) ? d : (DateTime?)null)
                .Where(x => x != null && x > expiredDate)
                .Select(x => x!)
                .ToArray();

            if (existing.Length >= limitPerPeriod)
            {
                var oldest = existing.OrderBy(x => x).First();
                var nextRun = CalculateNext(oldest!.Value, limitPeriod);

                _logger.LogDebug($"Exceeded our limit. Retrying in {nextRun.TotalSeconds} seconds. typeKey: {typeKey}, limitPerPeriod: " +
                    $"{limitPerPeriod}, limitPeriod: {limitPeriod}");
                return nextRun;
            }

            var instanceKey = Guid.NewGuid().ToString();
            await _dynamoDbService.AddItem(typeKey, instanceKey, limitPeriod);

            // if we went over our limit, then we encountered a concurrency issue. Let's figure out if we were last.
            var shouldDeleteSelf = (await _dynamoDbService.GetByType(typeKey))
                .Items
                .Select(x => new
                {
                    SetAt = DateTime.TryParse(x.GetValueOrDefault(DynamoDbService.SetAtName)?.S, out var d) ? d : (DateTime?)null,
                    InstanceKey = x.GetValueOrDefault(DynamoDbService.SortKeyName)?.S
                })
                .Where(x => x != null && x.SetAt > expiredDate)
                .OrderBy(x => x.SetAt)
                .ThenBy(x => x.InstanceKey)
                .Skip(limitPerPeriod)
                .Any(x => x.InstanceKey == instanceKey);

            if (shouldDeleteSelf)
            {
                await _dynamoDbService.DeleteItem(typeKey, instanceKey);

                var oldest = existing.OrderBy(x => x).FirstOrDefault();
                var nextRun = oldest == null
                    ? limitPeriod
                    : CalculateNext(oldest.Value, limitPeriod);

                _logger.LogDebug($"Exceeded our limit. Retrying in {nextRun.TotalSeconds} seconds. typeKey: {typeKey}, limitPerPeriod: " +
                    $"{limitPerPeriod}, limitPeriod: {limitPeriod}");
                return nextRun;
            }

            // we added our own lock and are allowed to execute now
            _logger.LogDebug($"Within rate limit - consuming callable. typeKey: {typeKey}, limitPerPeriod: {limitPerPeriod}, limitPeriod: {limitPeriod}");
            return null;
        }

        private TimeSpan CalculateNext(DateTime oldestExisting, TimeSpan limitPeriod)
        {
            var millis = (DateTime.UtcNow - oldestExisting).TotalMilliseconds;
            var diff = TimeSpan.FromMilliseconds(millis);
            var nextAvailable = limitPeriod - diff;

            return nextAvailable <= TimeSpan.FromSeconds(0)
                ? TimeSpan.FromSeconds(1) // only will get here if the item expired while we were processing.
                : nextAvailable;
        }
    }
}

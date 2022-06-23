using Microsoft.Extensions.Logging;
using Noogadev.CallableMessaging;
using Noogadev.CallableMessagingConsumer.Services;
using System;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.ConsumerContext
{
		/// <summary>
		/// This class implements the methods needed to manage a debounce callable.
		/// </summary>
    public class DebounceCallableContext : IDebounceCallableContext
    {
				private const string DebounceSortKey = "debounce";

        private readonly ILogger _logger;
				private readonly IDynamoDbService _dynamoDbService;

				public DebounceCallableContext(ILogger<DebounceCallableContext> logger, IDynamoDbService dynamoDbService)
        {
            _logger = logger;
						_dynamoDbService = dynamoDbService;
				}

        public async Task SetReference(string typeKey, string instanceKey, TimeSpan debounceInterval)
				{
						await _dynamoDbService.AddOrUpdateItem(
								typeKey,
								// We can't update dynamo sort keys. Since we're reusing a table with other callables
								// and we only want a single record (per typeKey) for debounce, just use a dummy 
								// "instaceKey" for this record
								DebounceSortKey,
								// We want to give the message time to be consumed off the queue before expiring its reference
								// The debounce should clean up this record after consumption, so this is just a fail-safe
								debounceInterval + TimeSpan.FromHours(1),
								new() { { DynamoDbService.DebounceKeyName, instanceKey } });

						_logger.LogDebug($"Set reference for DebounceCallable. typeKey: {typeKey}, instanceKey: {instanceKey}");
				}

        public async Task<bool> TryRemoveOwnReference(string typeKey, string instanceKey, TimeSpan debounceInterval)
				{
						try
						{
								var existing = await _dynamoDbService.GetByType(typeKey);
								if (existing.Count == 0)
								{
										// We're in a bad state, go ahead and set our own lock so that queued messages
										// don't all execute
										await SetReference(typeKey, instanceKey, debounceInterval);

										// Return true so this message processes
										return true;
								}

								var didDelete = await _dynamoDbService.TryDeleteItem(
										typeKey,
										DebounceSortKey,
										"#debounceKeyName = :debounceKey",
										new() { { "#debounceKeyName", DynamoDbService.DebounceKeyName } },
										new() { { ":debounceKey", instanceKey } });

								if (!didDelete)
                {
										_logger.LogDebug($"Could not find lock to remove for DebounceCallable. typeKey: {typeKey}, instanceKey: {instanceKey}");
										return false;
								}

								_logger.LogDebug($"Completed removing lock for DebounceCallable. typeKey: {typeKey}, instanceKey: {instanceKey}");
								return true;
						}
						catch (Exception e)
						{
								_logger.LogError($"Failed to remove lock for DebounceCallable. Lock should have a TTL. typeKey: {typeKey}, instanceKey: {instanceKey}");
								_logger.LogError(e.Message);
								throw;
						}
				}
    }
}

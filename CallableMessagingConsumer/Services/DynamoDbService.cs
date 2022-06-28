using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Noogadev.CallableMessagingConsumer.Services
{
    public interface IDynamoDbService
    {
        public Task AddItem(string typeKey, string instanceKey, TimeSpan expiration);
        public Task DeleteItem(string typeKey, string instanceKey);
        public Task<QueryResponse> GetByType(string typeKey);
        public Task AddOrUpdateItem(
            string typeKey,
            string instanceKey,
            TimeSpan expiration,
            Dictionary<string, string>? updates = null);
        public Task<bool> TryDeleteItem(
            string typeKey,
            string instanceKey,
            string conditionExpression,
            Dictionary<string, string>? expressionAttributesNames = null,
            Dictionary<string, string>? expressionAttributesValues = null);
    }

    /// <summary>
    /// This class facilitates interactions with DynamoDB for ICallable messages.
    /// For initial setup instructions, see the Readme in this project.
    /// </summary>
    public class DynamoDbService : IDynamoDbService
    {
        public const string LockTableName = "callable-exclusive-lock";
        public const string PrimaryKeyName = "type-key";
        public const string SortKeyName = "instance-key";
        public const string DebounceKeyName = "debounce-key";
        public const string SetAtName = "set-at";
        public const string ExpiresAtName = "expires-at";

        private readonly IAmazonDynamoDB _dynamoClient;

        public DynamoDbService(IAmazonDynamoDB dynamoClient)
        {
            _dynamoClient = dynamoClient;
        }

        public Task AddItem(string typeKey, string instanceKey, TimeSpan expiration)
        {
            var now = DateTimeOffset.UtcNow;
            return _dynamoClient.PutItemAsync(new PutItemRequest
            {
                TableName = LockTableName,
                Item = new()
                {
                    { PrimaryKeyName, new() { S = typeKey } },
                    { SortKeyName, new() { S = instanceKey } },
                    { SetAtName, new() { S = now.ToString() } },
                    // add an expiration in case something goes wrong and we didn't release our lock
                    { ExpiresAtName, new() { N = now.Add(expiration).ToUnixTimeSeconds().ToString() } }
                }
            });
        }

        public Task AddOrUpdateItem(
            string typeKey,
            string instanceKey,
            TimeSpan expiration,
            Dictionary<string, string>? updates = null)
        {
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.Add(expiration).ToUnixTimeSeconds().ToString();
            var attributeUpdates = new Dictionary<string, AttributeValueUpdate>()
            {
                { SetAtName, new() { Action = AttributeAction.PUT, Value = new() { S = now.ToString() } } },
                { ExpiresAtName, new() { Action = AttributeAction.PUT, Value = new() { N = expiresAt } } }
            };

            foreach (var update in updates ?? new())
            {
                attributeUpdates.TryAdd(
                    update.Key,
                    new() { Action = AttributeAction.PUT, Value = new() { S = update.Value } });
            }

            return _dynamoClient.UpdateItemAsync(new UpdateItemRequest(
                DynamoDbService.LockTableName,
                new()
                {
                    { PrimaryKeyName, new() { S = typeKey } },
                    { SortKeyName, new() { S = instanceKey } }
                },
                attributeUpdates
            ));
        }

        public Task DeleteItem(string typeKey, string instanceKey)
        {
            return _dynamoClient.DeleteItemAsync(LockTableName, new()
            {
                { PrimaryKeyName, new() { S = typeKey } },
                { SortKeyName, new() { S = instanceKey } }
            });
        }

        public async Task<bool> TryDeleteItem(
            string typeKey,
            string instanceKey,
            string conditionExpression,
            Dictionary<string, string>? expressionAttributesNames = null,
            Dictionary<string, string>? expressionAttributesValues = null)
        {
            try
            {
                await _dynamoClient.DeleteItemAsync(new()
                {
                    TableName = LockTableName,
                    Key = new()
                    {
                        { PrimaryKeyName, new() { S = typeKey } },
                        { SortKeyName, new() { S = instanceKey } }
                    },
                    ConditionExpression = conditionExpression,
                    ExpressionAttributeNames = expressionAttributesNames,
                    ExpressionAttributeValues = expressionAttributesValues?
                        .ToDictionary(x => x.Key, x => new AttributeValue { S = x.Value })
                });

                return true;
            }
            catch (ConditionalCheckFailedException)
            {
                return false;
            }
        }

        public Task<QueryResponse> GetByType(string typeKey)
        {
            return _dynamoClient.QueryAsync(new QueryRequest
            {
                TableName = LockTableName,
                KeyConditionExpression = $"#v_field = :v_value",
                ExpressionAttributeValues = { { ":v_value", new AttributeValue(typeKey) } },
                ExpressionAttributeNames = { { "#v_field", PrimaryKeyName } }
            });
        }
    }
}

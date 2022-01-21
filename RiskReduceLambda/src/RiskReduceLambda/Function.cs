using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace RiskReduceLambda
{
    public class Function
    {
        public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            var logger = context.Logger;

            logger.LogLine($"Beginning to process {dynamoEvent.Records.Count} records...");

            var recordListHash = dynamoEvent.Records.GetSequenceHashCode().ToString();

            var riskTotals = new Dictionary<string, decimal>();

            foreach (var record in dynamoEvent.Records)
            {
                logger.LogLine($"Event ID: {record.EventID}");
                logger.LogLine($"Event Name: {record.EventName}");

                var newImage = record.Dynamodb.NewImage;
                if (newImage != null && newImage.Count > 0)
                {
                    var riskAggregateString = newImage["Message"].S;
                    var riskTypeAggregation =
                        JsonConvert
                            .DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, decimal>>>>(
                                riskAggregateString);

                    if (riskTypeAggregation != null)
                    {
                        foreach (var riskType in riskTypeAggregation)
                        {
                            foreach (var tradeDesk in riskType.Value)
                            {
                                foreach (var region in tradeDesk.Value)
                                {
                                    if (riskTotals.ContainsKey($"{riskType.Key}:{tradeDesk.Key}:{region.Key}"))
                                    {
                                        riskTotals[$"{riskType.Key}:{tradeDesk.Key}:{region.Key}"] += region.Value;
                                    }
                                    else
                                    {
                                        riskTotals.Add($"{riskType.Key}:{tradeDesk.Key}:{region.Key}", region.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            var batch = riskTotals.Select(r => new TransactWriteItem
            {
                Update = new Update
                {
                    TableName = "StatelessAggregateTable",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        {"Identifier", new AttributeValue {S = r.Key}}
                    },
                    UpdateExpression = "ADD #val :val",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        {":val", new AttributeValue {N = r.Value.ToString(CultureInfo.InvariantCulture)}}
                    },
                    ExpressionAttributeNames = new Dictionary<string, string>
                    {
                        {"#val", "Value"}
                    }
                }
            }).ToList();

            if (batch.Any())
            {
                var itemsRequest = new TransactWriteItemsRequest
                {
                    TransactItems = batch,
                    ClientRequestToken = recordListHash
                };
            
                var dynamo = CreateDynamoDbClient();

                try
                {
                    await dynamo.TransactWriteItemsAsync(itemsRequest);
                }
                catch (IdempotentParameterMismatchException)
                {
                    logger.LogLine($"Batch with hash {recordListHash} already exist. Skipping it.");
                }
                catch (Exception e)
                {
                    logger.LogLine(e.Message);
                    throw;
                }
            }
            else
            {
                logger.LogLine("No transaction items found. Skipping it.");
            }
            
            logger.LogLine("Stream processing complete.");

        }

        private static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = RegionEndpoint.EUWest1
            };

            return new AmazonDynamoDBClient(config);
        }
    }

    public static class ListExtensionMethods
    {
        public static int GetSequenceHashCode<T>(this IList<T> sequence)
        {
            const int seed = 487;
            const int modifier = 31;

            unchecked
            {
                return sequence.Aggregate(seed, (current, item) =>
                    (current * modifier) + item.GetHashCode());
            }
        }
    }
}
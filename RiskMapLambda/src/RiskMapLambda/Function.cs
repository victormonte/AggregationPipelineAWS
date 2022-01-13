using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.Serialization.SystemTextJson;
using RiskReduceLambda;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace RiskMapLambda
{
    public class Function
    {
        public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            string messageHash = "";
            
            try
            {
                logger.LogLine($"Beginning to process {dynamoEvent.Records.Count} records...");

                var delta =
                    dynamoEvent
                        .Records
                        .Aggregate(new List<RiskEntry>(), (entries, next) =>
                        {
                            logger.LogLine($"Event ID: {next.EventID}");
                            logger.LogLine($"Event Name: {next.EventName}");

                            var newImage = next.Dynamodb.NewImage;
                            if (newImage != null && newImage.Count > 0)
                            {
                                var hierarchy = JsonSerializer.Deserialize<Hierarchy>(newImage["Hierarchy"].S);
                                var amount = decimal.Parse(newImage["Amount"].N);
                                var createdAt = DateTime.ParseExact(newImage["CreatedAt"].S, "dd-MM-yyyy HH:mm:ss",
                                    CultureInfo.InvariantCulture);

                                entries.Add(new RiskEntry
                                {
                                    CreatedAt = createdAt,
                                    Hierarchy = hierarchy,
                                    Amount = amount,
                                });
                            }

                            var oldImage = next.Dynamodb.OldImage;
                            if (oldImage != null && oldImage.Count > 0)
                            {
                                var oldHierarchy = JsonSerializer.Deserialize<Hierarchy>(oldImage["Hierarchy"].S);
                                var oldAmount = decimal.Parse(oldImage["Amount"].N);

                                entries.Add(new RiskEntry
                                {
                                    Hierarchy = oldHierarchy,
                                    Amount = decimal.Negate(Math.Abs(oldAmount))
                                });
                            }

                            return entries;
                        })
                        .Aggregate(new Dictionary<string, Dictionary<string, Dictionary<string, decimal>>>(),
                            (riskTypeAggregatorDictionary, entry) =>
                            {
                                if (HasRiskType(riskTypeAggregatorDictionary, entry))
                                {
                                    var riskTypeAggregator = riskTypeAggregatorDictionary[entry.Hierarchy.RiskType];
                                    if (HasTradeDesk(riskTypeAggregator, entry))
                                    {
                                        var regionAggregator = riskTypeAggregator[entry.Hierarchy.TradeDesk];
                                        if (HasRegion(regionAggregator, entry))
                                        {
                                            regionAggregator[entry.Hierarchy.Region] += entry.Amount;
                                        }
                                        else
                                        {
                                            regionAggregator.Add(entry.Hierarchy.Region, entry.Amount);
                                        }
                                    }
                                    else
                                    {
                                        var regionTypeAggregator = new Dictionary<string, decimal>
                                        {
                                            {
                                                entry.Hierarchy.Region, entry.Amount
                                            }
                                        };

                                        riskTypeAggregator.Add(entry.Hierarchy.TradeDesk, regionTypeAggregator);
                                    }
                                }
                                else
                                {
                                    var regionTypeAggregator = new Dictionary<string, decimal>
                                    {
                                        {
                                            entry.Hierarchy.Region, entry.Amount
                                        }
                                    };

                                    var tradeDeskAggregator = new Dictionary<string, Dictionary<string, decimal>>
                                    {
                                        {
                                            entry.Hierarchy.TradeDesk, regionTypeAggregator
                                        }
                                    };

                                    riskTypeAggregatorDictionary.Add(entry.Hierarchy.RiskType, tradeDeskAggregator);
                                }

                                return riskTypeAggregatorDictionary;
                            });

                messageHash = dynamoEvent.Records.GetSequenceHashCode().ToString();

                var putItemRequest = new PutItemRequest
                {
                    TableName = "StatelessReduceTable",
                    Item = new Dictionary<string, AttributeValue>
                    {
                        { "MessageHash", new AttributeValue {S = messageHash}},
                        { "Message", new AttributeValue {S = JsonSerializer.Serialize(delta)}}
                    },
                    ConditionExpression = "attribute_not_exists(MessageHash)"
                };

                var dynamo = CreateDynamoDbClient();

                await dynamo.PutItemAsync(putItemRequest);
            }
            catch (ConditionalCheckFailedException e)
            {
                logger.LogLine($"Message hash {messageHash} already exist. Skipping it.");
            }
            catch (Exception e)
            {
                logger.LogLine(e.Message);
                throw;
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

        private static bool HasRiskType(Dictionary<string, Dictionary<string,
            Dictionary<string, decimal>>> riskTypeAggregatorDictionary, RiskEntry entry) =>
            riskTypeAggregatorDictionary.ContainsKey(entry.Hierarchy.RiskType);

        private static bool HasRegion(Dictionary<string, decimal> riskRegionAggregatorDictionary, RiskEntry entry)
            => riskRegionAggregatorDictionary.ContainsKey(entry.Hierarchy.Region);

        private static bool HasTradeDesk(Dictionary<string, Dictionary<string, decimal>> riskTradeAggregatorDictionary,
            RiskEntry entry)
            => riskTradeAggregatorDictionary.ContainsKey(entry.Hierarchy.TradeDesk);
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
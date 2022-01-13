using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace RiskStateLambda
{
    public class Function
    {

        public async Task FunctionHandler(KinesisEvent kinesisEvent, ILambdaContext context)
        {
            var logger = context.Logger;
            
            logger.LogLine($"Beginning to process {kinesisEvent.Records.Count} records...");
            
            var dynamo = CreateDynamoDbClient();

            foreach (var record in kinesisEvent.Records)
            {
                logger.LogLine($"Event ID: {record.EventId}");
                logger.LogLine($"Event Name: {record.EventName}");

                using var reader = new StreamReader(record.Kinesis.Data, Encoding.UTF8);
                
                var riskMessage = JsonSerializer.Deserialize<RiskMessage>(await reader.ReadToEndAsync());
                
                logger.LogLine($"TradeId: {riskMessage.TradeId}, Amount: {riskMessage.Amount}, Version: {riskMessage.Version}");

                var updateRequest = new UpdateItemRequest
                {
                    TableName = "StatefulStateTable",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "Id", new AttributeValue { S = riskMessage.TradeId.ToString() } }
                    },
                    UpdateExpression = "SET Amount = :new_value," +
                                       "Version   = :new_version," +
                                       "Hierarchy = :new_hierarchy," +
                                       "CreatedAt = :created_at",
                    ConditionExpression = "attribute_not_exists(Id) OR Version < :new_version",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":new_version", new AttributeValue { N = riskMessage.Version.ToString(CultureInfo.InvariantCulture) } },
                        { ":new_value", new AttributeValue { N = riskMessage.Amount.ToString(CultureInfo.InvariantCulture) } },
                        { ":new_hierarchy", new AttributeValue { S = JsonSerializer.Serialize(riskMessage.Hierarchy) } },
                        { ":created_at", new AttributeValue { S = riskMessage.CreatedAt.ToString("dd-MM-yyyy HH:mm:ss")}}
                    }
                };

                try
                {
                    await dynamo.UpdateItemAsync(updateRequest);

                    logger.LogLine("Update item completed.");
                }
                catch (ConditionalCheckFailedException e)
                {
                    logger.LogLine($"Trade Id {riskMessage.TradeId} already exist. Skipping it.");
                }
                catch (Exception e)
                {
                    logger.LogLine(e.Message);
                    throw;
                }
            }

            logger.LogLine("Stream processing complete.");
        }

        private static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            var config = new AmazonDynamoDBConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.EUWest1
            };

            return new AmazonDynamoDBClient(config);
        }
    }
}
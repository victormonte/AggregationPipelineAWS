using System.Collections.Generic;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.Serialization.SystemTextJson;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace RiskReduceLambda
{
    public class Function
    {
        public void FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
        {
            context.Logger.LogLine($"Beginning to process {dynamoEvent.Records.Count} records...");

            foreach (var record in dynamoEvent.Records)
            {
                context.Logger.LogLine($"Event ID: {record.EventID}");
                context.Logger.LogLine($"Event Name: {record.EventName}");
				
				// TODO: Add business logic processing the record.Dynamodb object.
                
                var delta = new Dictionary<string, Dictionary<string, Dictionary<string, decimal>>>();
                // .Aggregate(delta, Calculate);
            }

            context.Logger.LogLine("Stream processing complete.");
        }
        
        private static Dictionary<string, Dictionary<string, Dictionary<string, decimal>>> Calculate(
            Dictionary<string, Dictionary<string, Dictionary<string, decimal>>> riskTypeAggregatorDictionary, 
            RiskMessage message)
        {
            if (HasRiskType(riskTypeAggregatorDictionary, message))
            {
                var riskTypeAggregator = riskTypeAggregatorDictionary[message.Hierarchy.RiskType];
                if (HasTradeDesk(riskTypeAggregator, message))
                {
                    var regionAggregator = riskTypeAggregator[message.Hierarchy.TradeDesk];
                    if (HasRegion(regionAggregator, message))
                    {
                        regionAggregator[message.Hierarchy.Region] += message.Value;
                    }
                    else
                    {
                        regionAggregator.Add(message.Hierarchy.Region, message.Value);
                    }
                }
                else
                {
                    var regionTypeAggregator = new Dictionary<string, decimal> {
                    {
                        message.Hierarchy.Region, message.Value
                    }};
                    
                    riskTypeAggregator.Add(message.Hierarchy.RiskType, regionTypeAggregator);
                }
            }
            else
            {
                var regionTypeAggregator = new Dictionary<string, decimal> {
                {
                    message.Hierarchy.Region, message.Value
                }};
                
                var tradeDeskAggregator = new Dictionary<string, Dictionary<string, decimal>>
                {
                    {
                        message.Hierarchy.TradeDesk, regionTypeAggregator
                    }
                };
        
                riskTypeAggregatorDictionary.Add(message.Hierarchy.RiskType, tradeDeskAggregator);
            }
        
            return riskTypeAggregatorDictionary;
        }
        
        private static bool HasRiskType(Dictionary<string, Dictionary<string, 
            Dictionary<string, decimal>>> riskTypeAggregatorDictionary, RiskMessage message) =>
            riskTypeAggregatorDictionary.ContainsKey(message.Hierarchy.RiskType);
        
        private static bool HasRegion(Dictionary<string, decimal> riskRegionAggregatorDictionary, RiskMessage message)
            => riskRegionAggregatorDictionary.ContainsKey(message.Hierarchy.Region);
        
        private static bool HasTradeDesk(Dictionary<string, Dictionary<string, decimal>> riskTradeAggregatorDictionary, RiskMessage message) 
            => riskTradeAggregatorDictionary.ContainsKey(message.Hierarchy.TradeDesk);
    }
}
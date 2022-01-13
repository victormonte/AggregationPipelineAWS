using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.Lambda.TestUtilities;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using RiskMapLambda;

namespace RiskMapLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestFunction()
        {
            DynamoDBEvent evnt = new DynamoDBEvent
            {
                Records = new List<DynamoDBEvent.DynamodbStreamRecord>
                {
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "1" } } },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXOption\"}" } }, 
                                { "Amount", new AttributeValue { N = "100" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") } }
                            },
                            OldImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXOption\"}" } }, 
                                { "Amount", new AttributeValue { N = "50" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("s") } }
                            },
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    },
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "2" } } },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXOption\"}" } }, 
                                { "Amount", new AttributeValue { N = "200" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") } }
                            },
                            OldImage = null,
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    },
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "3" } } },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXOption\"}" } }, 
                                { "Amount", new AttributeValue { N = "300" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") } }
                            },
                            OldImage = null,
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    },
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "1" } } },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXSpot\"}" } }, 
                                { "Amount", new AttributeValue { N = "100" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") } }
                            },
                            OldImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXSpot\"}" } }, 
                                { "Amount", new AttributeValue { N = "50" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("s") } }
                            },
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    },
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "2" } } },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXSpot\"}" } }, 
                                { "Amount", new AttributeValue { N = "200" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") } }
                            },
                            OldImage = null,
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    },
                    new DynamoDBEvent.DynamodbStreamRecord
                    {
                        AwsRegion = "us-west-2",
                        Dynamodb = new StreamRecord
                        {
                            ApproximateCreationDateTime = DateTime.Now,
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "3" } } },
                            NewImage = new Dictionary<string, AttributeValue>
                            {
                                { "Hierarchy", new AttributeValue { S = "{\"RiskType\":\"Delta\",\"Region\":\"EMEA\",\"TradeDesk\":\"FXSpot\"}" } }, 
                                { "Amount", new AttributeValue { N = "300" } }, 
                                { "CreatedAt", new AttributeValue { S = DateTime.Today.ToString("dd-MM-yyyy HH:mm:ss") } }
                            },
                            OldImage = null,
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    },
                }
            };

            var context = new TestLambdaContext();
            var function = new Function();

            function.FunctionHandler(evnt, context);

            var testLogger = context.Logger as TestLambdaLogger;
			Assert.Contains("Stream processing complete", testLogger.Buffer.ToString());
        }  
    }
}

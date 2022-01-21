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

using RiskReduceLambda;

namespace RiskReduceLambda.Tests
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
                            Keys = new Dictionary<string, AttributeValue> { {"id", new AttributeValue { S = "MyId" } } },
                            NewImage = new Dictionary<string, AttributeValue> { { "Message", new AttributeValue { S =  "{\"Delta\":{\"FXSpot\":{\"AMER\":966,\"APAC\":-416},\"FXOption\":{\"APAC\":4395,\"EMEA\":2667,\"AMER\":-4234}},\"PV\":{\"FXOption\":{\"AMER\":4303,\"APAC\":5888,\"EMEA\":8072},\"FXSpot\":{\"AMER\":3228,\"EMEA\":5551,\"APAC\":7442}}}"} } },
                            OldImage = null,
                            StreamViewType = StreamViewType.NEW_AND_OLD_IMAGES
                        }
                    }
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

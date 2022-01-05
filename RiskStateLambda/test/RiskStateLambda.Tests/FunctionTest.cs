using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Amazon;
using Amazon.Lambda.Core;
using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.TestUtilities;
using Newtonsoft.Json;
using RiskStateLambda;

namespace RiskMapLambda.Tests
{
    public class FunctionTest
    {
        private static readonly Random Random = new Random();
        
        [Fact]
        public void TestFunction()
        {
            var hierarchy1 = new Hierarchy
            {
                RiskType = "Delta",
                TradeDesk = "FXSpot",
                Region = "EMEA"
            };
                
            var hierarchy2 = new Hierarchy
            {
                RiskType = "PV", 
                TradeDesk = "FXOption", 
                Region = "EMEA"
            };
            
            var riskMessage1 = new RiskMessage
            {
                CreatedAt = DateTime.Now,
                Hierarchy = hierarchy1,
                TradeId = Guid.NewGuid(),
                Value = 10,
                Version = 1
            };
            
            var riskMessage2 = new RiskMessage
            {
                CreatedAt = DateTime.Now,
                Hierarchy = hierarchy1,
                TradeId = Guid.NewGuid(),
                Value = 10,
                Version = 1
            };
            
            var riskMessage3 = new RiskMessage
            {
                CreatedAt = DateTime.Now,
                Hierarchy = hierarchy2,
                TradeId = Guid.NewGuid(),
                Value = 30,
                Version = 1
            };
            
            var @event = new KinesisEvent
            {
                Records = new List<KinesisEvent.KinesisEventRecord>
                {
                    new KinesisEvent.KinesisEventRecord
                    {
                        AwsRegion = "us-west-2",
                        Kinesis = new KinesisEvent.Record
                        {
                            ApproximateArrivalTimestamp = DateTime.Now,
                            Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(riskMessage1)))
                        }
                    },
                    new KinesisEvent.KinesisEventRecord
                    {
                        AwsRegion = "us-west-2",
                        Kinesis = new KinesisEvent.Record
                        {
                            ApproximateArrivalTimestamp = DateTime.Now,
                            Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(riskMessage2)))
                        }
                    },
                    new KinesisEvent.KinesisEventRecord
                    {
                        AwsRegion = "us-west-2",
                        Kinesis = new KinesisEvent.Record
                        {
                            ApproximateArrivalTimestamp = DateTime.Now,
                            Data = new MemoryStream(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(riskMessage3)))
                        }
                    }
                }
            };


            var context = new TestLambdaContext();
            var function = new Function();

            function.FunctionHandler(@event, context);
        }
    }
}

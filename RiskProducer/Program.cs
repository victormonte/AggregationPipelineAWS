using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using Newtonsoft.Json;

namespace RiskProducer
{
    internal static class Program
    {
        private static readonly Random Random = new();
        private static readonly List<Hierarchy> HierarchyList = new()
        {
            new Hierarchy("Delta", "FXSpot", "EMEA"),
            new Hierarchy("Delta", "FXSpot", "APAC"),
            new Hierarchy("Delta", "FXSpot", "AMER"),
            new Hierarchy("Delta", "FXOption", "EMEA"),
            new Hierarchy("Delta", "FXOption", "APAC"),
            new Hierarchy("Delta", "FXOption", "AMER"),
            new Hierarchy("PV", "FXSpot", "EMEA"),
            new Hierarchy("PV", "FXSpot", "APAC"),
            new Hierarchy("PV", "FXSpot", "AMER"),
            new Hierarchy("PV", "FXOption", "EMEA"),
            new Hierarchy("PV", "FXOption", "APAC"),
            new Hierarchy("PV", "FXOption", "AMER")
        };

        static async Task Main(string[] args)
        {
            Console.WriteLine("Risk Message Producer started");

            var message = NewRiskMessage();

            Console.WriteLine($"Risk message {message.TradeId} created. Value: {message.Value}, " +
                              $"Version {message.Version}, CreatedAt: {message.CreatedAt:s}");

            await Publish(message);

            Console.WriteLine("Risk Message Producer finished");
        }

        private static RiskMessage NewRiskMessage() =>
            new(Guid.NewGuid(), Random.Next(1, 1000), 1, DateTime.Now, GetRandomHierarchy());

        private static async Task Publish(RiskMessage riskMessage)
        {
            var oByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(riskMessage));
            await using var stream = new MemoryStream(oByte);
            var requestRecord = new PutRecordRequest
            {
                StreamName = "StatelessRiskDataStream",
                Data = stream,
                PartitionKey = riskMessage.TradeId.ToString()
            };

            var config = new AmazonKinesisConfig
            {
                RegionEndpoint = Amazon.RegionEndpoint.EUWest1
            };

            var kinesisClient = new AmazonKinesisClient(config);

            try
            {
                await kinesisClient.PutRecordAsync(requestRecord);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static Hierarchy GetRandomHierarchy() => HierarchyList[new Random().Next(1, HierarchyList.Count)];
    }
}
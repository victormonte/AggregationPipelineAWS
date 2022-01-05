namespace RiskProducer
{
    public class Hierarchy
    {
        public Hierarchy(string riskType, string tradeDesk, string region)
        {
            RiskType = riskType;
            TradeDesk = tradeDesk;
            Region = region;
        }

        public string RiskType { get; }
        public string Region { get; }
        public string TradeDesk { get; }
    }
}
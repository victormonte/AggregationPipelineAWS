using System;
using System.Collections.Generic;

namespace RiskProducer
{
    public class RiskMessage
    {
        public RiskMessage(Guid tradeId, decimal amount, int version, DateTime createdAt, Hierarchy hierarchy)
        {
            TradeId = tradeId;
            Amount = amount;
            Version = version;
            CreatedAt = createdAt;
            Hierarchy = hierarchy;
        }

        public Guid TradeId { get; }
        public decimal Amount { get; }
        public int Version { get; }
        public DateTime CreatedAt { get; }
        public Hierarchy Hierarchy { get; }
    }
}
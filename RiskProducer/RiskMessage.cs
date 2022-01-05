using System;
using System.Collections.Generic;

namespace RiskProducer
{
    public class RiskMessage
    {
        public RiskMessage(Guid tradeId, decimal value, int version, DateTime createdAt, Hierarchy hierarchy)
        {
            TradeId = tradeId;
            Value = value;
            Version = version;
            CreatedAt = createdAt;
            Hierarchy = hierarchy;
        }

        public Guid TradeId { get; }
        public decimal Value { get; }
        public int Version { get; }
        public DateTime CreatedAt { get; }
        public Hierarchy Hierarchy { get; }
    }
}
using System;
using System.Collections.Generic;

namespace RiskStateLambda
{
    public class RiskMessage
    {
        public Guid TradeId { get; set; }
        public decimal Value { get; set;}
        public int Version { get; set;}
        public DateTime CreatedAt { get; set;}
        public Hierarchy Hierarchy { get; set;}
    }
    
    public class Hierarchy
    {
        public string RiskType { get; set;}
        public string Region { get; set;}
        public string TradeDesk { get; set;}
    }
}
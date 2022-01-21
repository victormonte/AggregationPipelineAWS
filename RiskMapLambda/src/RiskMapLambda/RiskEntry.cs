using System;

namespace RiskReduceLambda
{
    public class RiskEntry
    {
        public decimal Amount { get; set;}
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
export class RiskMessage implements IRiskMessage {
    public TradeId: string;
    Amount: number;
    Version: number;
    CreatedAt: Date;
    Hierarchy: IHierarchy;

    constructor(
        tradeId: string, 
        amount: number, 
        version: number, 
        createdAt: Date, 
        hierarchy: IHierarchy) {
        this.TradeId = tradeId;
        this.Amount = amount;
        this.Version = version;
        this.CreatedAt = createdAt;
        this.Hierarchy = hierarchy;
    }
}

export class Hierarchy implements IHierarchy {
    RiskType: string;
    TradeDesk: string;
    Region: string;

    constructor(riskType: string, tradeDesk: string, region: string) {
        this.RiskType = riskType;
        this.TradeDesk = tradeDesk;
        this.Region = region;
    }
    
}

export interface IRiskMessage {
    TradeId: string;
    Amount: number;
    Version: number;
    CreatedAt: Date;
    Hierarchy: IHierarchy;
}

export interface IHierarchy{
    RiskType: string;
    TradeDesk: string;
    Region: string;
}
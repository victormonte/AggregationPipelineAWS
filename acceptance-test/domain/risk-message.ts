export class RiskMessage {
    public TradeId: string;
    Amount: number;
    Version: number;
    CreatedAt: Date;
    Hierarchy: Hierarchy;

    constructor(
        tradeId: string, 
        amount: number, 
        version: number, 
        createdAt: Date, 
        hierarchy: Hierarchy) {
        this.TradeId = tradeId;
        this.Amount = amount;
        this.Version = version;
        this.CreatedAt = createdAt;
        this.Hierarchy = hierarchy;
    }
}

export class Hierarchy {
    RiskType: string;
    TradeDesk: string;
    Region: string;

    constructor(riskType: string, tradeDesk: string, region: string) {
        this.RiskType = riskType;
        this.TradeDesk = tradeDesk;
        this.Region = region;
    }
}

export interface IRiskMessageInputRow {
    Amount: string;
    Hierarchy: string; 
}

export class RiskMessageInputRow {
    public Amount: number;
    public Hierarchy: Hierarchy;

    constructor({Amount, Hierarchy: HierarchyId}: IRiskMessageInputRow) {
        this.Amount = Number(Amount);

        var hierarchyList = HierarchyId.split(":", 3);
        this.Hierarchy = new Hierarchy(hierarchyList[0], hierarchyList[1], hierarchyList[2]);
    }
}

// export interface IRiskMessage {
//     TradeId: string;
//     Amount: number;
//     Version: number;
//     CreatedAt: Date;
//     Hierarchy: IHierarchy;
// }

// export interface IHierarchy{
//     RiskType: string;
//     TradeDesk: string;
//     Region: string;
// }
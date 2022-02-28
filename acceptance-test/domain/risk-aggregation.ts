export interface IExpectedRiskAggregationRow {
    Identifier: string; 
    Amount: string;
}

export class ExpectedRiskAggregationRow {
    public Amount: string;
    public Identifier: string;

    constructor({Identifier, Amount}: IExpectedRiskAggregationRow) {
        this.Identifier = Identifier;
        this.Amount = Amount;
    }
}
import { binding, given, when, then, before } from "cucumber-tsflow";
import { expect } from 'chai';
import { v4 as uuidv4 } from 'uuid';
import { Kinesis, DynamoDB } from 'aws-sdk';
import { RiskMessage, Hierarchy} from '../domain/risk-message';
import { sleep } from './common';
import { config } from 'node-config-ts'

@binding()
export class RiskMessageDeduplication {
    private riskMessage: RiskMessage | null = null;
    private kinesis: Kinesis | null = null;
    private dynamodb: DynamoDB | null = null;

    public constructor() {

        // kinesis
        this.kinesis = new Kinesis({
            apiVersion: '2013-12-02',
            region: config.aws.region
        });

        // dynamodb
        this.dynamodb = new DynamoDB({
            apiVersion: '2012-08-10',
            region: config.aws.region
        })
    }

    @given(/^a risk message was published$/)
    public givenRiskMessageWasPublished(): void {
        if (this.kinesis === null) throw new Error("Kinesis cannot be null");

        var tradeId = uuidv4();

        this.riskMessage = new RiskMessage(tradeId, 3, 1, new Date(), new Hierarchy("Delta", "FXSpot", "EMEA"));

        try {

            this.kinesis.putRecord({
                StreamName: "StatelessRiskDataStream",
                PartitionKey: tradeId,
                Data: JSON.stringify(this.riskMessage)
            }).promise();

        } catch (error) {
            console.log(error);
        }
    }

    @when(/^the same risk message is published$/)
    public whenSameRiskMessageIsPublished(): void {
        if (this.riskMessage == null) throw new Error("Risk message cannot be null");
        if (this.kinesis == null) throw new Error("Kinesis cannot be null");

        this.kinesis.putRecord({
            StreamName: "StatelessRiskDataStream",
            PartitionKey: this.riskMessage.TradeId,
            Data: JSON.stringify(this.riskMessage)
        }).promise();
    }

    @then(/^risk is not duplicated$/, undefined, 60000)
    public async thenRiskIsNotDuplicated(): Promise<void> {
        if (this.riskMessage == null) throw new Error("Risk message cannot be null");
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");
        
        var params = {
            ExpressionAttributeValues: {
             ":id": {
               S: this.riskMessage.TradeId
              }
            }, 
            KeyConditionExpression: "Id = :id", 
            TableName: "StatefulStateTable"
           };

        // wait pipeline to catch up
        await sleep(10000);

        var result = await this.dynamodb.query(params).promise();

        expect(result.Count).to.equal(1, `Number of risk message doesn't match`);
    }
}


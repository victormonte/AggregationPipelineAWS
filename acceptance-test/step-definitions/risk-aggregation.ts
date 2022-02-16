import { binding, given, when, before, then } from "cucumber-tsflow";
import { expect } from 'chai';
import { v4 as uuidv4 } from 'uuid';
import { Kinesis, DynamoDB } from 'aws-sdk';
import { IRiskMessage, RiskMessage, Hierarchy } from '../domain/risk-message';

@binding()
export class RiskAggregation {
    private riskMessage: IRiskMessage | null = null;
    private kinesis: Kinesis | null = null;
    private dynamodb: DynamoDB | null = null;

    @before()
    public beforeAllScenarios(): void {

        //TODO: extract region to config
        const region = "eu-west-1";

        // kinesis
        this.kinesis = new Kinesis({
            apiVersion: '2013-12-02',
            region: region
        });

        // dynamodb
        this.dynamodb = new DynamoDB({
            apiVersion: '2012-08-10',
            region: region
        })
    }

    @given(/^a risk message was published$/)
    public givenRiskMessageWasPublished(): void {
        if (this.kinesis == null) throw new Error("Kinesis cannot be null");

        var tradeId = uuidv4();

        this.riskMessage = new RiskMessage(tradeId, 3, 1, new Date(), new Hierarchy("Delta", "FXSpot", "EMEA"));

        try {

            this.kinesis.putRecord({
                StreamName: "StatelessRiskDataStream",
                PartitionKey: tradeId,
                Data: JSON.stringify(this.riskMessage)
            }, function (err, data) {
                if (err) console.log(err, err.stack); // an error occurred
            });

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
        }, function (err, data) {
            if (err) console.log(err, err.stack); // an error occurred
        });
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

        // wait pipeline to catch
        await this.sleep(15000);

        var result = await this.dynamodb.query(params).promise();

        expect(result.Count).to.equal(1, `Number of risk message doesn't match`);

    }

    private sleep(ms: number) {
        return new Promise((resolve) => {
          setTimeout(resolve, ms);
        });
      }
}


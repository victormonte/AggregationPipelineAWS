import { binding, given, when, then, before } from "cucumber-tsflow";
import { expect } from 'chai';
import { v4 as uuidv4 } from 'uuid';
import { Kinesis, DynamoDB } from 'aws-sdk';
import { RiskMessage, IRiskMessageInputRow, RiskMessageInputRow } from '../domain/risk-message';
import { IExpectedRiskAggregationRow, ExpectedRiskAggregationRow } from '../domain/risk-aggregation';
import { TableDefinition  } from '../utils/helpers';
import { sleep } from './common';
import { config } from 'node-config-ts'

@binding()
export class RiskAggregation {
    private riskMessages: RiskMessage[] = [];
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
        });
    }

    @given(/^the following risk messages$/)
    public givenTheFollowingRiskMessages(table: TableDefinition<IRiskMessageInputRow>): void {
        if (this.kinesis == null) throw new Error("Kinesis cannot be null");

        const rows = table.hashes();

        for (const row of rows) {

            var inputs = new RiskMessageInputRow(row);
            var tradeId = uuidv4();
            var riskMessage = new RiskMessage(tradeId, inputs.Amount, 1, new Date(), inputs.Hierarchy);

            this.riskMessages.push(riskMessage);
        }
    }

    @when(/^risk messages are published$/)
    public whenRiskMessagesArePublished(): void {
        if (this.riskMessages == null) throw new Error("Risk messages cannot be null");
        if (this.kinesis == null) throw new Error("Kinesis cannot be null");

        for (const riskMessage of this.riskMessages)
        {
            try {

                this.kinesis.putRecord({
                    StreamName: "StatelessRiskDataStream",
                    PartitionKey: riskMessage.TradeId,
                    Data: JSON.stringify(riskMessage)
                }).promise();

            } catch (error) {
                console.log(error);
            }
        }
    }

    @then(/^the following risk aggregations are expected$/, undefined, 100000)
    public async thenTheFollowingRiskAggreationAreExpected(table: TableDefinition<IExpectedRiskAggregationRow>): Promise<void> {
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

        const rows = table.hashes();

        // wait pipeline to catch up
        await sleep(20000);

        for (const row of rows)
        {
            // wait pipeline to catch up
            await sleep(10000);

            var expectations = new ExpectedRiskAggregationRow(row);

            var params = {
                ExpressionAttributeValues: {
                 ":identifier": {
                   S: expectations.Identifier
                  }
                }, 
                KeyConditionExpression: "Identifier = :identifier", 
                TableName: "StatelessAggregateTable"
               };
    
            var result = await this.dynamodb.query(params).promise();

            if (result.Items == undefined || result.Items == null)
            {   
                throw new Error("Invalid result");
            }
    
            expect(result.Items[0].Value.N).to.equal(expectations.Amount, `Number of risk message doesn't match`);
        }
    }
}


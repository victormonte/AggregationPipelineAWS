import { binding, given, when, before, then, after } from "cucumber-tsflow";
import { expect } from 'chai';
import { v4 as uuidv4 } from 'uuid';
import { Kinesis, DynamoDB } from 'aws-sdk';
import { RiskMessage, Hierarchy, IRiskMessageInputRow, RiskMessageInputRow } from '../domain/risk-message';
import { IExpectedRiskAggregationRow, ExpectedRiskAggregationRow } from '../domain/risk-aggregation';
import { setDefaultTimeout } from '@cucumber/cucumber';
import { TableDefinition  } from '../utils/helpers';

// set hooks timeout
setDefaultTimeout(60 * 1000);

@binding()
export class RiskAggregation {
    private riskMessage: RiskMessage | null = null;
    private riskMessages: RiskMessage[] = [];
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
                }, function (err, data) {
                    if (err) console.log(err, err.stack); // an error occurred
                });

            } catch (error) {
                console.log(error);
            }
        }
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
        await this.sleep(10000);

        var result = await this.dynamodb.query(params).promise();

        expect(result.Count).to.equal(1, `Number of risk message doesn't match`);
    }

    @then(/^the following risk aggregations are expected$/, undefined, 60000)
    public async thenTheFollowingRiskAggreationAreExpected(table: TableDefinition<IExpectedRiskAggregationRow>): Promise<void> {
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

        const rows = table.hashes();

        for (const row of rows)
        {
            // wait pipeline to catch up
            await this.sleep(5000);

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

    @before("@cleanDatabase")
    public async afterAllScenarios(): Promise<void> {
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

        // wait pipeline to catch up
        await this.sleep(10000);

        // clear single risk message
        if (this.riskMessage != null)
        {
            var params = {
                Key: {
                 "Id": {
                   S: this.riskMessage.TradeId
                  }
                }, 
                TableName: "StatefulStateTable"
               };

            await this.dynamodb.deleteItem(params, function(err, data) {
                if (err) console.log(err, err.stack);
            });
        }
        
        this.riskMessage = null;

        // clear all risk messages
        for (const riskMessage of this.riskMessages) 
        {
            var params = {
                Key: {
                 "Id": {
                   S: riskMessage.TradeId
                  }
                }, 
                TableName: "StatefulStateTable"
               };

            await this.dynamodb.deleteItem(params, function(err, data) {
                if (err) console.log(err, err.stack);
            });
        }

        this.riskMessages = [];

        // clear all reduced items
        var i = 1;
        do {

            console.log(`Counter: ${i}`);

            var scanParams = {
                TableName: "StatelessReduceTable"
               };

            var result = await this.dynamodb.scan(scanParams).promise();

            if (result == null || result == undefined || result.Items == null || result.Items.length == 0) break;

            for (const item of result.Items)
            {
                var delParams = {
                    Key: {
                     "MessageHash": {
                       S: item.MessageHash.S || ""
                      }
                    },
                    TableName: "StatelessReduceTable"
                   };

                await this.dynamodb.deleteItem(delParams).promise();
            }

            i++;

        } while (i === 10);

        // clear all aggregations
        var riskTypes =  ["Delta", "PV"];
        var tradeDesks = ["FXSpot", "FXOption"];
        var regions = ["EMEA", "APAC", "AMER"];

        riskTypes.forEach((riskType) => {
            tradeDesks.forEach((tradeDesk) => {
                regions.forEach(async (region) => {

                    if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

                    var identifier = `${riskType}:${tradeDesk}:${region}`;

                    var params = {
                        Key: {
                            "Identifier": { S:  identifier }
                        }, 
                        TableName: "StatelessAggregateTable"
                    };

                    await this.dynamodb.deleteItem(params, function(err, data) {
                        if (err) console.log(err, err.stack)
                    });

                });
            });
        });
    }

    private sleep(ms: number) {
        return new Promise((resolve) => {
          setTimeout(resolve, ms);
        });
    }
}


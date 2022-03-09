import { binding, after } from "cucumber-tsflow";
import { DynamoDB } from 'aws-sdk';
import { setDefaultTimeout } from '@cucumber/cucumber';
import { config } from 'node-config-ts'

// set hooks timeout
setDefaultTimeout(60 * 1000);

@binding()
export class Common {
    private dynamodb: DynamoDB | null = null;
    
    public constructor() {

        // dynamodb
        this.dynamodb = new DynamoDB({
            apiVersion: '2012-08-10',
            region: config.aws.region
        })
    }

    @after("@cleanDatabase")
    public async afterAllScenarios(): Promise<void> {
        this.clearRiskStateMessages();
        this.clearReduceRisks();
        this.clearRiskAggregations();
    }

    private async clearRiskStateMessages(): Promise<void> {
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

        // wait pipeline to catch up
        await sleep(20000);

        var i = 1;
        do {

            var scanInput = { TableName: "StatefulStateTable" };

            var result = await this.dynamodb.scan(scanInput).promise();

            if (result == null || result == undefined || result.Items == null || result.Items.length == 0) break;

            for (const item of result.Items)
            {
                var deleteItemInput = {
                    Key: {
                     "Id": {
                       S: item.Id.S || ""
                      }
                    },
                    TableName: "StatefulStateTable"
                   };

                await this.dynamodb.deleteItem(deleteItemInput).promise();
            }

            i++;

        } while (i === 10);
    }

    private async clearReduceRisks(): Promise<void> {
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

        // clear all reduced items
        var i = 1;
        do {

            var scanInput = { TableName: "StatelessReduceTable" };

            var result = await this.dynamodb.scan(scanInput).promise();

            if (result == null || result == undefined || result.Items == null || result.Items.length == 0) break;

            for (const item of result.Items)
            {
                var deleteItemInput = {
                    Key: {
                    "MessageHash": {
                    S: item.MessageHash.S || ""
                    }
                    },
                    TableName: "StatelessReduceTable"
                };

                await this.dynamodb.deleteItem(deleteItemInput).promise();
            }

            i++;

        } while (i === 10);
    }

    private async clearRiskAggregations(): Promise<void> {
        if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

        // clear all aggregations
        var riskTypes =  ["Delta", "PV"];
        var tradeDesks = ["FXSpot", "FXOption"];
        var regions = ["EMEA", "APAC", "AMER"];

        riskTypes.forEach((riskType) => {
            tradeDesks.forEach((tradeDesk) => {
                regions.forEach(async (region) => {

                    if (this.dynamodb == null) throw new Error("Dynamodb cannot be null");

                    var identifier = `${riskType}:${tradeDesk}:${region}`;

                    var deleteItemInput = {
                        Key: {
                            "Identifier": { S:  identifier }
                        }, 
                        TableName: "StatelessAggregateTable"
                    };

                    await this.dynamodb.deleteItem(deleteItemInput).promise();
                });
            });
        });
    }
}

export function sleep(ms: number) {
    return new Promise((resolve) => {
      setTimeout(resolve, ms);
    });
}

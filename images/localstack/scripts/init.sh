#!/bin/sh
#Make sure this file is saved with LF line endings (not CRLF)
#Open this file in VSCode and look in the bottom right corner

echo "Creating Kinesis Stream"
aws kinesis create-stream \
    --stream-name StatelessRiskDataStream \
    --shard-count 1
echo "Creating Kinesis Stream - Completed"

echo "Creating Risk Map Lambda"
awslocal lambda create-function --function-name risk-map-lambda --runtime dotnetcore3.1 \
--zip-file fileb:///images/lambdas/RiskMapLambda.zip --handler RiskMapLambda::RiskMapLambda.Function::FunctionHandler --role local-role
echo "Creating Risk Map Lambda - Completed"

echo "Creating Event Mapping Lambda"
awslocal lambda create-event-source-mapping --function-name risk-map-lambda \
--batch-size 1 --starting-position LATEST --event-source-arn arn:aws:kinesis:us-east-1:000000000000:stream/StatelessRiskDataStream
echo "Creating Event Mapping - Completed"
	

 

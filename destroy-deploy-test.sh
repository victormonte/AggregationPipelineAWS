#!/bin/bash

# destroy
echo "Destroying pipeline..."
cd ./terraform/ && terraform refresh && terraform destroy

# publish lambdas
echo "Building dotnet binaries"
cd .. && dotnet publish

### zip lambda

## state lambda
rm -rf images/lambdas/StateLambda.zip
zip -j images/lambdas/StateLambda.zip RiskStateLambda/src/RiskStateLambda/bin/Debug/netcoreapp3.1/publish/*

## map lambda
rm -rf images/lambdas/MapLambda.zip
zip -j images/lambdas/MapLambda.zip RiskMapLambda/src/RiskMapLambda/bin/Debug/netcoreapp3.1/publish/*

## reduce lambda
rm -rf images/lambdas/ReduceLambda.zip
zip -j images/lambdas/ReduceLambda.zip RiskReduceLambda/src/RiskReduceLambda/bin/Debug/netcoreapp3.1/publish/*

## run terraform
echo "Applying terraform..."
cd ./terraform/ && terraform apply

echo "Waiting architecture to setup..."
sleep 120

# run acceptance tests
echo "Running accceptance tests..."
cd ../acceptance-test/ && npm test
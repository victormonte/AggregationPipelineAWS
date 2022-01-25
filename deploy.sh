#!/bin/bash

# publish lambdas
dotnet publish

## zip lambda
rm -rf images/lambdas/StateLambda.zip
zip -r images/lambdas/StateLambda.zip RiskStateLambda/src/RiskStateLambda/bin/Debug/netcoreapp3.1/publish/*
terraform {
    required_providers {
      aws = {
          source = "hashicorp/aws"
          version = "~>3.27"
      }
    }
}

provider "aws" {
    profile = "default"
    region = "eu-west-1"
}

// roles, polices, dynamo event mapper, kinesis event mapper
// kinesis stream
resource "aws_kinesis_stream" "stateless_risk_data_stream" {
    name = "StatelessRiskDataStream"
    shard_count = 1
    retention_period = 24

    shard_level_metrics = [
    "IncomingBytes",
    "OutgoingBytes",
  ]

  stream_mode_details {
    stream_mode = "PROVISIONED"
  }

  tags = {
    Environment = "test"
  }
}

// dynamodb table - state table
resource "aws_dynamodb_table" "stateful_state_table" {
  name           = "StatefulStateTable"
  billing_mode   = "PROVISIONED"
  read_capacity  = 20
  write_capacity = 20
  hash_key       = "Id"

  attribute {
    name = "Id"
    type = "S"
  }

  tags = {
    Name        = "StatefulStateTable"
    Environment = "production"
  }
} 

// state lambda
resource "aws_iam_role" "state_lambda_iam_role" {
  name = "state_lambda_iam_role"

  assume_role_policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Action": "sts:AssumeRole",
      "Principal": {
        "Service": "lambda.amazonaws.com"
      },
      "Effect": "Allow",
      "Sid": ""
    }
  ]
}
EOF
}

resource "aws_iam_policy" "state_lambda_kinesis_policy" {
  name        = "state-lambda-kinesis-policy"
  path        = "/"
  description = "Policy to allow state lambda to read form kinesis stream"

  policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
      {
          "Effect": "Allow",
          "Action": [
              "kinesis:DescribeStream",
              "kinesis:DescribeStreamSummary",
              "kinesis:GetRecords",
              "kinesis:GetShardIterator",
              "kinesis:ListShards",
              "kinesis:ListStreams",
              "kinesis:SubscribeToShard"
          ],
          "Resource": [
              "${aws_kinesis_stream.stateless_risk_data_stream.arn}"
          ]
      }
  ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "state_lambda_kinesis_policy_attach" {
  role       = aws_iam_role.state_lambda_iam_role.name
  policy_arn = aws_iam_policy.state_lambda_kinesis_policy.arn
}

resource "aws_iam_policy" "state_lambda_logs_policy" {
  name        = "state-lambda-logs-policy"
  path        = "/"
  description = "Policy to allow State Lambda create log groups"

  policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
      {
          "Effect": "Allow",
          "Action": [
              "logs:CreateLogGroup",
              "logs:CreateLogStream",
              "logs:PutLogEvents"
          ],
          "Resource": "arn:aws:logs:*:*:*"
      }
  ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "state_lambda_logs_policy_attach" {
  role       = aws_iam_role.state_lambda_iam_role.name
  policy_arn = aws_iam_policy.state_lambda_logs_policy.arn
}

resource "aws_iam_policy" "state_lambda_dynamodb_policy" {
  name        = "state-lambda-dynamodb-policy"
  description = "Policy to allow State Lambda to persist on dynamodb table"

  policy = <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
      {
          "Effect": "Allow",
          "Action": [
              "dynamodb:GetRecords",
              "dynamodb:GetShardIterator",
              "dynamodb:DescribeStream",
              "dynamodb:ListStreams"
          ],
          "Resource": [
              "${aws_dynamodb_table.stateful_state_table.arn}"
          ]
      }
  ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "state_lambda_dynamodb_policy_attach" {
  role       = aws_iam_role.state_lambda_iam_role.name
  policy_arn = aws_iam_policy.state_lambda_dynamodb_policy.arn
}

variable "state_lambda_function_name" {
  default = "RiskStateLambda"
}

resource "aws_cloudwatch_log_group" "state_lambda_log_group" {
  name              = "/aws/lambda/${var.state_lambda_function_name}"
  retention_in_days = 14
}

resource "aws_lambda_function" "state_lambda" {
  filename      = "../images/lambdas/StateLambda.zip"
  function_name = var.state_lambda_function_name
  role          = aws_iam_role.state_lambda_iam_role.arn
  handler       = "RiskStateLambda::RiskStateLambda.Function::FunctionHandler"

  runtime = "dotnetcore3.1"

  depends_on = [
    aws_cloudwatch_log_group.state_lambda_log_group,
    aws_kinesis_stream.stateless_risk_data_stream,
    aws_dynamodb_table.stateful_state_table
  ]
}

// dynamodb - reduce table
// map lambda
// dynamodb - aggregate table
// reduce lambda
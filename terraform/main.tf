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

  ttl {
    attribute_name = "TimeToExist"
    enabled        = false
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
      "Sid": "Stmt1643134282466",
      "Action": [
        "kinesis:DescribeStream",
        "kinesis:DescribeStreamSummary",
        "kinesis:GetRecords",
        "kinesis:GetShardIterator",
        "kinesis:ListShards",
        "kinesis:ListStreams",
        "kinesis:SubscribeToShard"
      ],
      "Effect": "Allow",
      "Resource": "arn:aws:kinesis:eu-west-1:528130383285:stream/StatelessRiskDataStream"
    },
    {
        "Sid": "AllowLambdaFunctionInvocation",
        "Effect": "Allow",
        "Action": [
            "lambda:InvokeFunction"
        ],
        "Resource": [
            "*"
        ]
    },
    {
        "Action": [
            "logs:CreateLogGroup",
            "logs:CreateLogStream",
            "logs:PutLogEvents"
        ],
        "Resource": "arn:aws:logs:*:*:*",
        "Effect": "Allow"
    },
    {
        "Sid": "APIAccessForDynamoDBStreams",
        "Effect": "Allow",
        "Action": [
            "dynamodb:GetRecords",
            "dynamodb:GetShardIterator",
            "dynamodb:DescribeStream",
            "dynamodb:ListStreams"
        ],
        "Resource": "arn:aws:dynamodb:eu-west-1:528130383285:table/StatefulStateTable/stream/*"
    }
  ]
}
EOF
}

variable "state_lambda_function_name" {
  default = "RiskStateLambda"
}

resource "aws_cloudwatch_log_group" "state_lambda_log_group" {
  name              = "/aws/lambda/${var.state_lambda_function_name}"
  retention_in_days = 14
}

resource "aws_lambda_function" "state_lambda" {
  filename      = "state_lambda.zip"
  function_name = var.state_lambda_function_name
  role          = aws_iam_role.state_lambda_iam_role.arn
  handler       = "RiskStateLambda::RiskStateLambda.Function::FunctionHandler"

  # The filebase64sha256() function is available in Terraform 0.11.12 and later
  # For Terraform 0.11.11 and earlier, use the base64sha256() function and the file() function:
  # source_code_hash = "${base64sha256(file("lambda_function_payload.zip"))}"
  source_code_hash = filebase64sha256("../images/lambdads/state_lambda.zip")

  runtime = "dotnetcore3.1"

#   environment {
#     variables = {
#       foo = "bar"
#     }
#   }

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
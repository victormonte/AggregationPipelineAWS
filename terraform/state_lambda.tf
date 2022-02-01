## Lambda
resource "aws_lambda_function" "state_lambda" {
  filename      = "../images/lambdas/StateLambda.zip"
  function_name = var.state_lambda_function_name
  role          = aws_iam_role.state_lambda_iam_role.arn
  handler       = "RiskStateLambda::RiskStateLambda.Function::FunctionHandler"
  timeout       = 60

  runtime = "dotnetcore3.1"

  depends_on = [
    aws_cloudwatch_log_group.state_lambda_log_group,
    aws_kinesis_stream.stateless_risk_data_stream,
    aws_dynamodb_table.stateful_state_table
  ]
}

## IAM Role
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

resource "aws_cloudwatch_log_group" "state_lambda_log_group" {
  name              = "/aws/lambda/${var.state_lambda_function_name}"
  retention_in_days = 14
}

## Kinesis Stream -> Lambda | Event Mapping
resource "aws_lambda_event_source_mapping" "kinesis_lambda_event_mapping" {
    batch_size = 10000
    event_source_arn = "${aws_kinesis_stream.stateless_risk_data_stream.arn}"
    enabled = true
    function_name = "${aws_lambda_function.state_lambda.arn}"
    starting_position = "LATEST"
}
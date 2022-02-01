## Lambda
resource "aws_lambda_function" "reduce_lambda" {
  filename      = "../images/lambdas/ReduceLambda.zip"
  function_name = var.reduce_lambda_function_name
  role          = aws_iam_role.reduce_lambda_iam_role.arn
  handler       = "RiskReduceLambda::RiskReduceLambda.Function::FunctionHandler"
  timeout       = 60

  runtime = "dotnetcore3.1"

  depends_on = [
    aws_cloudwatch_log_group.reduce_lambda_log_group,
    aws_dynamodb_table.stateless_reduce_table,
    aws_dynamodb_table.stateless_aggregate_table 
  ]
}

## IAM Role
resource "aws_iam_role" "reduce_lambda_iam_role" {
  name = "reduce_lambda_iam_role"

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

resource "aws_cloudwatch_log_group" "reduce_lambda_log_group" {
  name              = "/aws/lambda/${var.reduce_lambda_function_name}"
  retention_in_days = 14
}

## Dynamodb Stream -> Lambda
resource "aws_lambda_event_source_mapping" "reduce_lambda_dynamodb_event_mapping" {
    batch_size = 10000
    event_source_arn = "${aws_dynamodb_table.stateless_reduce_table.stream_arn}"
    enabled = true
    function_name = "${aws_lambda_function.reduce_lambda.arn}"
    starting_position = "LATEST"
}
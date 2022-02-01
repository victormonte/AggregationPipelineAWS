## Lambda
resource "aws_lambda_function" "map_lambda" {
  filename      = "../images/lambdas/MapLambda.zip"
  function_name = var.map_lambda_function_name
  role          = aws_iam_role.map_lambda_iam_role.arn
  handler       = "RiskMapLambda::RiskMapLambda.Function::FunctionHandler"
  timeout       = 60

  runtime = "dotnetcore3.1"

  depends_on = [
    aws_cloudwatch_log_group.map_lambda_log_group,
    aws_dynamodb_table.stateful_state_table,
    aws_dynamodb_table.stateless_reduce_table
  ]
}

## IAM Role
resource "aws_iam_role" "map_lambda_iam_role" {
  name = "map_lambda_iam_role"

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

resource "aws_cloudwatch_log_group" "map_lambda_log_group" {
  name              = "/aws/lambda/${var.map_lambda_function_name}"
  retention_in_days = 14
}

## Dynamodb Stream -> Lambda
resource "aws_lambda_event_source_mapping" "map_lambda_dynamodb_event_mapping" {
    batch_size = 10000
    event_source_arn = "${aws_dynamodb_table.stateful_state_table.stream_arn}"
    enabled = true
    function_name = "${aws_lambda_function.map_lambda.arn}"
    starting_position = "LATEST"
}

## Kinesis Policy
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

## Logs Policy
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

## DynamoDB Policy
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
              "dynamodb:ListStreams",
              "dynamodb:ListShards",
              "dynamodb:UpdateItem",
              "dynamodb:PutItem"
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

resource "aws_iam_policy" "map_lambda_dynamodb_policy" {
  name        = "map-lambda-dynamodb-policy"
  description = "Policy to allow Map Lambda to persist on dynamodb table"

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
              "dynamodb:ListStreams",
              "dynamodb:ListShards",
              "dynamodb:UpdateItem",
              "dynamodb:PutItem"
          ],
          "Resource": [
              "${aws_dynamodb_table.stateless_reduce_table.arn}",
              "${aws_dynamodb_table.stateful_state_table.arn}",
              "${aws_dynamodb_table.stateful_state_table.stream_arn}"
          ]
      }
  ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "map_lambda_dynamodb_policy_attach" {
  role       = aws_iam_role.map_lambda_iam_role.name
  policy_arn = aws_iam_policy.map_lambda_dynamodb_policy.arn
}

resource "aws_iam_policy" "reduce_lambda_dynamodb_policy" {
  name        = "reduce-lambda-dynamodb-policy"
  description = "Policy to allow Reduce Lambda to persist on dynamodb table"

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
              "dynamodb:ListStreams",
              "dynamodb:ListShards",
              "dynamodb:UpdateItem",
              "dynamodb:PutItem"
          ],
          "Resource": [
              "${aws_dynamodb_table.stateless_reduce_table.arn}",
              "${aws_dynamodb_table.stateless_reduce_table.stream_arn}",
              "${aws_dynamodb_table.stateless_aggregate_table.arn}"
          ]
      }
  ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "reduce_lambda_dynamodb_policy_attach" {
  role       = aws_iam_role.reduce_lambda_iam_role.name
  policy_arn = aws_iam_policy.reduce_lambda_dynamodb_policy.arn
}
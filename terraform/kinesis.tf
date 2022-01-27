
## Kinesis Stream
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
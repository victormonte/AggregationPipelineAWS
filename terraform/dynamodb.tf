
## Dynamodb Table
resource "aws_dynamodb_table" "stateful_state_table" {
  name           = "StatefulStateTable"
  billing_mode   = "PROVISIONED"
  read_capacity  = 20
  write_capacity = 20
  hash_key       = "Id"
  stream_enabled   = true
  stream_view_type = "NEW_AND_OLD_IMAGES"

  attribute {
    name = "Id"
    type = "S"
  }

  tags = {
    Name        = "StatefulStateTable"
    Environment = "production"
  }
} 

resource "aws_dynamodb_table" "stateless_reduce_table" {
  name           = "StatelessReduceTable"
  billing_mode   = "PROVISIONED"
  read_capacity  = 20
  write_capacity = 20
  hash_key       = "MessageHash"
  stream_enabled   = true
  stream_view_type = "NEW_AND_OLD_IMAGES"

  attribute {
    name = "MessageHash"
    type = "S"
  }

  tags = {
    Name        = "StatefulStateTable"
    Environment = "production"
  }
} 

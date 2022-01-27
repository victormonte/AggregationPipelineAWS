
## Dynamodb Table
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

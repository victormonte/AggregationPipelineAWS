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

## TODO
## [ ] - dynamodb - reduce table
## [ ] - map lambda
## [ ] - dynamodb - aggregate table
## [ ] - reduce lambda
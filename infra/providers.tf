provider "aws" {
  region = var.region

  default_tags {
    tags = {
      Project   = "Future Unicorn"
      ManagedBy = "terraform"
    }
  }
}
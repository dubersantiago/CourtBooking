output "vpc_id" {
  value = data.aws_vpc.default.id
}

output "subnet_ids" {
  value = data.aws_subnets.default.ids
}

output "instance_id" {
  value = aws_instance.api.id
}

output "public_ip" {
  value = aws_instance.api.public_ip
}
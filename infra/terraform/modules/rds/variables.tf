variable "project" { type = string }
variable "environment" { type = string }
variable "vpc_id" { type = string }
variable "private_subnet_ids" { type = list(string) }
variable "eks_sg_id" { type = string }
variable "instance_class" { type = string }
variable "multi_az" { type = bool }
variable "db_password" {
  type      = string
  sensitive = true
}

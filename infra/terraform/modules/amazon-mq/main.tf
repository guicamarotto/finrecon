locals {
  name_prefix = "${var.project}-${var.environment}"
}

resource "aws_security_group" "mq" {
  name        = "${local.name_prefix}-mq-sg"
  description = "Allow AMQP from EKS nodes only"
  vpc_id      = var.vpc_id

  ingress {
    from_port       = 5671
    to_port         = 5671
    protocol        = "tcp"
    security_groups = [var.eks_sg_id]
    description     = "AMQPS from EKS nodes"
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_mq_broker" "rabbitmq" {
  broker_name        = "${local.name_prefix}-rabbitmq"
  engine_type        = "RabbitMQ"
  engine_version     = "3.13"
  host_instance_type = var.instance_type
  deployment_mode    = "SINGLE_INSTANCE"

  subnet_ids         = [var.private_subnet_ids[0]]
  security_groups    = [aws_security_group.mq.id]

  publicly_accessible = false

  user {
    username = "finrecon"
    password = var.rabbitmq_password
  }

  tags = { Name = "${local.name_prefix}-rabbitmq" }
}

module "vpc" {
  source      = "./modules/vpc"
  project     = var.project
  environment = var.environment
  vpc_cidr    = var.vpc_cidr
  aws_region  = var.aws_region
}

module "eks" {
  source              = "./modules/eks"
  project             = var.project
  environment         = var.environment
  vpc_id              = module.vpc.vpc_id
  private_subnet_ids  = module.vpc.private_subnet_ids
  node_instance_type  = var.eks_node_instance_type
  node_desired        = var.eks_node_desired
  node_min            = var.eks_node_min
  node_max            = var.eks_node_max
}

module "rds" {
  source             = "./modules/rds"
  project            = var.project
  environment        = var.environment
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  eks_sg_id          = module.eks.node_security_group_id
  instance_class     = var.rds_instance_class
  multi_az           = var.rds_multi_az
  db_password        = var.db_password
}

module "s3" {
  source      = "./modules/s3"
  project     = var.project
  environment = var.environment
  oidc_provider_arn = module.eks.oidc_provider_arn
  oidc_provider_url = module.eks.oidc_provider_url
}

module "amazon_mq" {
  source             = "./modules/amazon-mq"
  project            = var.project
  environment        = var.environment
  vpc_id             = module.vpc.vpc_id
  private_subnet_ids = module.vpc.private_subnet_ids
  eks_sg_id          = module.eks.node_security_group_id
  instance_type      = var.mq_instance_type
  rabbitmq_password  = var.rabbitmq_password
}

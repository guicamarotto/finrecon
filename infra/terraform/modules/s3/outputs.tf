output "bucket_name" { value = aws_s3_bucket.uploads.bucket }
output "bucket_arn" { value = aws_s3_bucket.uploads.arn }
output "worker_role_arn" { value = aws_iam_role.worker_s3.arn }

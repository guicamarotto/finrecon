namespace FinRecon.Infrastructure.Storage;

public class MinioOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "finrecon-uploads";
    public bool UseSSL { get; set; } = false;
}

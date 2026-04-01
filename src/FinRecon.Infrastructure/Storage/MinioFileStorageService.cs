using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using FinRecon.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinRecon.Infrastructure.Storage;

public class MinioFileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3;
    private readonly MinioOptions _options;
    private readonly ILogger<MinioFileStorageService> _logger;

    public MinioFileStorageService(IOptions<MinioOptions> options, ILogger<MinioFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = _options.UseSSL
                ? $"https://{_options.Endpoint}"
                : $"http://{_options.Endpoint}",
            ForcePathStyle = true, // Required for MinIO compatibility
            AuthenticationRegion = "us-east-1"
        };

        _s3 = new AmazonS3Client(_options.AccessKey, _options.SecretKey, config);
    }

    public async Task<string> UploadAsync(string filename, Stream content, string contentType, CancellationToken ct = default)
    {
        var objectKey = BuildObjectKey(filename);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        };

        try
        {
            await _s3.PutObjectAsync(request, ct);
            _logger.LogInformation("Uploaded file {Filename} to {ObjectKey}", filename, objectKey);
            return objectKey;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {Filename}", filename);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey
        };

        try
        {
            var response = await _s3.GetObjectAsync(request, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to download object {ObjectKey}", objectKey);
            throw;
        }
    }

    private static string BuildObjectKey(string filename)
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var uniqueId = Guid.NewGuid();
        return $"{date}/{uniqueId}/{filename}";
    }
}

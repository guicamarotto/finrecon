namespace FinRecon.Core.Interfaces;

public interface IFileStorageService
{
    /// <summary>Uploads a file and returns the storage object key.</summary>
    Task<string> UploadAsync(string filename, Stream content, string contentType, CancellationToken ct = default);

    /// <summary>Downloads a file by its storage object key.</summary>
    Task<Stream> DownloadAsync(string objectKey, CancellationToken ct = default);
}

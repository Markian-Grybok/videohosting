namespace FileService.Infrastructure.Storage
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream stream, string objectName, string contentType, CancellationToken ct);
        Task DownloadFileAsync(string objectName, string destinationPath, CancellationToken ct);
        Task UploadDirectoryAsync(string localDirectory, string storagePrefix, CancellationToken ct);
        Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds, CancellationToken ct);
        Task DeleteFileAsync(string objectName, CancellationToken ct);
        Task DeleteDirectoryAsync(string prefix, CancellationToken ct);
    }
}

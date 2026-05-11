using Minio;
using Minio.DataModel.Args;
using Minio.DataModel;
using Minio.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FileService.Common.Exceptions;
using System.Threading;

namespace FileService.Infrastructure.Storage
{
    public class MinioStorageService : IStorageService
    {
        private readonly IMinioClient _minioClient;
        private readonly MinioOptions _options;
        private readonly ILogger<MinioStorageService> _logger;
        private volatile bool _bucketInitialized = false;
        private readonly SemaphoreSlim _bucketInitSemaphore = new(1, 1);

        public MinioStorageService(IOptions<MinioOptions> options, ILogger<MinioStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;
            _minioClient = new MinioClient()
                .WithEndpoint(_options.Endpoint)
                .WithCredentials(_options.AccessKey, _options.SecretKey)
                .WithSSL(_options.UseSSL)
                .Build();
        }

        private async Task EnsureBucketExistsAsync(CancellationToken ct)
        {
            if (_bucketInitialized)
                return;

            await _bucketInitSemaphore.WaitAsync(ct);
            try
            {
                if (_bucketInitialized)
                    return;

                var beArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
                bool found = await _minioClient.BucketExistsAsync(beArgs, ct);
                if (!found)
                {
                    var mbArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
                    await _minioClient.MakeBucketAsync(mbArgs, ct);
                    _logger.LogInformation("Bucket created: {Bucket}", _options.BucketName);
                }
                _bucketInitialized = true;
            }
            finally
            {
                _bucketInitSemaphore.Release();
            }
        }

        public async Task<string> UploadFileAsync(Stream stream, string objectName, string contentType, CancellationToken ct)
        {
            await EnsureBucketExistsAsync(ct);
            try
            {
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(stream.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs, ct);
                _logger.LogInformation("File uploaded successfully: {ObjectName}", objectName);
                return objectName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {ObjectName}", objectName);
                throw new StorageException($"Failed to upload file: {objectName}", ex);
            }
        }

        public async Task DownloadFileAsync(string objectName, string destinationPath, CancellationToken ct)
        {
            await EnsureBucketExistsAsync(ct);
            try
            {
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
                {
                    var getObjectArgs = new GetObjectArgs()
                        .WithBucket(_options.BucketName)
                        .WithObject(objectName)
                        .WithCallbackStream((stream) =>
                        {
                            stream.CopyTo(fileStream);
                        });

                    await _minioClient.GetObjectAsync(getObjectArgs, ct);
                }

                _logger.LogInformation("File downloaded successfully: {ObjectName}", objectName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {ObjectName}", objectName);
                throw new StorageException($"Failed to download file: {objectName}", ex);
            }
        }

        public async Task UploadDirectoryAsync(string localDirectory, string storagePrefix, CancellationToken ct)
        {
            await EnsureBucketExistsAsync(ct);
            try
            {
                if (!Directory.Exists(localDirectory))
                {
                    throw new StorageException($"Directory does not exist: {localDirectory}");
                }

                var files = Directory.GetFiles(localDirectory, "*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(localDirectory, file);
                    var objectName = $"{storagePrefix}/{relativePath.Replace("\\", "/")}";



                    using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        var contentType = GetContentType(file);
                        await UploadFileAsync(fileStream, objectName, contentType, ct);
                    }
                }

                _logger.LogInformation("Directory uploaded successfully: {LocalDirectory}", localDirectory);
            }
            catch (StorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading directory: {LocalDirectory}", localDirectory);
                throw new StorageException($"Failed to upload directory: {localDirectory}", ex);
            }
        }

        public async Task<string> GetPresignedUrlAsync(string objectName, int expirySeconds, CancellationToken ct)
        {
            await EnsureBucketExistsAsync(ct);
            try
            {
                var presignedGetObjectArgs = new PresignedGetObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectName)
                    .WithExpiry(expirySeconds);

                var url = await _minioClient.PresignedGetObjectAsync(presignedGetObjectArgs);
                _logger.LogInformation("Presigned URL generated for: {ObjectName}", objectName);
                return url;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating presigned URL: {ObjectName}", objectName);
                throw new StorageException($"Failed to generate presigned URL: {objectName}", ex);
            }
        }

        public async Task DeleteFileAsync(string objectName, CancellationToken ct)
        {
            await EnsureBucketExistsAsync(ct);
            try
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(_options.BucketName)
                    .WithObject(objectName);
                await _minioClient.RemoveObjectAsync(args, ct);
                _logger.LogInformation("Deleted object {ObjectName} from bucket {Bucket}", objectName, _options.BucketName);
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "Error deleting object {ObjectName}", objectName);
                throw new StorageException($"Failed to delete object: {objectName}", ex);
            }
        }

        public async Task DeleteDirectoryAsync(string prefix, CancellationToken ct)
        {
            await EnsureBucketExistsAsync(ct);
            try
            {
                var objectNames = new List<string>();

                // Новий спосіб в MinIO 7.x - використовуємо асинхронний перебір
                var listArgs = new ListObjectsArgs()
                    .WithBucket(_options.BucketName)
                    .WithPrefix(prefix)
                    .WithRecursive(true);

                await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs, ct))
                {
                    if (!string.IsNullOrEmpty(item.Key))
                    {
                        objectNames.Add(item.Key);
                    }
                }

                if (objectNames.Count == 0)
                {
                    _logger.LogInformation("No objects to delete for prefix {Prefix}", prefix);
                    return;
                }

                // Видалення пачками (по 1000 об'єктів за раз)
                var batchSize = 1000;
                for (int i = 0; i < objectNames.Count; i += batchSize)
                {
                    var batch = objectNames.Skip(i).Take(batchSize).ToList();
                    var removeArgs = new RemoveObjectsArgs()
                        .WithBucket(_options.BucketName)
                        .WithObjects(batch);

                    var deleteErrors = await _minioClient.RemoveObjectsAsync(removeArgs, ct);
                    int deleted = batch.Count - (deleteErrors?.Count ?? 0);
                    _logger.LogDebug("Deleted {Count} objects in batch", deleted);
                }

                _logger.LogInformation("Deleted {Count} objects with prefix {Prefix}", objectNames.Count, prefix);
            }
            catch (MinioException ex)
            {
                _logger.LogError(ex, "Error deleting directory with prefix {Prefix}", prefix);
                throw new StorageException($"Failed to delete directory: {prefix}", ex);
            }
        }

        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".mp4" => "video/mp4",
                ".m3u8" => "application/vnd.apple.mpegurl",
                ".ts" => "video/mp2t",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".json" => "application/json",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
        }
    }
}

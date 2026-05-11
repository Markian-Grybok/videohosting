using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FileService.Features.Delete
{
    public class DeleteVideoCommandHandler : IRequestHandler<DeleteVideoCommand, DeleteVideoResult>
    {
        private readonly FileServiceDbContext _context;
        private readonly IStorageService _storage;
        private readonly ILogger<DeleteVideoCommandHandler> _logger;

        public DeleteVideoCommandHandler(
            FileServiceDbContext context,
            IStorageService storage,
            ILogger<DeleteVideoCommandHandler> logger)
        {
            _context = context;
            _storage = storage;
            _logger = logger;
        }

        public async Task<DeleteVideoResult> Handle(
            DeleteVideoCommand request,
            CancellationToken cancellationToken)
        {
            // 1. Load VideoFile from DB
            var videoFile = await _context.VideoFiles
                .FirstOrDefaultAsync(v => v.Id == request.FileId, cancellationToken);

            if (videoFile is null)
            {
                _logger.LogWarning(
                    "VideoFile {FileId} not found for deletion.", request.FileId);
                return new DeleteVideoResult(true, null);
                // Return success — idempotent delete (already gone = ok)
            }

            // 2. Delete original file from MinIO
            try
            {
                await _storage.DeleteFileAsync(videoFile.StoragePath, cancellationToken);
                _logger.LogInformation(
                    "Deleted original file {Path} from storage.", videoFile.StoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Could not delete original file {Path} from storage. Continuing.",
                    videoFile.StoragePath);
                // Do NOT return error — continue to delete HLS and DB record
            }

            // 3. Delete HLS segments from MinIO (if video was processed)
            if (videoFile.HlsManifestPath is not null)
            {
                try
                {
                    var hlsPrefix = $"hls/{request.FileId}/";
                    await _storage.DeleteDirectoryAsync(hlsPrefix, cancellationToken);
                    _logger.LogInformation(
                        "Deleted HLS segments at prefix {Prefix}.", hlsPrefix);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Could not delete HLS segments for {FileId}. Continuing.",
                        request.FileId);
                }
            }

            // 4. Delete DB record
            _context.VideoFiles.Remove(videoFile);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("VideoFile {FileId} deleted successfully.", request.FileId);
            return new DeleteVideoResult(true, null);
        }
    }
}

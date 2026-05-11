namespace ContentService.API.Infrastructure.Http;

public interface IFileServiceClient
{
    Task<FileStatusResponse?> GetFileStatusAsync(Guid fileId, CancellationToken ct);
    Task<PlaybackUrlResponse?> GetPlaybackUrlAsync(Guid fileId, CancellationToken ct);
    Task<bool> DeleteFileAsync(Guid fileId, CancellationToken ct);
}

using Microsoft.Extensions.Logging;
using System.Net;

namespace ContentService.API.Infrastructure.Http;

public class FileServiceClient : IFileServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileServiceClient> _logger;

    public FileServiceClient(HttpClient httpClient, ILogger<FileServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FileStatusResponse?> GetFileStatusAsync(Guid fileId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/files/{fileId}/processing-status", ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<FileStatusResponse>(cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "FileService unavailable when fetching status for {FileId}", fileId);
            return null;
        }
    }

    public async Task<PlaybackUrlResponse?> GetPlaybackUrlAsync(Guid fileId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/files/{fileId}/play", ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PlaybackUrlResponse>(cancellationToken: ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "FileService unavailable when fetching playback URL for {FileId}", fileId);
            return null;
        }
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/files/{fileId}", ct);
            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
                return true;
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "FileService unavailable when deleting file {FileId}.", fileId);
            return false;
        }
    }
}

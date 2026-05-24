using MediatR;
using FileService.Common.Entities;
using FileService.Infrastructure.Persistence;
using FileService.Infrastructure.Storage;
using FileService.Features.Streaming.Dtos;
using FileService.Features.Processing;

namespace FileService.Features.Streaming.Queries
{
    public class GetPlaybackUrlQueryHandler : IRequestHandler<GetPlaybackUrlQuery, PlaybackUrlDto>
    {
        private readonly FileServiceDbContext _dbContext;
        private readonly IStorageService _storageService;
        private const int PresignedUrlExpirySeconds = 3600;
        private static readonly HashSet<string> AllowedQualities = new() { "360p", "480p", "720p", "1080p", "master" };

        public GetPlaybackUrlQueryHandler(FileServiceDbContext dbContext, IStorageService storageService)
        {
            _dbContext = dbContext;
            _storageService = storageService;
        }

        public async Task<PlaybackUrlDto> Handle(GetPlaybackUrlQuery request, CancellationToken cancellationToken)
        {
            var video = await _dbContext.VideoFiles.FindAsync(new object[] { request.FileId }, cancellationToken);

            if (video == null)
                throw new KeyNotFoundException($"Video file not found: {request.FileId}");

            if (video.Status != VideoFileStatus.Ready)
                throw new InvalidOperationException("Video is not ready for playback");

            // Determine the path to use
            string manifestPath;
            if (string.IsNullOrEmpty(request.Quality) || request.Quality == "master")
            {
                // Use master.m3u8
                manifestPath = video.HlsManifestPath ?? $"hls/{request.FileId}/master.m3u8";
            }
            else
            {
                // Validate quality parameter
                if (!AllowedQualities.Contains(request.Quality))
                    throw new ArgumentException($"Invalid quality: {request.Quality}");

                // Construct quality-specific path
                manifestPath = $"hls/{request.FileId}/{request.Quality}/index.m3u8";
            }

            var url = await _storageService.GetPresignedUrlAsync(manifestPath, PresignedUrlExpirySeconds, cancellationToken);

            return new PlaybackUrlDto(request.FileId, url, PresignedUrlExpirySeconds);
        }
    }
}

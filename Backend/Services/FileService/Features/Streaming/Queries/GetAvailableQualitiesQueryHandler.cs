using MediatR;
using FileService.Common.Entities;
using FileService.Infrastructure.Persistence;
using FileService.Features.Streaming.Dtos;

namespace FileService.Features.Streaming.Queries
{
    public class GetAvailableQualitiesQueryHandler : IRequestHandler<GetAvailableQualitiesQuery, AvailableQualitiesDto>
    {
        private readonly FileServiceDbContext _dbContext;

        public GetAvailableQualitiesQueryHandler(FileServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AvailableQualitiesDto> Handle(GetAvailableQualitiesQuery request, CancellationToken cancellationToken)
        {
            var video = await _dbContext.VideoFiles.FindAsync(new object[] { request.FileId }, cancellationToken);

            if (video == null)
                throw new KeyNotFoundException($"Video file not found: {request.FileId}");

            if (video.Status != VideoFileStatus.Ready)
                throw new InvalidOperationException("Video is not ready");

            var qualities = string.IsNullOrEmpty(video.AvailableQualities)
                ? new List<string>()
                : video.AvailableQualities.Split(',').ToList();

            return new AvailableQualitiesDto(request.FileId, qualities);
        }
    }
}

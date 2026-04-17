using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FileService.Common.Entities;
using FileService.Infrastructure.Persistence;

namespace FileService.Features.Processing.Queries
{
    public class GetProcessingStatusQueryHandler : IRequestHandler<GetProcessingStatusQuery, ProcessingStatusResult?>
    {
        private readonly FileServiceDbContext _dbContext;
        public GetProcessingStatusQueryHandler(FileServiceDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ProcessingStatusResult?> Handle(GetProcessingStatusQuery request, CancellationToken cancellationToken)
        {
            var video = await _dbContext.VideoFiles.FindAsync(new object[] { request.FileId }, cancellationToken);
            if (video == null) return null;
            return new ProcessingStatusResult(
                video.Id,
                video.Status.ToString(),
                video.Progress,
                video.ErrorMessage
            );
        }
    }
}

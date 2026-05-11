using ContentService.API.Data;
using ContentService.API.Infrastructure.Http;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContentService.API.Features.Lessons.Delete;

public class DeleteLessonHandler 
    : IRequestHandler<DeleteLessonCommand, Result>
{
    private readonly ContentDbContext _context;
    private readonly IFileServiceClient _fileServiceClient;
    private readonly ILogger<DeleteLessonHandler> _logger;

    public DeleteLessonHandler(
        ContentDbContext context,
        IFileServiceClient fileServiceClient,
        ILogger<DeleteLessonHandler> logger)
    {
        _context = context;
        _fileServiceClient = fileServiceClient;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteLessonCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load lesson
        var lesson = await _context.Lessons
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (lesson is null)
            return Result.Fail("Lesson not found");

        // 2. Delete video from FileService if lesson has one
        if (lesson.VideoFileId.HasValue)
        {
            var deleted = await _fileServiceClient
                .DeleteFileAsync(lesson.VideoFileId.Value, cancellationToken);

            if (!deleted)
                _logger.LogWarning(
                    "Could not delete video {VideoFileId} for lesson {LessonId}. " +
                    "Proceeding with lesson deletion.",
                    lesson.VideoFileId.Value, request.Id);
            else
                _logger.LogInformation(
                    "Deleted video {VideoFileId} for lesson {LessonId}.",
                    lesson.VideoFileId.Value, request.Id);
        }

        // 3. Delete lesson from DB
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}

using ContentService.API.Data;
using ContentService.API.Infrastructure.Http;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContentService.API.Features.Courses.Delete;

public class DeleteCourseHandler
    : IRequestHandler<DeleteCourseCommand, Result>
{
    private readonly ContentDbContext _context;
    private readonly IFileServiceClient _fileServiceClient;
    private readonly ILogger<DeleteCourseHandler> _logger;

    public DeleteCourseHandler(
        ContentDbContext context,
        IFileServiceClient fileServiceClient,
        ILogger<DeleteCourseHandler> logger)
    {
        _context = context;
        _fileServiceClient = fileServiceClient;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteCourseCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Load course WITH lessons (need videoFileIds)
        var course = await _context.Courses
            .Include(c => c.Lessons)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (course is null)
            return Result.Fail("Course not found");

        // 2. Collect all videoFileIds from lessons
        var videoFileIds = course.Lessons
            .Where(l => l.VideoFileId.HasValue)
            .Select(l => l.VideoFileId!.Value)
            .ToList();

        // 3. Delete all videos from FileService in parallel
        if (videoFileIds.Count > 0)
        {
            _logger.LogInformation(
                "Deleting {Count} video(s) for course {CourseId}.",
                videoFileIds.Count, request.Id);

            var deleteTasks = videoFileIds.Select(fileId =>
                _fileServiceClient.DeleteFileAsync(fileId, cancellationToken));

            var results = await Task.WhenAll(deleteTasks);

            var failedCount = results.Count(r => !r);
            if (failedCount > 0)
                _logger.LogWarning(
                    "{FailedCount} video(s) could not be deleted from FileService " +
                    "for course {CourseId}. Proceeding with course deletion.",
                    failedCount, request.Id);
        }

        // 4. Delete course from DB
        // EF Core cascade delete removes all lessons automatically
        // (configured in CourseConfiguration: OnDelete(DeleteBehavior.Cascade))
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Course {CourseId} deleted successfully.", request.Id);
        return Result.Ok();
    }
}

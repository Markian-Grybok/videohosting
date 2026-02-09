using EducationContentService.Core.EndpointsSettings;
using EducationContentService.Domain.Interfaces;
using EducationContentService.Domain.Lesson;
using EducationContentService.Domain.Lesson.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace EducationContentService.Core.Features.Lessons;

public sealed class DeleteEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(
            "/lessons/{id}",
            async (
                [FromRoute] Guid id,
                [FromServices] DeleteHandler handler,
                CancellationToken cancellationToken) =>
                    await handler.Handle(id, cancellationToken));
    }
}

public sealed class DeleteHandler
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ILogger<DeleteHandler> _logger;

    public DeleteHandler(
        ILessonRepository lessonRepository,
        ILogger<DeleteHandler> logger)
    {
        _lessonRepository = lessonRepository;
        _logger = logger;
    }

    public async Task<Guid> Handle(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            LessonId lessonId = LessonId.Create(id);

            Lesson? lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);

            if (lesson is null)
            {
                _logger.LogWarning("Attempt to delete non-existent lesson with ID {LessonId}", id);
                throw new InvalidOperationException("Lesson not found");
            }

            await _lessonRepository.Delete(lesson, cancellationToken);

            _logger.LogInformation("Lesson with ID {LessonId} was deleted successfully", id);

            return lessonId.Value;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Validation error while deleting lesson: {Message}", ex.Message);
            throw new BadHttpRequestException(ex.Message, StatusCodes.Status404NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting lesson with ID {Id}", id);
            throw new ArgumentException("An error occurred while deleting the lesson. Please try again later.");
        }
    }
}
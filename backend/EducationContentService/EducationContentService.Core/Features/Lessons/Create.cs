using EducationContentService.Core.EndpointsSettings;
using EducationContentService.Core.Features.Lessons.DTOs;
using EducationContentService.Domain.Interfaces;
using EducationContentService.Domain.Lesson;
using EducationContentService.Domain.Lesson.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace EducationContentService.Core.Features.Lessons;

public sealed class CreateEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            "/lessons",
            async Task (
            [FromBody] CreateLessonRequest request,
            [FromServices] CreateHanlder handler,
            CancellationToken cancellationToken) =>
                await handler.Handle(request, cancellationToken));
    }
}

public sealed class CreateHanlder
{
    private readonly ILogger<CreateHanlder> _logger;
    private readonly ILessonRepository _lessonsRepository;

    public CreateHanlder(
        ILogger<CreateHanlder> logger,
        ILessonRepository lessonsRepository)
    {
        _logger = logger;
        _lessonsRepository = lessonsRepository;
    }

    public async Task<Guid> Handle(
        CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Title title = Title.Create(request.Title);
            Description description = Description.Create(request.Description);

            Lesson lesson = Lesson.Create(title, description);

            await _lessonsRepository.AddAsync(lesson, cancellationToken);

            _logger.LogInformation("Lesson created successfully with ID {LessonId}", lesson.Id.Value);

            return lesson.Id.Value;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation error while creating lesson: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating lesson");
            throw;
        }
    }
}

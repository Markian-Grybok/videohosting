using ContentService.Application.Interfaces;
using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Commands.Lesson.CreateLesson;

public class CreateLessonHandler : IRequestHandler<CreateLessonCommand, Result<LessonResponse>>
{
    private readonly ILessonRepository _lessonRepository;

    public CreateLessonHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<Result<LessonResponse>> Handle(
        CreateLessonCommand request,
        CancellationToken cancellationToken)
    {
        var lesson = Domain.Entities.Lesson.Create(request.Title, request.Description);

        var result = await _lessonRepository.AddAsync(lesson, cancellationToken);

        if (result.IsFailed)
            return Result.Fail<LessonResponse>(result.Errors);

        return Result.Ok(LessonResponse.FromLesson(lesson));
    }
}

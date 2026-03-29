using ContentService.Application.Interfaces;
using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;
namespace ContentService.Application.Commands.Lesson.UpdateLesson;

public class UpdateLessonHandler : IRequestHandler<UpdateLessonCommand, Result<LessonResponse>>
{
    private readonly ILessonRepository _lessonRepository;

    public UpdateLessonHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<Result<LessonResponse>> Handle(
        UpdateLessonCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonRepository.GetByIdAsync(request.Id, cancellationToken);

        if (result.IsFailed)
            return Result.Fail<LessonResponse>(result.Errors);

        var lesson = result.Value;
        lesson.Update(request.Title, request.Description);

        var updateResult = await _lessonRepository.UpdateAsync(lesson, cancellationToken);

        if (updateResult.IsFailed)
            return Result.Fail<LessonResponse>(updateResult.Errors);

        return Result.Ok(LessonResponse.FromLesson(lesson));
    }
}

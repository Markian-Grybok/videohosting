using ContentService.Application.Commands.Lesson.DeleteLessonl;
using ContentService.Application.Interfaces;
using FluentResults;
using MediatR;
namespace ContentService.Application.Commands.Lesson.DeleteLesson;

public class DeleteLessonHandler : IRequestHandler<DeleteLessonCommand, Result>
{
    private readonly ILessonRepository _lessonRepository;

    public DeleteLessonHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<Result> Handle(
        DeleteLessonCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonRepository.GetByIdAsync(request.Id, cancellationToken);

        if (result.IsFailed)
            return Result.Fail(result.Errors);

        var deleteResult = await _lessonRepository.DeleteAsync(result.Value, cancellationToken);

        if (deleteResult.IsFailed)
            return Result.Fail(deleteResult.Errors);

        return Result.Ok();
    }
}
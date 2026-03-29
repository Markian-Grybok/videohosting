using ContentService.Application.Interfaces;
using ContentService.Application.Queries.Lesson.GetAllLessons;
using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Queries.Lesson.GetAllLessonsl;

public class GetAllLessonsHandler : IRequestHandler<GetAllLessonsQuery, Result<IReadOnlyList<LessonResponse>>>
{
    private readonly ILessonRepository _lessonRepository;

    public GetAllLessonsHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<Result<IReadOnlyList<LessonResponse>>> Handle(
        GetAllLessonsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonRepository.GetAllAsync(cancellationToken);

        if (result.IsFailed)
            return Result.Fail<IReadOnlyList<LessonResponse>>(result.Errors);

        var response = result.Value
            .Select(LessonResponse.FromLesson)
            .ToList();

        return Result.Ok<IReadOnlyList<LessonResponse>>(response);
    }
}
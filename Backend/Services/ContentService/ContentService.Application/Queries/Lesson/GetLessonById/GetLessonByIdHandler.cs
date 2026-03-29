using ContentService.Application.Interfaces;
using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Queries.Lesson.GetLessonById;

public class GetLessonByIdHandler : IRequestHandler<GetLessonByIdQuery, Result<LessonResponse>>
{
    private readonly ILessonRepository _lessonRepository;

    public GetLessonByIdHandler(ILessonRepository lessonRepository)
    {
        _lessonRepository = lessonRepository;
    }

    public async Task<Result<LessonResponse>> Handle(
        GetLessonByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _lessonRepository.GetByIdAsync(request.Id, cancellationToken);

        if (result.IsFailed)
            return Result.Fail<LessonResponse>(result.Errors);

        return Result.Ok(LessonResponse.FromLesson(result.Value));
    }
}
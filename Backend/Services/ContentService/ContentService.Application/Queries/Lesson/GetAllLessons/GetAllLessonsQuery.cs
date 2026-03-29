using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Queries.Lesson.GetAllLessons;

public record GetAllLessonsQuery : IRequest<Result<IReadOnlyList<LessonResponse>>>;

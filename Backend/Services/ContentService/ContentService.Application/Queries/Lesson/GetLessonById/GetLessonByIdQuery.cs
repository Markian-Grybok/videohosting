using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Queries.Lesson.GetLessonById;

public record GetLessonByIdQuery(Guid Id) : IRequest<Result<LessonResponse>>;

using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Commands.Lesson.UpdateLesson;

public record UpdateLessonCommand(Guid Id, string Title, string Description) : IRequest<Result<LessonResponse>>;

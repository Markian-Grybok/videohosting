using FluentResults;
using MediatR;

namespace ContentService.Application.Commands.Lesson.DeleteLessonl;

public record DeleteLessonCommand(Guid Id) : IRequest<Result>;

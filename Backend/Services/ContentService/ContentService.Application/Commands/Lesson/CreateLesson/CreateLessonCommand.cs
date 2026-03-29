using ContentService.Application.Responses.Lesson;
using FluentResults;
using MediatR;

namespace ContentService.Application.Commands.Lesson.CreateLesson;

public record CreateLessonCommand(string Title, string Description) : IRequest<Result<LessonResponse>>;

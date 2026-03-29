namespace ContentService.Application.Responses.Lesson;

public record LessonResponse(
    Guid Id,
    string Title,
    string Description,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static LessonResponse FromLesson(Domain.Entities.Lesson lesson) => new(
        lesson.Id,
        lesson.Title,
        lesson.Description,
        lesson.CreatedAt,
        lesson.UpdatedAt);
}

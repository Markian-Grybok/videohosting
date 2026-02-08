namespace EducationContentService.Core.Features.Lessons.DTOs;

public record CreateLessonRequest(string Title, string Description, DateTime CreatedAt);
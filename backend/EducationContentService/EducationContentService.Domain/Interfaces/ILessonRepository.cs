namespace EducationContentService.Domain.Interfaces;

public interface ILessonRepository
{
        Task AddAsync(Lesson.Lesson lesson, CancellationToken cancellationToken);
        Task<Lesson.Lesson?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task UpdateAsync(Lesson.Lesson lesson, CancellationToken cancellationToken);
        Task DeleteAsync(Lesson.Lesson lesson, CancellationToken cancellationToken);
}

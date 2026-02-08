using EducationContentService.Domain.Lesson.ValueObjects;

namespace EducationContentService.Domain.Interfaces;

public interface ILessonRepository
{
        Task<LessonId> AddAsync(Lesson.Lesson lesson, CancellationToken cancellationToken);
        Task<IEnumerable<Lesson.Lesson>> GetAllAsync(CancellationToken cancellationToken);
        Task<Lesson.Lesson?> GetByIdAsync(LessonId lessonId, CancellationToken cancellationToken);
        Task UpdateAsync(Lesson.Lesson lesson, CancellationToken cancellationToken);
        Task Delete(Lesson.Lesson lesson, CancellationToken cancellationToken);
        Task SaveChangesAsync(CancellationToken cancellationToken);
}

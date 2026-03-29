using ContentService.Domain.Entities;
using FluentResults;

namespace ContentService.Application.Interfaces;

public interface ILessonRepository
{
    Task<Result<Lesson>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Lesson>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result> AddAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

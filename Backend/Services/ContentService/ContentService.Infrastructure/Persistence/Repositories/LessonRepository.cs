using ContentService.Application.Interfaces;
using ContentService.Domain.Entities;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Infrastructure.Persistence.Repositories;

public class LessonRepository : ILessonRepository
{
    private readonly ContentDbContext _context;

    public LessonRepository(ContentDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Lesson>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lesson = await _context.Lessons
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return lesson is null
            ? Result.Fail<Lesson>($"Lesson with id '{id}' was not found.")
            : Result.Ok(lesson);
    }

    public async Task<Result<IReadOnlyList<Lesson>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var lessons = await _context.Lessons
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Ok<IReadOnlyList<Lesson>>(lessons);
    }

    public async Task<Result> AddAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        await _context.Lessons.AddAsync(lesson, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> UpdateAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _context.Lessons.Update(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Lesson lesson, CancellationToken cancellationToken = default)
    {
        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<bool>> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _context.Lessons
            .AsNoTracking()
            .AnyAsync(l => l.Id == id, cancellationToken);

        return Result.Ok(exists);
    }
}
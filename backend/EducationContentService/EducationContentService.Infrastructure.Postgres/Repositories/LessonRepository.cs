using EducationContentService.Domain.Interfaces;
using EducationContentService.Domain.Lesson;
using EducationContentService.Domain.Lesson.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EducationContentService.Infrastructure.Postgres.Repositories;

public class LessonRepository : ILessonRepository
{
    private readonly EducationDbContext _dbContext;
    private readonly ILogger<LessonRepository> _logger;

    public LessonRepository(EducationDbContext dbContext, ILogger<LessonRepository> logger) =>
        (_dbContext, _logger) = (dbContext, logger);

    public async Task<LessonId> AddAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Lessons.AddAsync(lesson, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lesson with ID {LessonId} was added successfully", lesson.Id);
            return lesson.Id;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while adding lesson");
            throw new InvalidOperationException("An error occurred while adding the lesson to the database.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while adding lesson");
            throw new InvalidOperationException("An unexpected error occurred while adding the lesson.", ex);
        }
    }

    public async Task<Lesson?> GetByIdAsync(LessonId lessonId, CancellationToken cancellationToken)
    {
        try
        {
            Lesson? lesson = await _dbContext.Lessons
                .FirstOrDefaultAsync(l => l.Id == lessonId, cancellationToken);

            if (lesson is null)
            {
                _logger.LogWarning("Lesson with ID {LessonId} was not found", lessonId);
                return null;
            }

            _logger.LogInformation("Lesson with ID {LessonId} was retrieved successfully", lessonId);
            return lesson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving lesson by ID {LessonId}", lessonId);
            throw new InvalidOperationException("An error occurred while retrieving the lesson.", ex);
        }
    }

    public async Task UpdateAsync(Lesson lesson, CancellationToken cancellationToken)
    {
        try
        {
            _dbContext.Lessons.Update(lesson);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lesson with ID {LessonId} was updated successfully", lesson.Id);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error occurred while updating lesson with ID {LessonId}", lesson.Id);
            throw new InvalidOperationException("An error occurred while updating the lesson in the database.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while updating lesson with ID {LessonId}", lesson.Id);
            throw new InvalidOperationException("An unexpected error occurred while updating the lesson.", ex);
        }
    }

    public async Task Delete(Lesson lesson, CancellationToken cancellationToken)
    {
        try
        {
            _dbContext.Lessons.Remove(lesson);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Lesson with ID {LessonId} was deleted successfully", lesson.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting lesson with ID {LessonId}", lesson.Id);
            throw new InvalidOperationException("An error occurred while deleting the lesson from the database.", ex);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Changes were saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while saving changes");
            throw new InvalidOperationException("An error occurred while saving changes to the database.", ex);
        }
    }

    public async Task<IEnumerable<Lesson>> GetAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Lesson> lessons = await _dbContext.Lessons
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Retrieved {LessonCount} lessons from the database", lessons.Count());
            return lessons;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving all lessons");
            throw new InvalidOperationException("An error occurred while retrieving all lessons from the database.", ex);
        }
    }
}

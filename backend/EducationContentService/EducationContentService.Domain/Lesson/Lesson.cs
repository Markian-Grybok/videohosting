using EducationContentService.Domain.Common.Models;
using EducationContentService.Domain.Lesson.ValueObjects;

namespace EducationContentService.Domain.Lesson;

public class Lesson : Entity<LessonId>
{
    public Title Title { get; private set; } = null!;
    public Description Description { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private Lesson(LessonId id, Title title, Description description)
        : base(id)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        IsDeleted = false;
        DeletedAt = null;
    }

    public static Lesson Create(LessonId id, Title title, Description description)
    {
        return new Lesson(id, title, description);
    }

    public void Update(Title title, Description description)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a deleted lesson.");
        }

        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("Lesson is already deleted.");
        }

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted)
        {
            throw new InvalidOperationException("Lesson is not deleted.");
        }

        IsDeleted = false;
        DeletedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }
}

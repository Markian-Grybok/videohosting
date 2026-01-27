using EducationContentService.Domain.Common.Models;

namespace EducationContentService.Domain.Lesson.ValueObjects;

public class LessonId : ValueObject
{
    public Guid Value { get; }

    private LessonId(Guid value)
    {
        Value = value;
    }

    public static LessonId NewId() => new LessonId(Guid.NewGuid());
    public static LessonId Create(Guid id) => new LessonId(id);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

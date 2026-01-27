using EducationContentService.Domain.Common.Models;

namespace EducationContentService.Domain.Lesson.ValueObjects;

public class Title : ValueObject
{
    public const int MAX_LENGTH = 100;
    public string Value { get; }

    private Title(string value)
    {
        Value = value;
    }

    public static Title Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Title cannot be null or empty.", nameof(value));
        }

        if (value.Length > MAX_LENGTH)
        {
            throw new ArgumentException($"Title cannot exceed {MAX_LENGTH} characters.", nameof(value));
        }

        return new Title(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

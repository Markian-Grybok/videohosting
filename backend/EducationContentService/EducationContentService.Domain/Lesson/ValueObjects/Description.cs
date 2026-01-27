using System.Text.RegularExpressions;
using EducationContentService.Domain.Common.Models;

namespace EducationContentService.Domain.Lesson.ValueObjects;

public class Description : ValueObject
{
    public const int MAX_LENGTH = 2000;
    public string Value { get; }

    private Description(string value)
    {
        Value = value;
    }

    public static Description Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(value));
        }

        string normalized = Regex.Replace(value.Trim(), @"\s+", " ");

        if (normalized.Length > MAX_LENGTH)
        {
            throw new ArgumentException($"Description cannot exceed {MAX_LENGTH} characters.", nameof(value));
        }

        return new Description(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}

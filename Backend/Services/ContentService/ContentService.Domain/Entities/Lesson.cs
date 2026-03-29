namespace ContentService.Domain.Entities;

public class Lesson
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    private Lesson() { }

    public static Lesson Create(string title, string description)
    {
        return new Lesson
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string description)
    {
        Title = title;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}

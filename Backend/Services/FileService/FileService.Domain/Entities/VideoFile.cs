using FileService.Domain.Enums;

namespace FileService.Domain.Entities;
public class VideoFile
{
    public Guid Id { get; private set; }
    public Guid LessonId { get; private set; }
    public string OriginalFileName { get; private set; } = null!;
    public string StoragePath { get; private set; } = null!;
    public string? HlsPath { get; private set; }
    public long SizeInBytes { get; private set; }
    public VideoFileStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private VideoFile() { }

    public static VideoFile Create(
        Guid lessonId,
        string originalFileName,
        string storagePath,
        long sizeInBytes)
    {
        return new VideoFile
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            OriginalFileName = originalFileName,
            StoragePath = storagePath,
            SizeInBytes = sizeInBytes,
            Status = VideoFileStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsProcessing()
    {
        Status = VideoFileStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsReady(string hlsPath)
    {
        HlsPath = hlsPath;
        Status = VideoFileStatus.Ready;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed()
    {
        Status = VideoFileStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}

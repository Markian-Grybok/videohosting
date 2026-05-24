namespace FileService.Common.Entities
{
    public class VideoFile
    {
        public Guid Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public VideoFileStatus Status { get; set; } = VideoFileStatus.Pending;
        public int? Progress { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? HlsManifestPath { get; set; }
        public string? ErrorMessage { get; set; }
        public string? AvailableQualities { get; set; }
    }
}

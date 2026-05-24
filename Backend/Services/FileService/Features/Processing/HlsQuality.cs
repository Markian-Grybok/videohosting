namespace FileService.Features.Processing;

public record HlsQuality(
    string Name,          // "360p", "480p", "720p", "1080p"
    string Resolution,    // "640x360", "854x480", "1280x720", "1920x1080"
    int VideoBitrate,     // kbps: 800, 1400, 2800, 5000
    int AudioBitrate,     // kbps: 96, 128, 128, 192
    int Height            // 360, 480, 720, 1080
);

public static class HlsQualities
{
    public static readonly IReadOnlyList<HlsQuality> All = new[]
    {
        new HlsQuality("360p",  "640x360",   800,  96,  360),
        new HlsQuality("480p",  "854x480",  1400, 128,  480),
        new HlsQuality("720p",  "1280x720", 2800, 128,  720),
        new HlsQuality("1080p", "1920x1080",5000, 192, 1080),
    };
}

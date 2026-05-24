using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using FileService.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Features.Processing
{
    public class VideoProcessor
    {
        private readonly IOptions<FfmpegOptions> _ffmpegOptions;
        private readonly ILogger<VideoProcessor> _logger;

        public VideoProcessor(IOptions<FfmpegOptions> ffmpegOptions, ILogger<VideoProcessor> logger)
        {
            _ffmpegOptions = ffmpegOptions;
            _logger = logger;
        }

        public async Task<string> ConvertToHlsAsync(
            string inputFilePath,
            string outputBaseDir,
            Func<int, string, Task>? onProgress,
            CancellationToken ct)
        {
            // Step 1: Get source video resolution using ffprobe
            var sourceHeight = await GetSourceHeightAsync(inputFilePath, ct);
            _logger.LogInformation("Source video height: {Height}px", sourceHeight);

            // Step 2: Get video duration for progress calculation
            var totalDuration = await GetVideoDurationAsync(inputFilePath, ct);
            _logger.LogInformation("Video duration: {Duration}", totalDuration?.ToString(@"hh\:mm\:ss\.ff") ?? "unknown");

            // Step 3: Filter qualities to only include those <= source height
            var selectedQualities = FilterQualitiesBySourceHeight(sourceHeight);
            _logger.LogInformation("Selected qualities: {Qualities}", 
                string.Join(", ", selectedQualities.Select(q => q.Name)));

            // Step 4: Process each quality sequentially with global progress tracking
            int totalQualities = selectedQualities.Count;
            int completedQualities = 0;

            foreach (var quality in selectedQualities)
            {
                await ProcessQualityAsync(
                    inputFilePath,
                    outputBaseDir,
                    quality,
                    totalDuration,
                    completedQualities,
                    totalQualities,
                    onProgress,
                    ct);
                
                completedQualities++;
            }

            // Step 5: Generate master.m3u8
            var masterPlaylistPath = await GenerateMasterPlaylistAsync(outputBaseDir, selectedQualities);
            _logger.LogInformation("Master playlist generated: {Path}", masterPlaylistPath);

            return masterPlaylistPath;
        }

        private async Task<TimeSpan?> GetVideoDurationAsync(string filePath, CancellationToken ct)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegOptions.Value.BinaryPath,
                    Arguments = $"-i \"{filePath}\"",
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                var output = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync(ct);

                var match = Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
                if (match.Success)
                {
                    var hours = int.Parse(match.Groups[1].Value);
                    var minutes = int.Parse(match.Groups[2].Value);
                    var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                    return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get video duration, will use fallback");
            }

            return null;
        }

        private async Task<int> GetSourceHeightAsync(string inputFilePath, CancellationToken ct)
        {
            try
            {
                var args = $"-v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 \"{inputFilePath}\"";
                var psi = new ProcessStartInfo("ffprobe", args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                var output = await proc.StandardOutput.ReadToEndAsync();
                await proc.WaitForExitAsync(ct);

                if (proc.ExitCode != 0)
                {
                    _logger.LogWarning("ffprobe failed, using fallback height 1080");
                    return 1080;
                }

                var parts = output.Trim().Split(',');
                if (parts.Length == 2 && int.TryParse(parts[1], out var height))
                {
                    return height;
                }

                _logger.LogWarning("Could not parse resolution from ffprobe output: {Output}", output);
                return 1080;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get source height, using fallback");
                return 1080;
            }
        }

        private List<HlsQuality> FilterQualitiesBySourceHeight(int sourceHeight)
        {
            var filtered = HlsQualities.All
                .Where(q => q.Height <= sourceHeight)
                .ToList();

            // Fallback: if no quality fits, use the lowest
            if (filtered.Count == 0)
            {
                filtered.Add(HlsQualities.All[0]);
            }

            return filtered;
        }

        private async Task ProcessQualityAsync(
            string inputFilePath,
            string outputBaseDir,
            HlsQuality quality,
            TimeSpan? totalDuration,
            int completedQualities,
            int totalQualities,
            Func<int, string, Task>? onProgress,
            CancellationToken ct)
        {
            var qualityDir = Path.Combine(outputBaseDir, quality.Name);
            Directory.CreateDirectory(qualityDir);

            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Processing quality {Quality} for {InputFile} ({Index}/{Total})", 
                quality.Name, inputFilePath, completedQualities + 1, totalQualities);

            var args = $"-i \"{inputFilePath}\" " +
                       $"-vf scale=-2:{quality.Height} " +

                       // Video encoding
                       $"-c:v libx264 " +
                       $"-b:v {quality.VideoBitrate}k " +

                       // Audio encoding
                       $"-c:a aac " +
                       $"-b:a {quality.AudioBitrate}k " +

                       // Synchronize keyframes
                       $"-g 180 " +
                       $"-keyint_min 180 " +
                       $"-sc_threshold 0 " +

                       // Force segment boundaries
                       $"-force_key_frames \"expr:gte(t,n_forced*6)\" " +

                       // HLS
                       $"-hls_time 6 " +
                       $"-hls_list_size 0 " +
                       $"-hls_segment_filename \"{qualityDir}/segment%d.ts\" " +
                       $"-f hls " +
                       $"\"{qualityDir}/index.m3u8\"";

            var psi = new ProcessStartInfo(_ffmpegOptions.Value.BinaryPath, args)
            {
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            
            // Read stderr continuously for progress updates
            int lastOverallPercent = 10;
            var regex = new Regex(@"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
            var lastUpdateTime = DateTime.MinValue;

            while (!proc.StandardError.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await proc.StandardError.ReadLineAsync();
                if (line == null) continue;

                _logger.LogDebug("FFmpeg [{Quality}]: {Line}", quality.Name, line);

                if (totalDuration.HasValue && onProgress != null)
                {
                    var match = regex.Match(line);
                    if (match.Success)
                    {
                        var hours = int.Parse(match.Groups[1].Value);
                        var minutes = int.Parse(match.Groups[2].Value);
                        var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                        var currentTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);

                        // Calculate progress for current quality (0-100)
                        var currentQualityProgress = (currentTime.TotalSeconds / totalDuration.Value.TotalSeconds) * 100;
                        currentQualityProgress = Math.Clamp(currentQualityProgress, 0, 100);

                        // Calculate global progress across all qualities (10-90 range)
                        var overallProgressRaw = 
                            (completedQualities + currentQualityProgress / 100.0) / totalQualities * 100;
                        
                        var overallProgress = 10 + (int)Math.Round(overallProgressRaw * 0.8);
                        overallProgress = Math.Clamp(overallProgress, 10, 90);

                        if (overallProgress != lastOverallPercent && (DateTime.UtcNow - lastUpdateTime).TotalMilliseconds > 500)
                        {
                            _logger.LogDebug("Progress [{Quality}] Current={Current}% Overall={Overall}%", 
                                quality.Name, (int)currentQualityProgress, overallProgress);
                            await onProgress(overallProgress, quality.Name);
                            lastOverallPercent = overallProgress;
                            lastUpdateTime = DateTime.UtcNow;
                        }
                    }
                }
            }

            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode != 0)
            {
                throw new ProcessingException(
                    $"FFmpeg failed for {quality.Name}");
            }

            stopwatch.Stop();

            // Calculate generated files size
            long totalBytes = Directory
                .GetFiles(
                    qualityDir,
                    "*",
                    SearchOption.AllDirectories)
                .Sum(file => new FileInfo(file).Length);

            double totalMb = totalBytes / (1024.0 * 1024.0);

            _logger.LogInformation(
                "Quality {Quality} completed. Time={Time}s Size={Size:F2}MB",
                quality.Name,
                stopwatch.Elapsed.TotalSeconds,
                totalMb);

            _logger.LogDebug("Quality {Quality} processing completed", quality.Name);
        }

        private async Task<string> GenerateMasterPlaylistAsync(
            string outputBaseDir,
            List<HlsQuality> selectedQualities)
        {
            var sb = new StringBuilder();
            sb.AppendLine("#EXTM3U");
            sb.AppendLine("#EXT-X-VERSION:3");
            sb.AppendLine();

            foreach (var q in selectedQualities)
            {
                sb.AppendLine(
                    $"#EXT-X-STREAM-INF:BANDWIDTH={q.VideoBitrate * 1000}," +
                    $"RESOLUTION={q.Resolution}," +
                    $"CODECS=\"avc1.42E01E,mp4a.40.2\"");
                sb.AppendLine($"{q.Name}/index.m3u8");
                sb.AppendLine();
            }

            var masterPlaylistPath = Path.Combine(outputBaseDir, "master.m3u8");
            await File.WriteAllTextAsync(masterPlaylistPath, sb.ToString());

            return masterPlaylistPath;
        }
    }
}

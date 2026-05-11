using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using FileService.Common.Entities;
using FileService.Infrastructure.Storage;
using FileService.Infrastructure.Persistence;
using FileService.Common.Exceptions;
using FileService.Features.Processing.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FileService.Features.Processing.Commands
{
    public class ProcessVideoCommandHandler : IRequestHandler<ProcessVideoCommand, ProcessVideoResult>
    {
        private readonly FileServiceDbContext _dbContext;
        private readonly IStorageService _storageService;
        private readonly IOptions<FfmpegOptions> _ffmpegOptions;
        private readonly IHubContext<ProcessingHub> _hubContext;
        private readonly ILogger<ProcessVideoCommandHandler> _logger;

        public ProcessVideoCommandHandler(
            FileServiceDbContext dbContext,
            IStorageService storageService,
            IOptions<FfmpegOptions> ffmpegOptions,
            IHubContext<ProcessingHub> hubContext,
            ILogger<ProcessVideoCommandHandler> logger)
        {
            _dbContext = dbContext;
            _storageService = storageService;
            _ffmpegOptions = ffmpegOptions;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<ProcessVideoResult> Handle(ProcessVideoCommand command, CancellationToken ct)
        {
            var fileId = command.FileId;
            var video = await _dbContext.VideoFiles.FindAsync(new object[] { fileId }, ct);
            if (video == null)
                throw new ProcessingException($"Video file not found: {fileId}");

            string tempInput = null;
            string tempOutputDir = null;
            try
            {
                video.Status = VideoFileStatus.Processing;
                video.Progress = 0;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 0);

                tempInput = Path.GetTempFileName();
                tempOutputDir = Path.Combine(Path.GetTempPath(), fileId.ToString());
                Directory.CreateDirectory(tempOutputDir);

                await _storageService.DownloadFileAsync(command.StoragePath, tempInput, ct);

                video.Progress = 20;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 20);

                // Отримуємо тривалість відео
                var totalDuration = await GetVideoDurationAsync(tempInput, ct);

                var ffmpeg = _ffmpegOptions.Value.BinaryPath;
                var segmentDuration = _ffmpegOptions.Value.SegmentDuration;
                var manifestPath = Path.Combine(tempOutputDir, "index.m3u8");
                var args = $"-i \"{tempInput}\" -codec: copy -start_number 0 -hls_time {segmentDuration} -hls_list_size 0 -f hls \"{manifestPath}\"";

                var psi = new ProcessStartInfo(ffmpeg, args)
                {
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);

                // Тільки одне читання stderr з трекінгом прогресу
                int lastPercent = 20;
                var regex = new Regex(@"time=(\d{2}):(\d{2}):(\d{2}\.\d{2})");
                var lastUpdateTime = DateTime.MinValue;

                while (!proc.StandardError.EndOfStream && !ct.IsCancellationRequested)
                {
                    var line = await proc.StandardError.ReadLineAsync();
                    if (line == null) continue;

                    _logger.LogDebug("FFmpeg: {Line}", line);

                    if (totalDuration.HasValue)
                    {
                        var match = regex.Match(line);
                        if (match.Success)
                        {
                            var hours = int.Parse(match.Groups[1].Value);
                            var minutes = int.Parse(match.Groups[2].Value);
                            var seconds = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
                            var currentTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);

                            var percentRaw = (currentTime.TotalSeconds / totalDuration.Value.TotalSeconds) * 70; // 20-90 це 70 діапазон
                            var percentInt = 20 + (int)Math.Round(percentRaw);
                            percentInt = Math.Clamp(percentInt, 20, 90);

                            if (percentInt != lastPercent && (DateTime.UtcNow - lastUpdateTime).TotalMilliseconds > 500)
                            {
                                video.Progress = percentInt;
                                await _dbContext.SaveChangesAsync(ct);
                                await NotifyHub(fileId, "Processing", percentInt);
                                lastPercent = percentInt;
                                lastUpdateTime = DateTime.UtcNow;
                            }
                        }
                    }
                }

                await proc.WaitForExitAsync(ct);

                if (proc.ExitCode != 0)
                    throw new ProcessingException($"FFmpeg failed with exit code {proc.ExitCode}");

                video.Progress = 90;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 90);

                var hlsPrefix = $"hls/{fileId}/";
                await _storageService.UploadDirectoryAsync(tempOutputDir, hlsPrefix, ct);

                video.Status = VideoFileStatus.Ready;
                video.Progress = 100;
                video.HlsManifestPath = $"{hlsPrefix}index.m3u8";
                video.ProcessedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Ready", 100);

                return new ProcessVideoResult(fileId, video.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video {FileId}", fileId);
                video.Status = VideoFileStatus.Failed;
                video.Progress = 0;
                video.ErrorMessage = ex.Message;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Failed", 0);
                throw;
            }
            finally
            {
                try { if (tempInput != null && File.Exists(tempInput)) File.Delete(tempInput); } catch { }
                try { if (tempOutputDir != null && Directory.Exists(tempOutputDir)) Directory.Delete(tempOutputDir, true); } catch { }
            }
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

            return null; // fallback
        }

        private async Task NotifyHub(Guid fileId, string status, int percent)
        {
            _logger.LogInformation("📡 Sending SignalR update: FileId={FileId}, Status={Status}, Progress={Percent}", fileId, status, percent);

            await _hubContext.Clients.Group($"file-{fileId}")
                .SendAsync("ProcessingUpdate", new { fileId, status, progressPercent = percent });
        }
    }
}
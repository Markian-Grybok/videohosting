using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
                await NotifyHub(fileId, "Processing", 5);

                tempInput = Path.GetTempFileName();
                tempOutputDir = Path.Combine(Path.GetTempPath(), fileId.ToString());
                Directory.CreateDirectory(tempOutputDir);

                await _storageService.DownloadFileAsync(command.StoragePath, tempInput, ct);
                
                video.Progress = 20;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 20);

                var ffmpeg = _ffmpegOptions.Value.BinaryPath;
                var segmentDuration = _ffmpegOptions.Value.SegmentDuration;
                var manifestPath = Path.Combine(tempOutputDir, "index.m3u8");
                var args = $"-i \"{tempInput}\" -codec: copy -start_number 0 -hls_time {segmentDuration} -hls_list_size 0 -f hls \"{manifestPath}\"";

                var psi = new ProcessStartInfo(ffmpeg, args)
                {
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                var stderr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync(ct);
                if (proc.ExitCode != 0)
                    throw new ProcessingException($"FFmpeg failed: {stderr}");

                video.Progress = 60;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 60);

                var hlsPrefix = $"hls/{fileId}/";
                await _storageService.UploadDirectoryAsync(tempOutputDir, hlsPrefix, ct);
                
                video.Progress = 90;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 90);

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

        private async Task NotifyHub(Guid fileId, string status, int percent)
        {
            await _hubContext.Clients.Group($"file-{fileId}")
                .SendAsync("ProcessingUpdate", new { fileId, status, progressPercent = percent });
        }
    }
}

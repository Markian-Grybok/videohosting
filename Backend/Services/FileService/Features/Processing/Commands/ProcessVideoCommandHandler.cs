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
using FileService.Features.Processing;
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
        private readonly VideoProcessor _videoProcessor;

        public ProcessVideoCommandHandler(
            FileServiceDbContext dbContext,
            IStorageService storageService,
            IOptions<FfmpegOptions> ffmpegOptions,
            IHubContext<ProcessingHub> hubContext,
            ILogger<ProcessVideoCommandHandler> logger,
            VideoProcessor videoProcessor)
        {
            _dbContext = dbContext;
            _storageService = storageService;
            _ffmpegOptions = ffmpegOptions;
            _hubContext = hubContext;
            _logger = logger;
            _videoProcessor = videoProcessor;
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
                video.Progress = 5;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 5);

                tempInput = Path.GetTempFileName();
                tempOutputDir = Path.Combine(Path.GetTempPath(), fileId.ToString());
                Directory.CreateDirectory(tempOutputDir);

                await _storageService.DownloadFileAsync(command.StoragePath, tempInput, ct);

                video.Progress = 10;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 10);

                // Convert to ABR HLS with dynamic progress callback
                var masterPlaylistRelativePath = await _videoProcessor.ConvertToHlsAsync(
                    tempInput,
                    tempOutputDir,
                    async (progress, qualityName) =>
                    {
                        video.Progress = progress;
                        await _dbContext.SaveChangesAsync(ct);

                        await NotifyHub(
                            fileId,
                            $"Processing {qualityName}",
                            progress
                        );
                    },
                    ct);
                
                // Extract selected qualities for storage
                var qualityDirs = Directory.GetDirectories(tempOutputDir)
                    .Select(d => Path.GetFileName(d))
                    .OrderBy(q => q)
                    .ToList();
                var selectedQualities = HlsQualities.All
                    .Where(q => qualityDirs.Contains(q.Name))
                    .ToList();

                var hlsPrefix = $"hls/{fileId}";
                await _storageService.UploadDirectoryAsync(tempOutputDir, hlsPrefix, ct);

                video.Status = VideoFileStatus.Ready;
                video.Progress = 90;
                video.HlsManifestPath = $"hls/{fileId}/master.m3u8";
                video.AvailableQualities = string.Join(",", selectedQualities.Select(q => q.Name));
                video.ProcessedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
                await NotifyHub(fileId, "Processing", 90);

                video.Progress = 100;
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

        private async Task NotifyHub(Guid fileId, string status, int percent)
        {
            _logger.LogInformation("📡 Sending SignalR update: FileId={FileId}, Status={Status}, Progress={Percent}", fileId, status, percent);

            await _hubContext.Clients.Group($"file-{fileId}")
                .SendAsync("ProcessingUpdate", new { fileId, status, progressPercent = percent });
        }
    }
}
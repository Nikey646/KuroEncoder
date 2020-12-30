using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimeMetadataCollector.Extensions;
using KuroEncoder.Classes;
using KuroEncoder.Extensions;
using KuroEncoder.Models;
using MediaInfo;
using MediaInfo.Model;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KuroEncoder.Services
{
    public class EncoderService : IHostedService
    {
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger<EncoderService> _logger;
        private readonly EncoderOptions _options;
        private List<Process> _processes;

        public EncoderService(IHostApplicationLifetime applicationLifetime, IOptions<EncoderOptions> options,
            ILogger<EncoderService> logger)
        {
            this._applicationLifetime = applicationLifetime;
            this._logger = logger;
            this._options = options.Value;

            this._processes = new List<Process>();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._applicationLifetime.ApplicationStarted.Register(() =>
                Task.Factory.StartNew(this.ExecuteAsync, cancellationToken));

            return Task.CompletedTask;
        }

        private async Task ExecuteAsync()
        {
            try
            {
                this._logger.Trace("Searching {folder} for *.[mkv|mp4|webm|avi|ogm] files.",
                    this._options.SourceFolder);

                var matcher = new Matcher();
                matcher.AddIncludePatterns(new[] {"*.mkv", "*.mp4", "*.webm", "*.avi", "*.ogm"});

                var files = matcher.GetResultsInFullPath(this._options.SourceFolder)
                    .Select(f => new FileInfo(f))
                    .ToList();

                this._logger.Trace("Found {count} files in {folder}", files.Count, this._options.SourceFolder);

                if (this._options.SingleFile)
                {
                    if (this._options.FileIndex >= files.Count)
                    {
                        this._logger.Fatal(
                            "Single file mode is active, but the provided file index ({fileIndex}) exceeds the amount of files found ({filesCount})",
                            this._options.FileIndex, files.Count);
                    }

                    var file = files[this._options.FileIndex];
                    files.Clear();
                    files.Add(file);
                }

                var outputFolder = this._options.OutputFolder;
                if (!Path.IsPathFullyQualified(this._options.OutputFolder))
                    outputFolder =
                        Path.GetFullPath(Path.Combine(this._options.SourceFolder, this._options.OutputFolder));

                if (!Directory.Exists(outputFolder))
                {
                    this._logger.Info("Creating output folder {folder}.", outputFolder);
                    Directory.CreateDirectory(outputFolder);
                }

                var ffmpeg = await FfmpegUtils.GetFfmpegAsync();

                foreach (var file in files)
                {
                    var mediaInfo = new MediaInfoWrapper(file.FullName);

                    var ratio = (Single) mediaInfo.Width / mediaInfo.Height;
                    var height = Math.Min(mediaInfo.Height, this._options.Resolution);
                    var width = Math.Ceiling(height * ratio);
                    if (width == 1279)
                        width++;

                    var scale = $"{width}x{height}";
                    var frames = Math.Ceiling(mediaInfo.BestVideoStream.Duration.TotalSeconds * mediaInfo.Framerate);

                    var outputFile = new FileInfo(Path.Combine(outputFolder, Path.ChangeExtension(file.Name, ".mkv")));

                    var videoFilter = $"-vf scale={scale}";
                    if (mediaInfo.Height <= this._options.Resolution)
                        videoFilter = String.Empty;

                    var audioTrack = this.FindBestAudioTrack(mediaInfo.AudioStreams.ToArray());
                    var audioMap = $"-map -0:a -map 0:a:{audioTrack}";

                    var subtitleMap = String.Empty;
                    if (mediaInfo.Subtitles.Count > 0)
                    {
                        var subtitleTrack = this.FindBestSubtitleTrack(mediaInfo.Subtitles.ToArray());
                        subtitleMap = $"-map -0:s -map 0:s:{subtitleTrack}";

                        if (this._options.SubtitleTrackId > -1)
                        {
                            subtitleMap = subtitleMap[..^1] + this._options.SubtitleTrackId;
                        }
                    }

                    var process = FfmpegUtils.StartFfmpeg(ffmpeg, mediaInfo.BestVideoStream.Duration, file.FullName,
                        this._options.Threads, audioMap, subtitleMap, this._options.Bitrate, videoFilter,
                        this._options.CRF, mediaInfo.BestVideoStream.StreamPosition, frames, outputFile.FullName);

                    this._logger.Debug("Starting ffmpeg with the following arguments: {arguments}",
                        process.StartInfo.Arguments);

                    this._processes.Add(process);

                    process.WaitForExit();

                    this._processes.Remove(process);
                    process.Dispose();
                }
            }
            catch (Exception crap)
            {
                this._logger.Fatal(crap, "There was an unexpected exception.");
            }
            finally
            {
                this._applicationLifetime.StopApplication();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var process in this._processes.Where(p => !p.HasExited))
            {
                process.Kill();
            }

            return Task.CompletedTask;
        }

        private Int32 FindBestAudioTrack(AudioStream[] audioStreams)
        {
            if (audioStreams.Length == 1)
                return 0;

            for (var i = 0; i < audioStreams.Length; i++)
            {
                if (audioStreams[i].Language == "Japanese")
                    return i;
            }

            // If we cannot find Japanese audio, and there are multiple, just return the default audio.
            return 0;
        }

        private Int32 FindBestSubtitleTrack(SubtitleStream[] subtitleStreams)
        {
            if (subtitleStreams.Length == 1)
                return 0;

            var possibleId = -1;

            for (var i = 0; i < subtitleStreams.Length; i++)
            {
                var stream = subtitleStreams[i];

                if (stream.Language != "English")
                    continue;

                if (stream.Name.Contains("Signs", StringComparison.OrdinalIgnoreCase) ||
                    stream.Name.Contains("Songs", StringComparison.OrdinalIgnoreCase))
                {
                    possibleId = i;
                    continue;
                }

                if (stream.Name.Contains("Full", StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            if (possibleId > 0)
                return possibleId;
            // If there was no subtitle stream that was possibly the full english subs, return default.
            return 0;
        }
    }
}

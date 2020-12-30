using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AnimeMetadataCollector.Extensions;
using Serilog;
using SharpCompress.Archives;

namespace KuroEncoder.Classes
{
    public class FfmpegUtils
    {
        private const String VersionEndpoint = "https://www.gyan.dev/ffmpeg/builds/git-version";
        private const String DownloadEndpoint = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";

        private static ILogger logger;

        static FfmpegUtils()
        {
            logger = Log.ForContext<FfmpegUtils>();
        }

        public static async Task<String> GetFfmpegAsync()
        {
            var http = new HttpClient();
            var ffmpegPath = Path.Combine(AppContext.BaseDirectory, ".tools", "ffmpeg.exe");
            var ffmpegArchivePath = Path.ChangeExtension(ffmpegPath, "7z");
            var ffmpegVersionPath = Path.ChangeExtension(ffmpegPath, "version");

            if (File.Exists(ffmpegPath) && File.Exists(ffmpegVersionPath))
            {
                logger.Verbose("Found existing ffmpeg at {toolsPath}", ffmpegPath);
                var version = File.ReadAllText(ffmpegVersionPath);
                var remoteVersion = await http.GetStringAsync(VersionEndpoint);

                if (String.Equals(version, remoteVersion, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Verbose("Existing ffmpeg is up to date");
                    return ffmpegPath;
                }

                logger.Warning("Existing ffmpeg is out of date");
            }

            if (!Directory.Exists(Path.GetDirectoryName(ffmpegPath)))
            {
                logger.Verbose("Creating .tools directory at: {baseDir}", AppContext.BaseDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(ffmpegPath));
            }

            logger.Warning("Downloading ffmpeg from gyan.dev");
            await using var fs = File.OpenWrite(ffmpegArchivePath);
            await using var stream = await http.GetStreamAsync(DownloadEndpoint);

            await stream.CopyToAsync(fs);
            await fs.DisposeAsync();

            logger.Warning("Extracting ffmpeg.7z");
            await using var readFs = File.OpenRead(ffmpegArchivePath);

            var archive = ArchiveFactory.Open(readFs);
            var archiveFile =
                archive.Entries.FirstOrDefault(e => e.Key.EndsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase));
            if (archiveFile == default)
                throw new Exception("Unable to extract ffmpeg.exe");

            await using var writeFs = File.OpenWrite(ffmpegPath);
            archiveFile.WriteTo(writeFs);

            archive.Dispose();
            await readFs.DisposeAsync();

            try
            {
                File.Delete(ffmpegArchivePath);
            }
            catch (Exception crap)
            {
                logger.Error(crap, "Failed to delete ffmoeg.7z");
            }

            logger.Verbose("Saving ffmpeg version");
            var newVersion = await http.GetStringAsync(VersionEndpoint);
            await File.WriteAllTextAsync(ffmpegVersionPath, newVersion);

            return ffmpegPath;
        }


        // TODO: FFMPEG Args
        public static Process StartFfmpeg(String ffmpeg, TimeSpan duration, String inputFile, Int32 ffmpegThreads,
            String audioMap, String subtitleMap, Int32 audioBitrate, String videoFilter, Single crf,
            Int32 videoStreamIndex, Double frames, String outputFile)
        {
            var startInfo = new ProcessStartInfo(ffmpeg);
            startInfo.Arguments =
                $"-i {inputFile.Quote()} -hide_banner -y -threads {ffmpegThreads} -map 0 {audioMap} {subtitleMap} -c:s copy -c:a aac -b:a {audioBitrate}k {videoFilter} -c:v libx265 -preset fast -crf {crf} -pix_fmt yuv420p -frames:{videoStreamIndex} {frames} {outputFile.Quote()}";
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardError = true;


            var process = Process.Start(startInfo);

            var lastPercentage = 0d;
            var progressBar = new ProgressBar("Encoding Progress: ");

            var encodeSpeed = "";

            progressBar.SetSuffixFunc(() => encodeSpeed);

            void ProgressReport(Object sender, DataReceivedEventArgs e)
            {
                if (e?.Data == null)
                    return;

                var chunks = e.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var time = chunks.FirstOrDefault(c => c.StartsWith("time="));

                var speed = 0d;

                for (var i = 0; i < chunks.Length; i++)
                {
                    var chunk = chunks[i];
                    if (chunk.StartsWith("speed="))
                    {
                        if (chunk.Length == 6)
                            chunk = chunks[i + 1];
                        else chunk = chunk.Substring(6);

                        speed = Double.Parse(chunk[..^1]);

                        break;
                    }
                }

                encodeSpeed = $" x{speed:0.00} ";

                if (time.IsEmpty())
                    return;

                var encodedTime = TimeSpan.Parse(time.Substring(5)).TotalSeconds;
                var totalTime = duration.TotalSeconds;

                var percentage = encodedTime / totalTime;
                if (percentage > lastPercentage)
                {
                    progressBar.Report(percentage);
                    // this._logger.Trace("{file} is currently {percentage}% encoded.", file.Name, percentage);
                    lastPercentage = percentage;
                }
            }

            void OnExited(Object sender, EventArgs e)
            {
                progressBar?.Dispose();
                if (process == null)
                    return;

                process.Exited -= OnExited;
                process.ErrorDataReceived -= ProgressReport;
            }

            process.Exited += OnExited;
            process.ErrorDataReceived += ProgressReport;
            process.BeginErrorReadLine();

            return process;
        }
    }
}

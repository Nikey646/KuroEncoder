using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
    }
}

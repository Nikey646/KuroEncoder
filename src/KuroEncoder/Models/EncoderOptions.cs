using System;
using System.Collections.Generic;
using System.IO;
using AnimeMetadataCollector.Extensions;
using KuroEncoder.Attributes;
using Microsoft.Extensions.Options;

namespace KuroEncoder.Models
{
    public class EncoderOptions : IValidateOptions<EncoderOptions>
    {

        [CliShortName("sf"), CliAlias("source")]
        public String SourceFolder { get; init; }

        [CliShortName("of"), CliAlias("output")]
        public String OutputFolder { get; init; }

        [CliShortName("sid")]
        public Int32 SubtitleTrackId { get; init; } = -1;

        [CliShortName("aid")]
        public Int32 AudioTrackId { get; init; } = -1;

        [CliShortName("r"), CliAlias("res")]
        public Int32 Resolution { get; init; } = 720;

        public Int32 Threads { get; init; } = 0;

        public Int32 Bitrate { get; init; } = 128;

        public Single CRF { get; init; } = 18;

        [CliAlias("single-file")]
        public Boolean SingleFile { get; init; }

        [CliAlias("file-index")]
        public Int32 FileIndex { get; init; } = -1;


        public ValidateOptionsResult Validate(String name, EncoderOptions opts)
        {
            var errors = new List<String>();

            if (opts.SourceFolder.IsEmpty())
                errors.Add("Source Folder should not be empty");
            else if (!Directory.Exists(opts.SourceFolder))
                errors.Add("Source Folder must be a valid path");

            if (opts.OutputFolder.IsEmpty())
                errors.Add("Output Folder should not be empty");

            if (opts.SingleFile && opts.FileIndex < 0)
                errors.Add("File Index must be provided when using Single File mode");

            return errors.Count > 0
                ? ValidateOptionsResult.Fail(errors)
                : ValidateOptionsResult.Success;
        }
    }
}

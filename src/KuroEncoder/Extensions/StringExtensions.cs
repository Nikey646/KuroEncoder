using System;

namespace AnimeMetadataCollector.Extensions
{
    public static class StringExtensions
    {
        public static String Quote(this String s) => $"\"{s}\"";

        public static Boolean IsEmpty(this String s) => String.IsNullOrWhiteSpace(s);
    }
}

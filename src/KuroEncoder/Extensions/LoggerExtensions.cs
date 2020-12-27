using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace KuroEncoder.Extensions
{
    public static class LoggerExtensions
    {
        [DebuggerStepThrough]
        public static void Fatal<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogCritical(message, args);

        [DebuggerStepThrough]
        public static void Fatal<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogCritical(exception, message, args);

        [DebuggerStepThrough]
        public static void Error<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogError(message, args);

        [DebuggerStepThrough]
        public static void Error<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogError(exception, message, args);

        [DebuggerStepThrough]
        public static void Warning<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogWarning(message, args);

        [DebuggerStepThrough]
        public static void Warning<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogWarning(exception, message, args);

        [DebuggerStepThrough]
        public static void Info<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogInformation(message, args);

        [DebuggerStepThrough]
        public static void Info<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogInformation(exception, message, args);

        [DebuggerStepThrough]
        public static void Debug<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogDebug(message, args);

        [DebuggerStepThrough]
        public static void Debug<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogDebug(exception, message, args);

        [DebuggerStepThrough]
        public static void Trace<T>(this ILogger<T> logger, String message, params Object[] args)
            => logger.LogTrace(message, args);

        [DebuggerStepThrough]
        public static void Trace<T>(this ILogger<T> logger, Exception exception, String message, params Object[] args)
            => logger.LogTrace(exception, message, args);
    }
}

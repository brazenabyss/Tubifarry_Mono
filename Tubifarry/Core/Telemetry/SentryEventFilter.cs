#if !MASTER_BRANCH
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using Sentry;

namespace Tubifarry.Core.Telemetry
{
    public static class SentryEventFilter
    {
        private static readonly HashSet<string> FilteredExceptionTypes = new(StringComparer.Ordinal)
        {
            "TaskCanceledException",
            "OperationCanceledException",
            "TimeoutException",
            "ThreadAbortException"
        };

        private static readonly HashSet<string> FilteredMessageParts = new(StringComparer.OrdinalIgnoreCase)
        {
            "connection refused",
            "network is unreachable",
            "download cancelled",
            "operation was canceled",
            "request aborted",
            "the operation has timed out",
            "connection reset",
            "connection closed",
            "broken pipe"
        };

        private static readonly HashSet<string> KnownTransientHttpErrors = new(StringComparer.OrdinalIgnoreCase)
        {
            "503",
            "502",
            "504",
            "429"
        };

        public static SentryEvent? FilterEvent(SentryEvent? sentryEvent, SentryHint hint)
        {
            if (sentryEvent == null)
                return null;

            var ex = sentryEvent.Exception;

            if (ex != null)
            {
                if (FilteredExceptionTypes.Contains(ex.GetType().Name))
                    return null;

                if (!string.IsNullOrEmpty(ex.Message) &&
                    FilteredMessageParts.Any(p => ex.Message.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    return null;

                if (ex is HttpRequestException httpEx && httpEx.StatusCode != null)
                {
                    var statusCode = ((int)httpEx.StatusCode).ToString();
                    if (KnownTransientHttpErrors.Contains(statusCode))
                        return null;
                }

                if (ex is SocketException socketEx)
                {
                    var socketError = socketEx.SocketErrorCode.ToString();
                    if (FilteredMessageParts.Any(p => socketError.Contains(p, StringComparison.OrdinalIgnoreCase)))
                        return null;
                }
            }

            EnrichFingerprint(sentryEvent, ex);

            return sentryEvent;
        }

        private static void EnrichFingerprint(SentryEvent sentryEvent, Exception? ex)
        {
            if (sentryEvent.Fingerprint.Any())
                return;

            var fingerprint = new List<string>();

            if (ex != null)
            {
                fingerprint.Add(ex.GetType().Name);

                var (operation, _) = ClassifyException(ex);
                if (operation != null)
                    fingerprint.Add(operation);
            }
            else
            {
                fingerprint.Add("no_exception");
            }

            fingerprint.Add(PluginInfo.Branch);
            sentryEvent.SetFingerprint(fingerprint);
        }

        private static (string? Operation, string Component) ClassifyException(Exception ex) => ex switch
        {
            HttpRequestException => ("http_request", "network"),
            SocketException => ("socket", "network"),
            TimeoutException => ("timeout", "general"),
            IOException => ("io", "filesystem"),
            UnauthorizedAccessException => ("unauthorized", "security"),
            ArgumentException => ("invalid_argument", "validation"),
            InvalidOperationException => ("invalid_operation", "logic"),
            TaskCanceledException => ("task_cancelled", "async"),
            AggregateException agg when agg.InnerExceptions.Count == 1 => ClassifyException(agg.InnerExceptions[0]),
            _ => (null, "unknown")
        };
    }
}
#endif

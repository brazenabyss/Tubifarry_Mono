#if !MASTER_BRANCH
namespace Tubifarry.Core.Telemetry
{
    public class SentryHelper : ISentryHelper
    {
        private readonly ISearchContextBuffer _contextBuffer;

        public SentryHelper(ISearchContextBuffer contextBuffer) => _contextBuffer = contextBuffer;

        public bool IsEnabled => TubifarrySentry.IsEnabled;

        public ISpan? StartSpan(string operation, string? description = null)
        {
            ISpan? parent = SentrySdk.GetSpan();
            if (parent == null)
                return null;

            ISpan span = parent.StartChild(operation, description ?? operation);
            span.SetTag("plugin", "tubifarry");
            span.SetTag("branch", PluginInfo.Branch);
            return span;
        }

        public void FinishSpan(ISpan? span, SpanStatus status = SpanStatus.Ok)
            => span?.Finish(status);

        public void FinishSpan(ISpan? span, Exception ex)
        {
            if (span == null)
                return;

            SpanStatus status = ex switch
            {
                TimeoutException => SpanStatus.DeadlineExceeded,
                OperationCanceledException => SpanStatus.Cancelled,
                UnauthorizedAccessException => SpanStatus.PermissionDenied,
                ArgumentException => SpanStatus.InvalidArgument,
                _ => SpanStatus.InternalError
            };

            span.Finish(ex, status);
        }

        public void SetSpanData(ISpan? span, string key, object? value)
        {
            if (span != null && value != null)
                span.SetExtra(key, value);
        }

        public void SetSpanTag(ISpan? span, string key, string value)
            => span?.SetTag(key, value);

        public void AddBreadcrumb(string? message, string? category = null)
        {
            if (!string.IsNullOrEmpty(message))
                SentrySdk.AddBreadcrumb(message, category);
        }

        public void CaptureException(Exception ex, string? message = null)
        {
            if (!string.IsNullOrEmpty(message))
                SentrySdk.CaptureException(ex, scope => scope.SetExtra("message", message));
            else
                SentrySdk.CaptureException(ex);
        }

        public void CaptureEvent(string message, string[] fingerprint, Dictionary<string, string>? tags = null, Dictionary<string, object>? extras = null, SentryLevel level = SentryLevel.Warning)
        {
            SentrySdk.CaptureEvent(new SentryEvent
            {
                Message = new SentryMessage { Formatted = message },
                Level = level,
                Fingerprint = fingerprint
            }, scope =>
            {
                if (tags != null)
                    foreach ((string? k, string? v) in tags)
                        scope.SetTag(k, v);
                if (extras != null)
                    foreach ((string? k, object? v) in extras)
                        scope.SetExtra(k, v);
            });
        }

        // Context buffer delegation
        public void LogSearch(string searchId, string query, string? artist, string? album, string strategy, int resultCount)
            => _contextBuffer.LogSearch(searchId, query, artist, album, strategy, resultCount);

        public void LogParseResult(string searchId, string folderPath, string regexMatchType, int fuzzyArtistScore, int fuzzyAlbumScore, int fuzzyArtistTokenSort, int fuzzyAlbumTokenSort, int priority, string codec, int bitrate, int bitDepth, int trackCountExpected, int trackCountActual, string username, bool hasFreeSlot, int queueLength, List<string>? directoryFiles, bool isInteractive)
            => _contextBuffer.LogParseResult(searchId, folderPath, regexMatchType, fuzzyArtistScore, fuzzyAlbumScore, fuzzyArtistTokenSort, fuzzyAlbumTokenSort, priority, codec, bitrate, bitDepth, trackCountExpected, trackCountActual, username, hasFreeSlot, queueLength, directoryFiles, isInteractive);

        public void LogGrab(string searchId, string downloadId, bool isInteractive)
            => _contextBuffer.LogGrab(searchId, downloadId, isInteractive);

        public SlskdBufferedContext? GetContext(string downloadId)
            => _contextBuffer.GetContext(downloadId);

        public SlskdBufferedContext? GetAndRemoveContext(string downloadId)
            => _contextBuffer.GetAndRemoveContext(downloadId);

        public void AddContextBreadcrumb(string downloadId, string message)
            => _contextBuffer.AddBreadcrumb(downloadId, message);

        public void RecordImport(string albumKey)
            => _contextBuffer.RecordImport(albumKey);

        public bool WasRecentlyImported(string albumKey, out int daysSinceImport)
            => _contextBuffer.WasRecentlyImported(albumKey, out daysSinceImport);
    }
}
#endif

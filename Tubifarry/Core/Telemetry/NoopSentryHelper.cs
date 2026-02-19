namespace Tubifarry.Core.Telemetry
{
    public class NoopSentryHelper : ISentryHelper
    {
        public bool IsEnabled => false;

        public ISpan? StartSpan(string operation, string? description = null) => null;

        public void FinishSpan(ISpan? span, SpanStatus status = SpanStatus.Ok) { }

        public void FinishSpan(ISpan? span, Exception ex) { }

        public void SetSpanData(ISpan? span, string key, object? value) { }

        public void SetSpanTag(ISpan? span, string key, string value) { }

        public void AddBreadcrumb(string? message, string? category = null) { }

        public void CaptureException(Exception ex, string? message = null) { }

        public void CaptureEvent(string message, string[] fingerprint, Dictionary<string, string>? tags = null, Dictionary<string, object>? extras = null, SentryLevel level = SentryLevel.Warning) { }

        public void LogSearch(string searchId, string query, string? artist, string? album, string strategy, int resultCount) { }

        public void LogParseResult(string searchId, string folderPath, string regexMatchType, int fuzzyArtistScore, int fuzzyAlbumScore, int fuzzyArtistTokenSort, int fuzzyAlbumTokenSort, int priority, string codec, int bitrate, int bitDepth, int trackCountExpected, int trackCountActual, string username, bool hasFreeSlot, int queueLength, List<string>? directoryFiles, bool isInteractive) { }

        public void LogGrab(string searchId, string downloadId, bool isInteractive) { }

        public SlskdBufferedContext? GetContext(string downloadId) => null;

        public SlskdBufferedContext? GetAndRemoveContext(string downloadId) => null;

        public void AddContextBreadcrumb(string downloadId, string message) { }

        public void RecordImport(string albumKey) { }

        public bool WasRecentlyImported(string albumKey, out int daysSinceImport) { daysSinceImport = 0; return false; }
    }
}

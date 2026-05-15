namespace Tubifarry.Core.Telemetry
{
    public interface ISentryHelper
    {
        bool IsEnabled { get; }

        // Span operations
        ISpan? StartSpan(string operation, string? description = null);
        void FinishSpan(ISpan? span, SpanStatus status = SpanStatus.Ok);
        void FinishSpan(ISpan? span, Exception ex);
        void SetSpanData(ISpan? span, string key, object? value);
        void SetSpanTag(ISpan? span, string key, string value);

        // Event operations
        void AddBreadcrumb(string? message, string? category = null);
        void CaptureException(Exception ex, string? message = null);
        void CaptureEvent(string message, string[] fingerprint, Dictionary<string, string>? tags = null, Dictionary<string, object>? extras = null, SentryLevel level = SentryLevel.Warning);

        // Context buffer operations (routed to ISearchContextBuffer)
        void LogSearch(string searchId, string query, string? artist, string? album, string strategy, int resultCount);
        void LogSearchSettings(string searchId, int trackCountFilter, bool normalizedSearch, bool appendYear, bool handleVolumeVariations, bool useFallbackSearch, bool useTrackFallback, int minimumResults, bool hasTemplates);
        void LogExpectedTracks(string searchId, List<string> trackNames, int expectedCount);
        void LogParseResult(string searchId, string folderPath, string regexMatchType, int fuzzyArtistScore, int fuzzyAlbumScore, int fuzzyArtistTokenSort, int fuzzyAlbumTokenSort, int priority, string codec, int bitrate, int bitDepth, int trackCountExpected, int trackCountActual, string username, bool hasFreeSlot, int queueLength, List<string>? directoryFiles, bool isInteractive);
        void UpdateSearchResultCount(string searchId, int actualResultCount);
        void LogGrab(string searchId, string downloadId, bool isInteractive);
        SlskdBufferedContext? GetContext(string downloadId);
        SlskdBufferedContext? GetAndRemoveContext(string downloadId);
        void AddContextBreadcrumb(string downloadId, string message);
        void RecordImport(string albumKey);
        bool WasRecentlyImported(string albumKey, out int daysSinceImport);
    }
}

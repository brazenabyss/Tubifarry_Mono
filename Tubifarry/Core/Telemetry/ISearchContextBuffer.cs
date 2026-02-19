namespace Tubifarry.Core.Telemetry
{
    public interface ISearchContextBuffer
    {
        void LogSearch(string searchId, string query, string? artist, string? album, string strategy, int resultCount);

        void LogParseResult(string searchId, string folderPath, string regexMatchType, int fuzzyArtistScore, int fuzzyAlbumScore, int fuzzyArtistTokenSort, int fuzzyAlbumTokenSort, int priority, string codec, int bitrate, int bitDepth, int trackCountExpected, int trackCountActual, string username, bool hasFreeSlot, int queueLength, List<string>? directoryFiles, bool isInteractive);

        void LogGrab(string searchId, string downloadId, bool isInteractive);

        SlskdBufferedContext? GetContext(string downloadId);

        SlskdBufferedContext? GetAndRemoveContext(string downloadId);

        void AddBreadcrumb(string downloadId, string message);

        void RecordImport(string albumKey);

        bool WasRecentlyImported(string albumKey, out int daysSinceImport);
    }
}

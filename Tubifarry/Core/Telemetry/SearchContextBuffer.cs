#if !MASTER_BRANCH
using System.Collections.Concurrent;

namespace Tubifarry.Core.Telemetry
{
    public class SearchContextBuffer : ISearchContextBuffer
    {
        private readonly ConcurrentDictionary<string, SlskdBufferedContext> _contextBySearchId = new();
        private readonly ConcurrentDictionary<string, SlskdBufferedContext> _contextByDownloadId = new();
        private readonly ConcurrentDictionary<string, DateTime> _recentImports = new();
        private static readonly TimeSpan ContextExpiry = TimeSpan.FromHours(1);
        private static readonly TimeSpan ImportTrackingExpiry = TimeSpan.FromDays(8);
        private DateTime _lastCleanup = DateTime.UtcNow;

        public void LogSearch(string searchId, string query, string? artist, string? album, string strategy, int resultCount)
        {
            CleanupIfNeeded();

            SlskdBufferedContext context = new()
            {
                SearchId = searchId,
                SearchQuery = query,
                Artist = artist,
                Album = album,
                Strategy = strategy,
                TotalResults = resultCount
            };
            context.Breadcrumbs.Add($"Search: '{query}' via {strategy} → {resultCount} results");

            _contextBySearchId[searchId] = context;
        }

        public void LogParseResult(
            string searchId,
            string folderPath,
            string regexMatchType,
            int fuzzyArtistScore,
            int fuzzyAlbumScore,
            int fuzzyArtistTokenSort,
            int fuzzyAlbumTokenSort,
            int priority,
            string codec,
            int bitrate,
            int bitDepth,
            int trackCountExpected,
            int trackCountActual,
            string username,
            bool hasFreeSlot,
            int queueLength,
            List<string>? directoryFiles,
            bool isInteractive)
        {
            if (!_contextBySearchId.TryGetValue(searchId, out SlskdBufferedContext? context))
            {
                context = new SlskdBufferedContext { SearchId = searchId, CreatedAt = DateTime.UtcNow };
                _contextBySearchId[searchId] = context;
            }

            context.FolderPath = folderPath;
            context.RegexMatchType = regexMatchType;
            context.FuzzyArtistScore = fuzzyArtistScore;
            context.FuzzyAlbumScore = fuzzyAlbumScore;
            context.FuzzyArtistTokenSort = fuzzyArtistTokenSort;
            context.FuzzyAlbumTokenSort = fuzzyAlbumTokenSort;
            context.Priority = priority;
            context.Codec = codec;
            context.Bitrate = bitrate;
            context.BitDepth = bitDepth;
            context.TrackCountExpected = trackCountExpected;
            context.TrackCountActual = trackCountActual;
            context.Username = username;
            context.HasFreeSlot = hasFreeSlot;
            context.QueueLength = queueLength;
            context.DirectoryFiles = directoryFiles;
            context.IsInteractive = isInteractive;

            context.Breadcrumbs.Add($"Parse: dir='{folderPath}' regex={regexMatchType} fuzzy_artist={fuzzyArtistScore} fuzzy_album={fuzzyAlbumScore} priority={priority}");
        }

        public void LogGrab(string searchId, string downloadId, bool isInteractive)
        {
            if (_contextBySearchId.TryRemove(searchId, out SlskdBufferedContext? context))
            {
                context.DownloadId = downloadId;
                context.IsInteractive = isInteractive;
                context.Breadcrumbs.Add($"Grab: {(isInteractive ? "interactive" : "auto-selected")}, downloadId={downloadId}");

                _contextByDownloadId[downloadId] = context;
            }
        }

        public SlskdBufferedContext? GetContext(string downloadId)
        {
            _contextByDownloadId.TryGetValue(downloadId, out SlskdBufferedContext? context);
            return context;
        }

        public SlskdBufferedContext? GetAndRemoveContext(string downloadId)
        {
            if (_contextByDownloadId.TryRemove(downloadId, out SlskdBufferedContext? context))
                return context;
            return null;
        }

        public void AddBreadcrumb(string downloadId, string message)
        {
            if (_contextByDownloadId.TryGetValue(downloadId, out SlskdBufferedContext? context))
                context.Breadcrumbs.Add(message);
        }

        public void RecordImport(string albumKey)
        {
            CleanupIfNeeded();
            _recentImports[albumKey] = DateTime.UtcNow;
        }

        public bool WasRecentlyImported(string albumKey, out int daysSinceImport)
        {
            daysSinceImport = 0;
            if (_recentImports.TryGetValue(albumKey, out DateTime importTime))
            {
                TimeSpan elapsed = DateTime.UtcNow - importTime;
                if (elapsed.TotalDays <= 7)
                {
                    daysSinceImport = (int)Math.Ceiling(elapsed.TotalDays);
                    return true;
                }
            }
            return false;
        }

        private void CleanupIfNeeded()
        {
            if (DateTime.UtcNow - _lastCleanup < TimeSpan.FromMinutes(10))
                return;

            _lastCleanup = DateTime.UtcNow;
            DateTime now = DateTime.UtcNow;

            List<string> expiredSearchIds = _contextBySearchId
                .Where(kvp => now - kvp.Value.CreatedAt > ContextExpiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in expiredSearchIds)
                _contextBySearchId.TryRemove(key, out _);

            List<string> expiredDownloadIds = _contextByDownloadId
                .Where(kvp => now - kvp.Value.CreatedAt > ContextExpiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in expiredDownloadIds)
                _contextByDownloadId.TryRemove(key, out _);

            List<string> expiredImports = _recentImports
                .Where(kvp => now - kvp.Value > ImportTrackingExpiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (string key in expiredImports)
                _recentImports.TryRemove(key, out _);
        }
    }
}
#endif

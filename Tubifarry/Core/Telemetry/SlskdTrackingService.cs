#if !MASTER_BRANCH
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser.Model;

namespace Tubifarry.Core.Telemetry
{
    public class SlskdTrackingService(ISentryHelper sentry, Logger logger) :
        IHandle<AlbumImportIncompleteEvent>,
        IHandle<AlbumGrabbedEvent>,
        IHandle<TrackImportedEvent>,
        IHandle<DownloadFailedEvent>
    {
        private readonly ISentryHelper _sentry = sentry;
        private readonly Logger _logger = logger;

        public void Handle(AlbumGrabbedEvent message)
        {
            if (!_sentry.IsEnabled)
                return;

            string? downloadId = message.DownloadId;
            if (string.IsNullOrEmpty(downloadId))
                return;

            string? infoUrl = message.Album?.Release?.InfoUrl;
            if (string.IsNullOrEmpty(infoUrl))
                return;

            string? searchId = ExtractSearchIdFromInfoUrl(infoUrl);
            if (string.IsNullOrEmpty(searchId))
                return;

            bool isInteractive = message.Album?.ReleaseSource == ReleaseSourceType.InteractiveSearch;
            _sentry.LogGrab(searchId, downloadId, isInteractive);

            _logger.Trace($"Linked search {searchId} to download {downloadId}");
        }

        public void Handle(AlbumImportIncompleteEvent message)
        {
            if (!_sentry.IsEnabled)
                return;

            TrackedDownload trackedDownload = message.TrackedDownload;

            if (trackedDownload.State != TrackedDownloadState.ImportFailed)
                return;

            if (!IsSlskdDownload(trackedDownload))
                return;

            string? downloadId = trackedDownload.DownloadItem?.DownloadId;
            if (string.IsNullOrEmpty(downloadId))
                return;

            SlskdSentryEvents.ImportFailureReason failureReason = DetermineFailureReason(trackedDownload);
            List<string> statusMessages = ExtractStatusMessages(trackedDownload);

            SlskdBufferedContext? context = _sentry.GetAndRemoveContext(downloadId);

            SlskdSentryEvents.EmitImportFailed(_sentry, failureReason, context, statusMessages);

            _logger.Debug($"Tracked import failure for download {downloadId}: {failureReason}");
        }

        public void Handle(TrackImportedEvent message)
        {
            if (!_sentry.IsEnabled)
                return;

            string? downloadId = message.DownloadId;
            if (string.IsNullOrEmpty(downloadId))
                return;

            SlskdBufferedContext? context = _sentry.GetContext(downloadId);

            if (context == null)
                return;

            bool hadReplacement = false;

            // Check for replacement (old files exist)
            if (message.OldFiles?.Any() == true)
            {
                string albumKey = GetAlbumKey(message);

                if (_sentry.WasRecentlyImported(albumKey, out int daysSinceImport))
                {
                    hadReplacement = true;
                    string replacementSource = DetermineReplacementSource(message);

                    SlskdSentryEvents.EmitUserReplaced(
                        _sentry,
                        daysSinceImport,
                        context,
                        replacementSource,
                        message.TrackInfo?.Artist?.Name,
                        message.TrackInfo?.Album?.Title);

                    _logger.Debug($"Tracked user replacement after {daysSinceImport} days");
                }
            }

            if (!hadReplacement)
            {
                SlskdSentryEvents.EmitImportSuccess(_sentry, context);
                _logger.Debug($"Tracked import success for download {downloadId}");
            }

            string key = GetAlbumKey(message);
            _sentry.RecordImport(key);
            _sentry.GetAndRemoveContext(downloadId);
        }

        public void Handle(DownloadFailedEvent message)
        {
            if (!_sentry.IsEnabled)
                return;

            string? downloadId = message.DownloadId;
            if (string.IsNullOrEmpty(downloadId))
                return;

            SlskdBufferedContext? context = _sentry.GetAndRemoveContext(downloadId);
            SlskdSentryEvents.DownloadFailureReason errorType = SlskdSentryEvents.CategorizeDownloadError(message.Message);

            SlskdSentryEvents.EmitDownloadFailed(_sentry, errorType, context, message.Message);

            _logger.Debug($"Tracked download failure for {downloadId}: {errorType}");
        }

        private static bool IsSlskdDownload(TrackedDownload trackedDownload)
        {
            string? indexer = trackedDownload.Indexer;
            if (string.IsNullOrEmpty(indexer))
                return false;

            return indexer.Contains("slskd", StringComparison.OrdinalIgnoreCase) ||
                   indexer.Contains("soulseek", StringComparison.OrdinalIgnoreCase);
        }

        private static SlskdSentryEvents.ImportFailureReason DetermineFailureReason(TrackedDownload trackedDownload)
        {
            bool hasMissingTracks = trackedDownload.StatusMessages
                .Any(sm => sm.Messages.Any(m => m.Contains("Has missing tracks", StringComparison.OrdinalIgnoreCase)));

            bool hasUnmatchedTracks = trackedDownload.StatusMessages
                .Any(sm => sm.Messages.Any(m => m.Contains("Has unmatched tracks", StringComparison.OrdinalIgnoreCase)));

            bool hasAlbumMatchNotClose = trackedDownload.StatusMessages
                .Any(sm => sm.Messages.Any(m => m.Contains("Album match is not close enough", StringComparison.OrdinalIgnoreCase)));

            if (hasAlbumMatchNotClose)
                return SlskdSentryEvents.ImportFailureReason.AlbumMatchNotClose;

            return (hasMissingTracks, hasUnmatchedTracks) switch
            {
                (true, true) => SlskdSentryEvents.ImportFailureReason.MixedTrackIssues,
                (true, false) => SlskdSentryEvents.ImportFailureReason.MissingTracks,
                (false, true) => SlskdSentryEvents.ImportFailureReason.UnmatchedTracks,
                _ => SlskdSentryEvents.ImportFailureReason.Unknown
            };
        }

        private static List<string> ExtractStatusMessages(TrackedDownload trackedDownload) => [.. trackedDownload.StatusMessages
                .SelectMany(sm => sm.Messages)
                .Where(m => !string.IsNullOrEmpty(m))];

        private static string? ExtractSearchIdFromInfoUrl(string infoUrl)
        {
            int searchesIndex = infoUrl.LastIndexOf("/searches/", StringComparison.OrdinalIgnoreCase);
            if (searchesIndex >= 0)
                return infoUrl[(searchesIndex + "/searches/".Length)..];
            return null;
        }

        private static string GetAlbumKey(TrackImportedEvent message) => $"{message.TrackInfo?.Artist?.Id ?? 0}-{message.TrackInfo?.Album?.Id ?? 0}";

        private static string DetermineReplacementSource(TrackImportedEvent message)
        {
            string? downloadClient = message.DownloadClientInfo?.Name;
            if (string.IsNullOrEmpty(downloadClient))
                return "other";

            if (downloadClient.Contains("slskd", StringComparison.OrdinalIgnoreCase) ||
                downloadClient.Contains("soulseek", StringComparison.OrdinalIgnoreCase))
                return "slskd";

            if (downloadClient.Contains("youtube", StringComparison.OrdinalIgnoreCase))
                return "youtube";

            return "other";
        }
    }
}
#endif

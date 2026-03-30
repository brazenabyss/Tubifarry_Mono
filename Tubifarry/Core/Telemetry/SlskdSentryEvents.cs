#if !MASTER_BRANCH
namespace Tubifarry.Core.Telemetry
{
    public static class SlskdSentryEvents
    {
        public enum ImportFailureReason
        {
            MissingTracks,
            UnmatchedTracks,
            AlbumMatchNotClose,
            MixedTrackIssues,
            Unknown
        }

        public enum DownloadFailureReason
        {
            Timeout,
            Cancelled,
            UserOffline,
            PermissionDenied,
            Unknown
        }

        public static void EmitImportFailed(
            ISentryHelper sentry,
            ImportFailureReason failureReason,
            SlskdBufferedContext? context,
            List<string>? statusMessages = null)
        {
            if (!sentry.IsEnabled)
                return;

            string reasonStr = failureReason switch
            {
                ImportFailureReason.MissingTracks => "missing_tracks",
                ImportFailureReason.UnmatchedTracks => "unmatched_tracks",
                ImportFailureReason.AlbumMatchNotClose => "album_match_not_close",
                ImportFailureReason.MixedTrackIssues => "mixed_track_issues",
                _ => "unknown"
            };

            string[] fingerprint = ["slskd-import-failed", reasonStr];

            Dictionary<string, string> tags = new()
            {
                ["slskd.failure_reason"] = reasonStr,
                ["slskd.regex_match"] = context?.RegexMatchType ?? "unknown",
                ["slskd.interactive"] = (context?.IsInteractive ?? false).ToString().ToLower(),
                ["slskd.codec"] = context?.Codec ?? "unknown",
                ["slskd.has_free_slot"] = (context?.HasFreeSlot ?? false).ToString().ToLower(),

                ["slskd.fuzzy_artist_weak"] = ((context?.FuzzyArtistScore ?? 100) < 90).ToString().ToLower(),
                ["slskd.fuzzy_album_weak"] = ((context?.FuzzyAlbumScore ?? 100) < 90).ToString().ToLower(),
                ["slskd.track_count_mismatch"] = ((context?.TrackCountExpected ?? 0) != (context?.TrackCountActual ?? 0)).ToString().ToLower(),
                ["slskd.had_regex_match"] = (context?.RegexMatchType != "none").ToString().ToLower(),
                ["slskd.multiple_candidates"] = ((context?.AllCandidates?.Count ?? 0) > 1).ToString().ToLower(),
                ["slskd.grabbed_top_candidate"] = (context?.LidarrUsedOurTop ?? false).ToString().ToLower()
            };

            Dictionary<string, object> extras = new()
            {
                ["search_query"] = context?.SearchQuery ?? "",
                ["folder_path"] = context?.FolderPath ?? "",
                ["fuzzy_artist_score"] = context?.FuzzyArtistScore ?? 0,
                ["fuzzy_album_score"] = context?.FuzzyAlbumScore ?? 0,
                ["fuzzy_artist_token"] = context?.FuzzyArtistTokenSort ?? 0,
                ["fuzzy_album_token"] = context?.FuzzyAlbumTokenSort ?? 0,
                ["priority"] = context?.Priority ?? 0,
                ["track_count_expected"] = context?.TrackCountExpected ?? 0,
                ["track_count_actual"] = context?.TrackCountActual ?? 0,
                ["username"] = context?.Username ?? "",
                ["queue_length"] = context?.QueueLength ?? 0,
                ["search_strategy"] = context?.Strategy ?? "",
                ["bitrate"] = context?.Bitrate ?? 0,
                ["bit_depth"] = context?.BitDepth ?? 0,

                ["expected_tracks"] = context?.ExpectedTracks ?? new List<string>(),
                ["expected_track_count"] = context?.ExpectedTrackCount ?? 0,

                ["our_top_priority"] = context?.OurTopPriority ?? 0,
                ["grabbed_priority"] = context?.GrabbedPriority ?? 0,
                ["grabbed_was_top_priority"] = context?.LidarrUsedOurTop ?? false,
                ["candidate_count"] = context?.AllCandidates?.Count ?? 0,

                ["settings.track_count_filter"] = context?.SettingsTrackCountFilter ?? -1,
                ["settings.normalized_search"] = context?.SettingsNormalizedSearch ?? false,
                ["settings.append_year"] = context?.SettingsAppendYear ?? false,
                ["settings.volume_variations"] = context?.SettingsHandleVolumeVariations ?? false,
                ["settings.fallback_search"] = context?.SettingsUseFallbackSearch ?? false,
                ["settings.track_fallback"] = context?.SettingsUseTrackFallback ?? false,
                ["settings.minimum_results"] = context?.SettingsMinimumResults ?? 0,
                ["settings.has_templates"] = context?.SettingsHasTemplates ?? false
            };

            if (statusMessages != null && statusMessages.Count > 0)
                extras["status_messages"] = statusMessages;

            if (context?.DirectoryFiles != null && context.DirectoryFiles.Count > 0)
                extras["directory_listing"] = context.DirectoryFiles;

            if (context?.AllCandidates != null && context.AllCandidates.Count > 0)
            {
                extras["candidates"] = context.AllCandidates
                    .OrderByDescending(c => c.Priority)
                    .Select(c => new
                    {
                        folder = c.FolderName,
                        regex = c.RegexMatchType,
                        fuzzy_artist = c.FuzzyArtist,
                        fuzzy_album = c.FuzzyAlbum,
                        priority = c.Priority,
                        tracks = c.TrackCount,
                        codec = c.Codec,
                        username = c.Username,
                        grabbed = c.WasGrabbed
                    }).ToList();
            }

            if (context?.Breadcrumbs != null)
            {
                foreach (string breadcrumb in context.Breadcrumbs)
                    sentry.AddBreadcrumb(breadcrumb, "slskd");
            }
            sentry.AddBreadcrumb($"Import Failed: {reasonStr}", "slskd");

            string message = $"slskd import failed: {reasonStr} for '{context?.Artist ?? "unknown"} - {context?.Album ?? "unknown"}'";
            sentry.CaptureEvent(message, fingerprint, tags, extras, SentryLevel.Warning);
        }

        public static void EmitImportSuccess(
            ISentryHelper sentry,
            SlskdBufferedContext? context)
        {
            if (!sentry.IsEnabled)
                return;

            string[] fingerprint = ["slskd-import-success"];

            Dictionary<string, string> tags = new()
            {
                ["slskd.regex_match"] = context?.RegexMatchType ?? "unknown",
                ["slskd.interactive"] = (context?.IsInteractive ?? false).ToString().ToLower(),
                ["slskd.codec"] = context?.Codec ?? "unknown",
                ["slskd.has_free_slot"] = (context?.HasFreeSlot ?? false).ToString().ToLower(),
                ["slskd.grabbed_top_candidate"] = (context?.LidarrUsedOurTop ?? false).ToString().ToLower(),
                ["slskd.track_count_filter"] = context?.SettingsTrackCountFilter?.ToString() ?? "unknown"
            };

            Dictionary<string, object> extras = new()
            {
                ["search_query"] = context?.SearchQuery ?? "",
                ["folder_path"] = context?.FolderPath ?? "",
                ["fuzzy_artist_score"] = context?.FuzzyArtistScore ?? 0,
                ["fuzzy_album_score"] = context?.FuzzyAlbumScore ?? 0,
                ["priority"] = context?.Priority ?? 0,
                ["track_count_expected"] = context?.TrackCountExpected ?? 0,
                ["track_count_actual"] = context?.TrackCountActual ?? 0,
                ["candidate_count"] = context?.AllCandidates?.Count ?? 0,
                ["grabbed_was_top_priority"] = context?.LidarrUsedOurTop ?? false,

                ["settings.track_count_filter"] = context?.SettingsTrackCountFilter ?? -1,
                ["settings.normalized_search"] = context?.SettingsNormalizedSearch ?? false,
                ["settings.append_year"] = context?.SettingsAppendYear ?? false,
                ["settings.volume_variations"] = context?.SettingsHandleVolumeVariations ?? false,
                ["settings.fallback_search"] = context?.SettingsUseFallbackSearch ?? false,
                ["settings.track_fallback"] = context?.SettingsUseTrackFallback ?? false,
                ["settings.minimum_results"] = context?.SettingsMinimumResults ?? 0,
                ["settings.has_templates"] = context?.SettingsHasTemplates ?? false
            };

            if (context?.Breadcrumbs != null)
            {
                foreach (string breadcrumb in context.Breadcrumbs)
                    sentry.AddBreadcrumb(breadcrumb, "slskd");
            }
            sentry.AddBreadcrumb("Import Success", "slskd");

            string message = $"slskd import success: '{context?.Artist ?? "unknown"} - {context?.Album ?? "unknown"}'";
            sentry.CaptureEvent(message, fingerprint, tags, extras, SentryLevel.Info);
        }

        public static void EmitUserReplaced(
            ISentryHelper sentry,
            int daysUntilReplaced,
            SlskdBufferedContext? originalContext,
            string replacementSource,
            string? replacementArtist = null,
            string? replacementAlbum = null)
        {
            if (!sentry.IsEnabled)
                return;

            string[] fingerprint = ["slskd-user-replaced"];

            Dictionary<string, string> tags = new()
            {
                ["slskd.days_until_replaced"] = daysUntilReplaced.ToString(),
                ["slskd.interactive"] = (originalContext?.IsInteractive ?? false).ToString().ToLower(),
                ["slskd.original_codec"] = originalContext?.Codec ?? "unknown",
                ["slskd.replacement_source"] = replacementSource
            };

            Dictionary<string, object> extras = new()
            {
                ["original_folder_path"] = originalContext?.FolderPath ?? "",
                ["original_search_query"] = originalContext?.SearchQuery ?? "",
                ["original_fuzzy_artist"] = originalContext?.FuzzyArtistScore ?? 0,
                ["original_fuzzy_album"] = originalContext?.FuzzyAlbumScore ?? 0,
                ["replacement_artist"] = replacementArtist ?? "",
                ["replacement_album"] = replacementAlbum ?? ""
            };

            string message = $"slskd user replaced album after {daysUntilReplaced} days";
            sentry.CaptureEvent(message, fingerprint, tags, extras, SentryLevel.Info);
        }

        public static void EmitDownloadFailed(
            ISentryHelper sentry,
            DownloadFailureReason errorType,
            SlskdBufferedContext? context,
            string? errorMessage = null,
            int retryCount = 0)
        {
            if (!sentry.IsEnabled)
                return;

            string errorStr = errorType switch
            {
                DownloadFailureReason.Timeout => "timeout",
                DownloadFailureReason.Cancelled => "cancelled",
                DownloadFailureReason.UserOffline => "user_offline",
                DownloadFailureReason.PermissionDenied => "permission_denied",
                _ => "unknown"
            };

            string[] fingerprint = ["slskd-download-failed", errorStr];

            Dictionary<string, string> tags = new()
            {
                ["slskd.error_type"] = errorStr,
                ["slskd.retry_count"] = retryCount.ToString(),
                ["slskd.codec"] = context?.Codec ?? "unknown"
            };

            Dictionary<string, object> extras = new()
            {
                ["folder_path"] = context?.FolderPath ?? "",
                ["username"] = context?.Username ?? "",
                ["file_count"] = context?.TrackCountActual ?? 0,
                ["error_message"] = errorMessage ?? ""
            };

            if (context?.Breadcrumbs != null)
            {
                foreach (string breadcrumb in context.Breadcrumbs)
                    sentry.AddBreadcrumb(breadcrumb, "slskd");
            }
            sentry.AddBreadcrumb($"Download failed: {errorStr}", "slskd");

            string message = $"slskd download failed: {errorStr}";
            sentry.CaptureEvent(message, fingerprint, tags, extras, SentryLevel.Warning);
        }

        public static DownloadFailureReason CategorizeDownloadError(string? errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return DownloadFailureReason.Unknown;

            string lower = errorMessage.ToLowerInvariant();

            if (lower.Contains("timeout") || lower.Contains("timed out"))
                return DownloadFailureReason.Timeout;

            if (lower.Contains("cancel") || lower.Contains("abort"))
                return DownloadFailureReason.Cancelled;

            if (lower.Contains("offline") || lower.Contains("not available") || lower.Contains("disconnected"))
                return DownloadFailureReason.UserOffline;

            if (lower.Contains("permission") || lower.Contains("denied") || lower.Contains("access"))
                return DownloadFailureReason.PermissionDenied;

            return DownloadFailureReason.Unknown;
        }
    }
}
#endif

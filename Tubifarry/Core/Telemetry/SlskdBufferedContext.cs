namespace Tubifarry.Core.Telemetry
{
    public class SlskdBufferedContext
    {
        // Search phase
        public string? SearchId { get; set; }
        public string? SearchQuery { get; set; }
        public string? Artist { get; set; }
        public string? Album { get; set; }
        public string? Strategy { get; set; }
        public int TotalResults { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Parse phase
        public List<string> ExpectedTracks { get; set; } = [];
        public int ExpectedTrackCount { get; set; }
        public string? FolderPath { get; set; }
        public string? RegexMatchType { get; set; }
        public int FuzzyArtistScore { get; set; }
        public int FuzzyAlbumScore { get; set; }
        public int FuzzyArtistTokenSort { get; set; }
        public int FuzzyAlbumTokenSort { get; set; }
        public int Priority { get; set; }
        public string? Codec { get; set; }
        public int Bitrate { get; set; }
        public int BitDepth { get; set; }
        public int TrackCountExpected { get; set; }
        public int TrackCountActual { get; set; }
        public string? Username { get; set; }
        public bool HasFreeSlot { get; set; }
        public int QueueLength { get; set; }
        public List<string>? DirectoryFiles { get; set; }
        public bool IsInteractive { get; set; }

        public List<ParseCandidate> AllCandidates { get; set; } = [];

        // Grab phase
        public string? DownloadId { get; set; }

        // Selection analysis
        public int? OurTopPriority { get; set; }
        public int? GrabbedPriority { get; set; }
        public bool? LidarrUsedOurTop { get; set; }

        // Settings snapshot
        public int? SettingsTrackCountFilter { get; set; }
        public bool? SettingsNormalizedSearch { get; set; }
        public bool? SettingsAppendYear { get; set; }
        public bool? SettingsHandleVolumeVariations { get; set; }
        public bool? SettingsUseFallbackSearch { get; set; }
        public bool? SettingsUseTrackFallback { get; set; }
        public int? SettingsMinimumResults { get; set; }
        public bool? SettingsHasTemplates { get; set; }

        // Breadcrumbs
        public List<string> Breadcrumbs { get; set; } = [];
    }

    public class ParseCandidate
    {
        public string FolderName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string RegexMatchType { get; set; } = "";
        public int FuzzyArtist { get; set; }
        public int FuzzyAlbum { get; set; }
        public int Priority { get; set; }
        public int TrackCount { get; set; }
        public string Codec { get; set; } = "";
        public string Username { get; set; } = "";
        public bool WasGrabbed { get; set; }
    }
}

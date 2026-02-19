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

        // Grab phase
        public string? DownloadId { get; set; }

        // Breadcrumbs
        public List<string> Breadcrumbs { get; set; } = [];
    }
}

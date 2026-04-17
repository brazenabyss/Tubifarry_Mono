using System.Text.Json.Serialization;

namespace Tubifarry.Indexers.Monochrome
{
    public class MonochromeResponse
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("data")]
        public MonochromeSearchData? Data { get; set; }
    }

    public class MonochromeSearchData
    {
        [JsonPropertyName("albums")]
        public MonochromeAlbumList? Albums { get; set; }

        [JsonPropertyName("tracks")]
        public MonochromeTrackList? Tracks { get; set; }
    }

    public class MonochromeAlbumList
    {
        [JsonPropertyName("items")]
        public List<MonochromeAlbum>? Items { get; set; }

        [JsonPropertyName("totalNumberOfItems")]
        public int Total { get; set; }
    }

    public class MonochromeTrackList
    {
        [JsonPropertyName("items")]
        public List<MonochromeTrack>? Items { get; set; }
    }

    public class MonochromeAlbum
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("numberOfTracks")]
        public int NumberOfTracks { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("audioQuality")]
        public string? AudioQuality { get; set; }

        [JsonPropertyName("audioModes")]
        public List<string>? AudioModes { get; set; }

        [JsonPropertyName("mediaMetadata")]
        public MonochromeMediaMetadata? MediaMetadata { get; set; }

        [JsonPropertyName("artists")]
        public List<MonochromeArtist>? Artists { get; set; }

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        // Convenience: primary artist name
        public string ArtistName => Artists?.FirstOrDefault()?.Name ?? "Unknown Artist";

        // Cover art URL from UUID
        public string CoverUrl => string.IsNullOrEmpty(Cover)
            ? string.Empty
            : $"https://resources.tidal.com/images/{Cover.Replace('-', '/')}/1280x1280.jpg";

        public bool IsHiRes => MediaMetadata?.Tags?.Contains("HIRES_LOSSLESS") == true;
    }

    public class MonochromeMediaMetadata
    {
        [JsonPropertyName("tags")]
        public List<string>? Tags { get; set; }
    }

    public class MonochromeArtist
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class MonochromeTrack
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("trackNumber")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("volumeNumber")]
        public int VolumeNumber { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("audioQuality")]
        public string? AudioQuality { get; set; }

        [JsonPropertyName("artists")]
        public List<MonochromeArtist>? Artists { get; set; }

        [JsonPropertyName("album")]
        public MonochromeAlbum? Album { get; set; }

        [JsonPropertyName("manifest")]
        public string? Manifest { get; set; }

        [JsonPropertyName("manifestMimeType")]
        public string? ManifestMimeType { get; set; }

        public string ArtistName => Artists?.FirstOrDefault()?.Name ?? "Unknown Artist";
    }

    public class MonochromeManifest
    {
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("codecs")]
        public string? Codecs { get; set; }

        [JsonPropertyName("encryptionType")]
        public string? EncryptionType { get; set; }

        [JsonPropertyName("urls")]
        public List<string>? Urls { get; set; }
    }

    public class MonochromeAlbumResponse
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("data")]
        public MonochromeAlbumDetail? Data { get; set; }
    }

    public class MonochromeAlbumDetail
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("numberOfTracks")]
        public int NumberOfTracks { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("audioQuality")]
        public string? AudioQuality { get; set; }

        [JsonPropertyName("artists")]
        public List<MonochromeArtist>? Artists { get; set; }

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("items")]
        public List<MonochromeTrackItem>? Items { get; set; }

        public string ArtistName => Artists?.FirstOrDefault()?.Name ?? "Unknown Artist";
    }

    public class MonochromeTrackItem
    {
        [JsonPropertyName("item")]
        public MonochromeTrack? Item { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}

    public class MonochromeTrackResponse
    {
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("data")]
        public MonochromeTrackManifestData? Data { get; set; }
    }

    public class MonochromeTrackManifestData
    {
        [JsonPropertyName("trackId")]
        public long TrackId { get; set; }

        [JsonPropertyName("audioQuality")]
        public string? AudioQuality { get; set; }

        [JsonPropertyName("manifestMimeType")]
        public string? ManifestMimeType { get; set; }

        [JsonPropertyName("manifest")]
        public string? Manifest { get; set; }

        [JsonPropertyName("bitDepth")]
        public int BitDepth { get; set; }

        [JsonPropertyName("sampleRate")]
        public int SampleRate { get; set; }
    }

namespace Tubifarry.Download.Clients.Monochrome
{
    public enum MonochromeDownloadState
    {
        Idle,
        Running,
        Completed,
        Failed
    }
}

// Tidal direct API response for /tracks/{id}/playbackinfo
public class TidalPlaybackInfo
{
    [JsonPropertyName("manifest")]
    public string? Manifest { get; set; }
    [JsonPropertyName("manifestMimeType")]
    public string? ManifestMimeType { get; set; }
    [JsonPropertyName("audioQuality")]
    public string? AudioQuality { get; set; }
}

using System.Text.Json.Serialization;

namespace Tubifarry.Indexers.Monochrome
{
    public class MonochromeSearchResult
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

        [JsonPropertyName("artist")]
        public MonochromeArtist? Artist { get; set; }

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }
    }

    public class MonochromeTrack
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("trackNumber")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("audioQuality")]
        public string? AudioQuality { get; set; }

        [JsonPropertyName("artist")]
        public MonochromeArtist? Artist { get; set; }

        [JsonPropertyName("album")]
        public MonochromeAlbum? Album { get; set; }

        [JsonPropertyName("manifest")]
        public string? Manifest { get; set; }

        [JsonPropertyName("manifestMimeType")]
        public string? ManifestMimeType { get; set; }
    }

    public class MonochromeArtist
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
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

        [JsonPropertyName("artist")]
        public MonochromeArtist? Artist { get; set; }

        [JsonPropertyName("cover")]
        public string? Cover { get; set; }

        [JsonPropertyName("tracks")]
        public MonochromeTrackList? Tracks { get; set; }
    }
}
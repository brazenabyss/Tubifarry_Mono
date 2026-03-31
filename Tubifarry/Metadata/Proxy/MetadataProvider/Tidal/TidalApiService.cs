using NLog;
using NzbDrone.Common.Instrumentation;
using System.Net.Http;
using System.Text.Json;
using Tubifarry.Indexers.Monochrome;

namespace Tubifarry.Metadata.Proxy.MetadataProvider.Tidal
{
    // --- Response DTOs ---

    public class TidalArtistResponse
    {
        public TidalArtistData? Artist { get; set; }
        public TidalArtistCover? Cover { get; set; }
    }

    public class TidalArtistData
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
        public int Popularity { get; set; }
        public string? Url { get; set; }
    }

    public class TidalArtistCover
    {
        public string? Name { get; set; }
        public string? _750 { get; set; }
        [System.Text.Json.Serialization.JsonPropertyName("750")]
        public string? Size750 { get; set; }
    }

    public class TidalArtistFullResponse
    {
        public TidalAlbumList? Albums { get; set; }
        public List<TidalTrackSummary>? Tracks { get; set; }
    }

    public class TidalAlbumList
    {
        public List<TidalAlbumSummary>? Items { get; set; }
    }

    public class TidalAlbumSummary
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public int Duration { get; set; }
        public int NumberOfTracks { get; set; }
        public int NumberOfVolumes { get; set; }
        public string? ReleaseDate { get; set; }
        public string? Copyright { get; set; }
        public string? Type { get; set; }
        public string? Cover { get; set; }
        public string? Upc { get; set; }
        public bool Explicit { get; set; }
        public int Popularity { get; set; }
        public string? AudioQuality { get; set; }
        public TidalArtistRef? Artist { get; set; }
        public List<TidalArtistRef>? Artists { get; set; }
        public string? Url { get; set; }
        public string ArtistName => Artists?.FirstOrDefault(a => a.Type == "MAIN")?.Name ?? Artist?.Name ?? "Unknown Artist";
        public string CoverUrl => string.IsNullOrEmpty(Cover) ? string.Empty
            : $"https://resources.tidal.com/images/{Cover.Replace('-', '/')}/1280x1280.jpg";
    }

    public class TidalArtistRef
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Picture { get; set; }
    }

    public class TidalTrackSummary
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public int Duration { get; set; }
        public int TrackNumber { get; set; }
        public int VolumeNumber { get; set; }
        public bool Explicit { get; set; }
        public TidalAlbumRef? Album { get; set; }
        public List<TidalArtistRef>? Artists { get; set; }
    }

    public class TidalAlbumRef
    {
        public long Id { get; set; }
        public string? Title { get; set; }
        public string? Cover { get; set; }
        public string? ReleaseDate { get; set; }
    }

    public class TidalSearchArtistResponse
    {
        public TidalSearchArtistData? Data { get; set; }
    }

    public class TidalSearchArtistData
    {
        public TidalArtistSearchList? Artists { get; set; }
    }

    public class TidalArtistSearchList
    {
        public List<TidalArtistSearchItem>? Items { get; set; }
    }

    public class TidalArtistSearchItem
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public string? Picture { get; set; }
        public int Popularity { get; set; }
        public string? Url { get; set; }
        public string PictureUrl => string.IsNullOrEmpty(Picture) ? string.Empty
            : $"https://resources.tidal.com/images/{Picture.Replace('-', '/')}/750x750.jpg";
    }

    public class TidalSearchAlbumResponse
    {
        public TidalSearchAlbumData? Data { get; set; }
    }

    public class TidalSearchAlbumData
    {
        public TidalAlbumSearchList? Albums { get; set; }
    }

    public class TidalAlbumSearchList
    {
        public List<TidalAlbumSummary>? Items { get; set; }
    }

    // --- API Service ---

    public class TidalApiService
    {
        private static readonly HttpClient _http = new();
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        private readonly Logger _logger = NzbDroneLogger.GetLogger(typeof(TidalApiService));

        public async Task<TidalArtistResponse?> GetArtistAsync(string baseUrl, string artistId)
        {
            string url = $"{baseUrl.TrimEnd('/')}/artist/?id={artistId}";
            return await GetAsync<TidalArtistResponse>(url);
        }

        public async Task<TidalArtistFullResponse?> GetArtistFullAsync(string baseUrl, string artistId)
        {
            string url = $"{baseUrl.TrimEnd('/')}/artist/?f={artistId}";
            return await GetAsync<TidalArtistFullResponse>(url);
        }

        public async Task<MonochromeAlbumResponse?> GetAlbumAsync(string baseUrl, string albumId)
        {
            string url = $"{baseUrl.TrimEnd('/')}/album/?id={albumId}";
            return await GetAsync<MonochromeAlbumResponse>(url);
        }

        public async Task<List<TidalArtistSearchItem>> SearchArtistsAsync(string baseUrl, string query)
        {
            string url = $"{baseUrl.TrimEnd('/')}/search/?a={Uri.EscapeDataString(query)}";
            TidalSearchArtistResponse? result = await GetAsync<TidalSearchArtistResponse>(url);
            return result?.Data?.Artists?.Items ?? [];
        }

        public async Task<List<TidalAlbumSummary>> SearchAlbumsAsync(string baseUrl, string query)
        {
            string url = $"{baseUrl.TrimEnd('/')}/search/?al={Uri.EscapeDataString(query)}";
            TidalSearchAlbumResponse? result = await GetAsync<TidalSearchAlbumResponse>(url);
            return result?.Data?.Albums?.Items ?? [];
        }

        private async Task<T?> GetAsync<T>(string url)
        {
            try
            {
                HttpRequestMessage req = new(HttpMethod.Get, url);
                req.Headers.Add("User-Agent", Tubifarry.UserAgent);
                HttpResponseMessage response = await _http.SendAsync(req);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _json);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Tidal API request failed: {Url}", url);
                return default;
            }
        }
    }
}

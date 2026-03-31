using NLog;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
using Tubifarry.Indexers.Monochrome;
using Tubifarry.Metadata.Proxy.MetadataProvider.Mixed;

namespace Tubifarry.Metadata.Proxy.MetadataProvider.Tidal
{
    [Proxy(ProxyMode.Public)]
    [ProxyFor(typeof(IProvideArtistInfo))]
    [ProxyFor(typeof(IProvideAlbumInfo))]
    [ProxyFor(typeof(ISearchForNewArtist))]
    [ProxyFor(typeof(ISearchForNewAlbum))]
    [ProxyFor(typeof(ISearchForNewEntity))]
    public class TidalMetadataProxy : ProxyBase<TidalMetadataProxySettings>, IMetadata, ISupportMetadataMixing
    {
        private readonly TidalApiService _api = new();
        private readonly Logger _logger;

        public override string Name => "Tidal";
        private string BaseUrl => Settings?.BaseUrl ?? TidalMetadataProxySettings.Instance?.BaseUrl
            ?? "https://frankfurt-1.monochrome.tf";

        public TidalMetadataProxy(Logger logger) => _logger = logger;

        // --- Search ---

        public List<Artist> SearchForNewArtist(string title) =>
            _api.SearchArtistsAsync(BaseUrl, title).GetAwaiter().GetResult()
                .Select(TidalMappingHelper.MapArtist).ToList();

        public List<Album> SearchForNewAlbum(string title, string artist)
        {
            string query = string.IsNullOrWhiteSpace(artist) ? title : $"{artist} {title}";
            return _api.SearchAlbumsAsync(BaseUrl, query).GetAwaiter().GetResult()
                .Select(a => TidalMappingHelper.MapAlbumFromSummary(a)).ToList();
        }

        public List<object> SearchForNewEntity(string title)
        {
            List<object> results = [];
            results.AddRange(SearchForNewArtist(title));
            results.AddRange(SearchForNewAlbum(title, string.Empty));
            return results;
        }

        // --- Info ---

        public Artist GetArtistInfo(string foreignArtistId, int metadataProfileId)
        {
            string? tidalId = TidalMappingHelper.ExtractTidalId(foreignArtistId);
            if (tidalId == null)
            {
                _logger.Debug("Non-Tidal artist ID {0}, attempting MusicBrainz name resolution", foreignArtistId);
                tidalId = ResolveMusicBrainzToTidal(foreignArtistId);
                if (tidalId == null)
                    throw new NzbDrone.Core.Exceptions.ArtistNotFoundException(foreignArtistId);
            }

            TidalArtistResponse? artistData = _api.GetArtistAsync(BaseUrl, tidalId).GetAwaiter().GetResult();
            TidalArtistFullResponse? full = _api.GetArtistFullAsync(BaseUrl, tidalId).GetAwaiter().GetResult();

            if (artistData?.Artist == null)
                throw new Exception($"Artist {tidalId} not found on Tidal");

            List<TidalAlbumSummary> albums = full?.Albums?.Items ?? [];
            Artist artist = TidalMappingHelper.MapArtistFromData(artistData.Artist, artistData.Cover, albums);

            // Attach albums
            List<Album> mappedAlbums = albums.Select(a => TidalMappingHelper.MapAlbumFromSummary(a, artist)).ToList();
            artist.Albums = new(mappedAlbums);

            return artist;
        }

        public Tuple<string, Album, List<ArtistMetadata>> GetAlbumInfo(string foreignAlbumId)
        {
            string? tidalId = TidalMappingHelper.ExtractTidalId(foreignAlbumId);
            if (tidalId == null)
            {
                _logger.Debug("Tidal proxy skipping non-Tidal album ID: {0}", foreignAlbumId);
                throw new NzbDrone.Core.Exceptions.AlbumNotFoundException(foreignAlbumId);
            }

            MonochromeAlbumResponse? response = _api.GetAlbumAsync(BaseUrl, tidalId).GetAwaiter().GetResult();
            MonochromeAlbumDetail? detail = response?.Data;

            if (detail == null)
                throw new Exception($"Album {tidalId} not found on Tidal");

            // Build a minimal artist from the album data
            MonochromeArtist? mainArtist = detail.Artists?.FirstOrDefault();
            Artist artist;
            List<ArtistMetadata> artistMetadata;

            if (mainArtist != null)
            {
                string pictureUrl = string.Empty;
                ArtistMetadata meta = new()
                {
                    ForeignArtistId = TidalMappingHelper.ToForeignId(mainArtist.Id),
                    Name = mainArtist.Name ?? string.Empty,
                    Images = string.IsNullOrEmpty(pictureUrl) ? [] :
                        [new NzbDrone.Core.MediaCover.MediaCover(NzbDrone.Core.MediaCover.MediaCoverTypes.Poster, pictureUrl)],
                    Genres = [],
                    Links = []
                };
                artist = new Artist
                {
                    ForeignArtistId = meta.ForeignArtistId,
                    Name = meta.Name,
                    Metadata = new(meta)
                };
                artistMetadata = [meta];
            }
            else
            {
                ArtistMetadata meta = new() { ForeignArtistId = "unknown@tidal", Name = "Unknown Artist", Genres = [], Links = [], Images = [] };
                artist = new Artist { ForeignArtistId = meta.ForeignArtistId, Name = meta.Name, Metadata = new(meta) };
                artistMetadata = [meta];
            }

            Album album = TidalMappingHelper.MapAlbumFromDetail(detail, artist);
            return Tuple.Create(artist.ForeignArtistId, album, artistMetadata);
        }

        // --- Change tracking (not supported) ---

        public HashSet<string> GetChangedAlbums(DateTime startTime)
        {
            _logger.Warn("GetChangedAlbums: Tidal does not support change tracking.");
            return [];
        }

        public HashSet<string> GetChangedArtists(DateTime startTime)
        {
            _logger.Warn("GetChangedArtists: Tidal does not support change tracking.");
            return [];
        }

        public List<Album> SearchForNewAlbumByRecordingIds(List<string> recordingIds) => [];

        // --- ID/search capability ---

        public MetadataSupportLevel CanHandleSearch(string? albumTitle, string? artistName)
        {
            if (IsTidalId(albumTitle) || IsTidalId(artistName))
                return MetadataSupportLevel.Supported;
            return MetadataSupportLevel.ImplicitSupported;
        }

        public MetadataSupportLevel CanHandleId(string id) =>
            id.EndsWith("@tidal") ? MetadataSupportLevel.Supported : MetadataSupportLevel.Unsupported;

        public MetadataSupportLevel CanHandleIRecordingIds(params string[] recordingIds) =>
            MetadataSupportLevel.Unsupported;

        public MetadataSupportLevel CanHandleChanged() => MetadataSupportLevel.Unsupported;

        public string? SupportsLink(List<Links> links)
        {
            if (links == null || links.Count == 0) return null;
            foreach (Links link in links)
            {
                if (string.IsNullOrWhiteSpace(link.Url)) continue;
                System.Text.RegularExpressions.Match m =
                    System.Text.RegularExpressions.Regex.Match(link.Url,
                        @"tidal\.com/(?:album|artist|track)/(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                    return TidalMappingHelper.ToForeignId(long.Parse(m.Groups[1].Value));
            }
            return null;
        }

        private static bool IsTidalId(string? s) => s != null && s.EndsWith("@tidal");
        // --- MusicBrainz → Tidal resolution ---

        private static readonly string _cacheFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Lidarr", "mb-tidal-cache.json");

        private static Dictionary<string, string>? _idCache;

        private static Dictionary<string, string> LoadCache()
        {
            if (_idCache != null) return _idCache;
            try
            {
                if (File.Exists(_cacheFile))
                    _idCache = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(
                        File.ReadAllText(_cacheFile)) ?? [];
            }
            catch { }
            _idCache ??= [];
            return _idCache;
        }

        private static void SaveCache()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_cacheFile)!);
                File.WriteAllText(_cacheFile,
                    System.Text.Json.JsonSerializer.Serialize(_idCache));
            }
            catch { }
        }

        private string? ResolveMusicBrainzToTidal(string mbId)
        {
            Dictionary<string, string> cache = LoadCache();
            if (cache.TryGetValue(mbId, out string? cached))
            {
                _logger.Debug("Cache hit for MusicBrainz ID {0} → Tidal {1}", mbId, cached);
                return cached;
            }

            // Resolve name from MusicBrainz
            string? artistName = null;
            try
            {
                using System.Net.Http.HttpClient http = new();
                http.DefaultRequestHeaders.Add("User-Agent", Tubifarry.UserAgent);
                string mbUrl = $"https://musicbrainz.org/ws/2/artist/{mbId}?fmt=json";
                string json = http.GetStringAsync(mbUrl).GetAwaiter().GetResult();
                System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(json);
                artistName = doc.RootElement.GetProperty("name").GetString();
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to resolve MusicBrainz ID {0} to artist name", mbId);
                return null;
            }

            if (string.IsNullOrEmpty(artistName))
                return null;

            _logger.Debug("Resolved MusicBrainz {0} → '{1}', searching Tidal", mbId, artistName);

            // Search Tidal for the artist
            List<TidalArtistSearchItem> results = _api.SearchArtistsAsync(BaseUrl, artistName)
                .GetAwaiter().GetResult();

            // Best match: exact name match first, then highest popularity
            TidalArtistSearchItem? best = results
                .OrderByDescending(a => string.Equals(a.Name, artistName, StringComparison.OrdinalIgnoreCase) ? 1000 : 0)
                .ThenByDescending(a => a.Popularity)
                .FirstOrDefault();

            if (best == null)
            {
                _logger.Warn("No Tidal match found for artist '{0}' (MB: {1})", artistName, mbId);
                return null;
            }

            string tidalId = best.Id.ToString();
            _logger.Info("Mapped MusicBrainz {0} ('{1}') → Tidal {2} ('{3}')",
                mbId, artistName, tidalId, best.Name);

            cache[mbId] = tidalId;
            SaveCache();
            return tidalId;
        }

    }
}

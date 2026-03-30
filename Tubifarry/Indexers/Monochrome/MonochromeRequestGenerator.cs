using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.IndexerSearch.Definitions;
using Tubifarry.Core.Utilities;

namespace Tubifarry.Indexers.Monochrome
{
    public interface IMonochromeRequestGenerator : IIndexerRequestGenerator
    {
        void SetSetting(MonochromeIndexerSettings settings);
    }

    public class MonochromeRequestGenerator : IMonochromeRequestGenerator
    {
        private MonochromeIndexerSettings? _settings;
        private readonly Logger _logger;

        public MonochromeRequestGenerator(Logger logger) => _logger = logger;

        public void SetSetting(MonochromeIndexerSettings settings) => _settings = settings;

        public IndexerPageableRequestChain GetSearchRequests(AlbumSearchCriteria searchCriteria)
        {
            // Search by album title, with artist as fallback tier
            LazyIndexerPageableRequestChain chain = new();
            string baseUrl = _settings!.BaseUrl.TrimEnd('/');

            // Primary: search by album name
            string albumQuery = searchCriteria.AlbumQuery?.Trim() ?? string.Empty;
            string artistQuery = searchCriteria.ArtistQuery?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(albumQuery))
            {
                string url = $"{baseUrl}/search/?al={Uri.EscapeDataString(albumQuery)}";
                _logger.Trace("Monochrome album search: {Url}", url);
                chain.Add([CreateRequest(url)]);
            }

            // Fallback tier: search by artist if album search yields nothing
            if (!string.IsNullOrEmpty(artistQuery))
            {
                string fallback = $"{baseUrl}/search/?a={Uri.EscapeDataString(artistQuery)}";
                chain.AddTier([CreateRequest(fallback)]);
            }

            return chain.ToStandardChain();
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria)
        {
            LazyIndexerPageableRequestChain chain = new();
            string baseUrl = _settings!.BaseUrl.TrimEnd('/');
            string url = $"{baseUrl}/search/?a={Uri.EscapeDataString(searchCriteria.ArtistQuery.Trim())}";
            _logger.Trace("Monochrome artist search: {Url}", url);
            chain.Add([CreateRequest(url)]);
            return chain.ToStandardChain();
        }

        public IndexerPageableRequestChain GetRecentRequests() => new();

        private IndexerRequest CreateRequest(string url)
        {
            HttpRequest req = new(url)
            {
                RequestTimeout = TimeSpan.FromSeconds(_settings!.RequestTimeout),
                SuppressHttpError = false,
                LogHttpError = true
            };
            req.Headers["User-Agent"] = Tubifarry.UserAgent;
            req.Headers["X-Quality"] = ((MonochromeQuality)_settings.Quality).ToString();
            return new IndexerRequest(req);
        }
    }
}

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
            LazyIndexerPageableRequestChain chain = new();
            string baseUrl = _settings!.BaseUrl.TrimEnd('/');

            string albumQuery = searchCriteria.AlbumQuery?.Trim() ?? string.Empty;
            string artistQuery = searchCriteria.ArtistQuery?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(albumQuery))
            {
                string url = $"{baseUrl}/search/?al={Uri.EscapeDataString(albumQuery)}&limit=100";
                IndexerRequest request = CreateRequest(url);

                // Pass artist query via header so the parser can filter the 100 results down
                if (!string.IsNullOrEmpty(artistQuery))
                    request.HttpRequest.Headers["X-Artist-Filter"] = artistQuery;

                _logger.Trace("Monochrome album search: {Url}", url);
                chain.Add([request]);
            }

            // Fallback tier: artist-only if album search yields nothing after filtering
            if (!string.IsNullOrEmpty(artistQuery))
            {
                string fallback = $"{baseUrl}/search/?a={Uri.EscapeDataString(artistQuery)}";
                _logger.Trace("Monochrome artist-only fallback: {Url}", fallback);
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

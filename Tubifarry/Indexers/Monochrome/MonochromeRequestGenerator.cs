using Tubifarry.Indexers.Monochrome;
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
            string query = $"{searchCriteria.ArtistQuery} {searchCriteria.AlbumQuery}".Trim();
            return BuildChain(query, false);
        }

        public IndexerPageableRequestChain GetSearchRequests(ArtistSearchCriteria searchCriteria) =>
            BuildChain(searchCriteria.ArtistQuery, false);

        public IndexerPageableRequestChain GetRecentRequests() => new();

        private IndexerPageableRequestChain BuildChain(string query, bool isSingle)
        {
            LazyIndexerPageableRequestChain chain = new();
            string baseUrl = _settings!.BaseUrl.TrimEnd('/');
            string url = $"{baseUrl}/search/?q={Uri.EscapeDataString(query)}&type=ALBUMS&limit={_settings.SearchLimit}";
            _logger.Trace("Creating Monochrome search request: {Url}", url);
            chain.Add([CreateRequest(url)]);
            if (isSingle)
            {
                string fallback = $"{baseUrl}/search/?q={Uri.EscapeDataString(query)}&type=TRACKS&limit={_settings.SearchLimit}";
                chain.AddTier([CreateRequest(fallback)]);
            }
            return chain.ToStandardChain();
        }

        private IndexerRequest CreateRequest(string url)
        {
            HttpRequest req = new(url)
            {
                RequestTimeout = TimeSpan.FromSeconds(_settings!.RequestTimeout),
                SuppressHttpError = false,
                LogHttpError = true
            };
            req.Headers["User-Agent"] = Tubifarry.UserAgent;
            req.Headers["X-Quality"] = _settings.Quality.ToString();
            return new IndexerRequest(req);
        }
    }
}

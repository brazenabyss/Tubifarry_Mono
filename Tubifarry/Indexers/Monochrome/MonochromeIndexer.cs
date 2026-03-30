using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;

namespace Tubifarry.Indexers.Monochrome
{
    public class MonochromeIndexer : HttpIndexerBase<MonochromeIndexerSettings>
    {
        private readonly IMonochromeRequestGenerator _requestGenerator;
        private readonly IMonochromeParser _parser;

        public override string Name => "Monochrome";
        public override string Protocol => nameof(MonochromeDownloadProtocol);
        public override bool SupportsRss => false;
        public override bool SupportsSearch => true;
        public override int PageSize => 20;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(1);
        public override ProviderMessage Message => new(
            "Monochrome provides lossless music via the HiFi API (Tidal backend). No account required.",
            ProviderMessageType.Info);

        public MonochromeIndexer(
            IMonochromeRequestGenerator requestGenerator,
            IMonochromeParser parser,
            IHttpClient httpClient,
            IIndexerStatusService statusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, statusService, configService, parsingService, logger)
        {
            _requestGenerator = requestGenerator;
            _parser = parser;
        }

        protected override async Task Test(List<ValidationFailure> failures)
        {
            try
            {
                string testUrl = $"{Settings.BaseUrl.TrimEnd('/')}/search/?q=test&type=ALBUMS&limit=1";
                HttpRequest req = new(testUrl) { RequestTimeout = TimeSpan.FromSeconds(15) };
                req.Headers["User-Agent"] = Tubifarry.UserAgent;
                HttpResponse response = await _httpClient.ExecuteAsync(req);
                if (!response.HasHttpError)
                {
                    _logger.Debug("Successfully connected to Monochrome instance at {Url}", Settings.BaseUrl);
                    return;
                }
                failures.Add(new ValidationFailure("BaseUrl",
                    $"Could not connect to Monochrome instance: HTTP {response.StatusCode}"));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error connecting to Monochrome API");
                failures.Add(new ValidationFailure("BaseUrl", ex.Message));
            }
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            _requestGenerator.SetSetting(Settings);
            return _requestGenerator;
        }

        public override IParseIndexerResponse GetParser() => _parser;
    }
}

using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser;
using NzbDrone.Core.ThingiProvider;
using System.Text.Json;

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
        public override int PageSize => 25;
        public override TimeSpan RateLimit => TimeSpan.FromSeconds(1);
        public override ProviderMessage Message => new(
            "Monochrome provides lossless music via the Tidal backend. No account required.",
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
                // Use a known artist search to validate the API is responding correctly
                string testUrl = $"{Settings.BaseUrl.TrimEnd('/')}/search/?a=radiohead";
                HttpRequest req = new(testUrl) { RequestTimeout = TimeSpan.FromSeconds(15) };
                req.Headers["User-Agent"] = Tubifarry.UserAgent;

                HttpResponse response = await _httpClient.ExecuteAsync(req);

                if (response.HasHttpError)
                {
                    failures.Add(new ValidationFailure("BaseUrl",
                        $"Could not connect to Monochrome API: HTTP {response.StatusCode}"));
                    return;
                }

                // Validate the response is actually a Monochrome API response
                MonochromeResponse? parsed = JsonSerializer.Deserialize<MonochromeResponse>(
                    response.Content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed?.Data == null)
                {
                    failures.Add(new ValidationFailure("BaseUrl",
                        "The URL does not appear to be a valid Monochrome API instance — unexpected response format."));
                    return;
                }

                _logger.Debug("Successfully connected to Monochrome API at {Url} (version {Version})",
                    Settings.BaseUrl, parsed.Version);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error connecting to Monochrome API");
                failures.Add(new ValidationFailure("BaseUrl", $"Connection failed: {ex.Message}"));
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

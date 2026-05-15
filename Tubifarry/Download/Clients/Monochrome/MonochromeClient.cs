using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RemotePathMappings;
using System.Net.Http;
using System.Text.Json;
using Tubifarry.Indexers.Monochrome;

namespace Tubifarry.Download.Clients.Monochrome
{
    public class MonochromeClient : DownloadClientBase<MonochromeProviderSettings>
    {
        private readonly IMonochromeDownloadManager _downloadManager;
        private readonly INamingConfigService _namingService;
        private readonly IDiskProvider _disk;
        private static readonly HttpClient _httpClient = new();

        public override string Name => "Monochrome";
        public override string Protocol => nameof(MonochromeDownloadProtocol);
        public new MonochromeProviderSettings Settings => base.Settings;

        public MonochromeClient(
            IMonochromeDownloadManager downloadManager,
            INamingConfigService namingConfigService,
            IConfigService configService,
            IDiskProvider diskProvider,
            IRemotePathMappingService remotePathMappingService,
            ILocalizationService localizationService,
            Logger logger)
            : base(configService, diskProvider, remotePathMappingService, localizationService, logger)
        {
            _downloadManager = downloadManager;
            _namingService = namingConfigService;
            _disk = diskProvider;
        }

        public override Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer) =>
            _downloadManager.Download(remoteAlbum, indexer, _namingService.GetConfig(), this);

        public override IEnumerable<DownloadClientItem> GetItems() => _downloadManager.GetItems();

        public override void RemoveItem(DownloadClientItem item, bool deleteData)
        {
            if (deleteData) DeleteItemData(item);
            _downloadManager.RemoveItem(item);
        }

        public override DownloadClientInfo GetStatus() => new()
        {
            IsLocalhost = Settings.BaseUrl.Contains("localhost") || Settings.BaseUrl.Contains("127.0.0.1"),
            OutputRootFolders = [new OsPath(Settings.DownloadPath)]
        };

        protected override void Test(List<ValidationFailure> failures)
        {
            // 1 — Check download path
            if (!_disk.FolderExists(Settings.DownloadPath))
            {
                failures.Add(new ValidationFailure("DownloadPath", "Download path does not exist"));
                return;
            }
            if (!_disk.FolderWritable(Settings.DownloadPath))
            {
                failures.Add(new ValidationFailure("DownloadPath", "Download path is not writable"));
                return;
            }

            // 2 — Check API connectivity and validate response format
            try
            {
                string testUrl = $"{Settings.BaseUrl.TrimEnd('/')}/search/?a=radiohead";
                HttpResponseMessage response = _httpClient.GetAsync(testUrl).GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    failures.Add(new ValidationFailure("BaseUrl",
                        $"Could not connect to Monochrome API: HTTP {(int)response.StatusCode}"));
                    return;
                }

                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                MonochromeResponse? parsed = JsonSerializer.Deserialize<MonochromeResponse>(content,
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
    }
}

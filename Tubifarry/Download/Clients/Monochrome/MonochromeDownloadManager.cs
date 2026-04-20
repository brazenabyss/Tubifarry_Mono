using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using Tubifarry.Download.Base;
using Tubifarry.Indexers.Monochrome;

namespace Tubifarry.Download.Clients.Monochrome
{
    public interface IMonochromeDownloadManager : IBaseDownloadManager<MonochromeDownloadRequest, MonochromeDownloadOptions, MonochromeClient> { }

    public class MonochromeDownloadManager : BaseDownloadManager<MonochromeDownloadRequest, MonochromeDownloadOptions, MonochromeClient>, IMonochromeDownloadManager
    {
        private readonly List<MonochromeDownloadRequest> _requests = new();

        public MonochromeDownloadManager(Logger logger) : base(logger) { }

        public override async Task<string> Download(RemoteAlbum remoteAlbum, IIndexer indexer, NamingConfig namingConfig, MonochromeClient provider)
        {
            MonochromeDownloadRequest request = await CreateDownloadRequest(remoteAlbum, indexer, namingConfig, provider);
            _requests.Add(request);

            // Call base to add to its internal queue for status tracking
            // Then start immediately
            string id = request.ID;
            _logger.Debug("Starting Monochrome download {Id} | {Title}", id, remoteAlbum.Release.Title);
            request.Start();
            return id;
        }

        public override IEnumerable<DownloadClientItem> GetItems() =>
            _requests.Select(r => r.ClientItem);

        public override void RemoveItem(DownloadClientItem item)
        {
            MonochromeDownloadRequest? request = _requests.Find(r => r.ID == item.DownloadId);
            if (request == null) return;
            request.Dispose();
            _requests.Remove(request);
        }

        protected override async Task<MonochromeDownloadRequest> CreateDownloadRequest(
            RemoteAlbum remoteAlbum,
            IIndexer indexer,
            NamingConfig namingConfig,
            MonochromeClient provider)
        {
            string downloadUrl = remoteAlbum.Release.DownloadUrl;
            bool isTrack = downloadUrl.Contains("/track/");

            string itemId = Uri.TryCreate(downloadUrl, UriKind.Absolute, out Uri? uri)
                ? System.Web.HttpUtility.ParseQueryString(uri.Query)["id"] ?? downloadUrl.Split('/').Last()
                : downloadUrl.Split('/').Last();

            _logger.Debug("Monochrome download: {Type}, ID: {Id} | Token present: {HasToken} | Token length: {Len}", isTrack ? "Track" : "Album", itemId, !string.IsNullOrEmpty(provider.Settings.TidalToken), provider.Settings.TidalToken?.Length ?? 0);

            MonochromeDownloadOptions options = new()
            {
                Handler = _requesthandler,
                DownloadPath = provider.Settings.DownloadPath,
                BaseUrl = provider.Settings.BaseUrl,
                Quality = ((MonochromeQuality)provider.Settings.Quality).ToString(),
                MaxDownloadSpeed = provider.Settings.MaxDownloadSpeed * 1024,
                ConnectionRetries = provider.Settings.ConnectionRetries,
                NamingConfig = namingConfig,
                DelayBetweenAttemps = TimeSpan.FromSeconds(2),
                NumberOfAttempts = (byte)provider.Settings.ConnectionRetries,
                ClientInfo = DownloadClientItemClientInfo.FromDownloadClient(provider, false),
                IsTrack = isTrack,
                ItemId = itemId,
            };

            _requesthandler.MaxParallelism = provider.Settings.MaxParallelDownloads;
            return await Task.FromResult(new MonochromeDownloadRequest(remoteAlbum, options));
        }
    }
}

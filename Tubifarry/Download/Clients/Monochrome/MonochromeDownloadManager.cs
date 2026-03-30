using Tubifarry.Indexers.Monochrome;
using NLog;
using NzbDrone.Core.Download;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Parser.Model;
using Tubifarry.Download.Base;

namespace Tubifarry.Download.Clients.Monochrome
{
    public interface IMonochromeDownloadManager : IBaseDownloadManager<MonochromeDownloadRequest, MonochromeDownloadOptions, MonochromeClient> { }

    public class MonochromeDownloadManager : BaseDownloadManager<MonochromeDownloadRequest, MonochromeDownloadOptions, MonochromeClient>, IMonochromeDownloadManager
    {
        public MonochromeDownloadManager(Logger logger) : base(logger) { }

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

            _logger.Trace("Monochrome download: {Type}, ID: {Id}", isTrack ? "Track" : "Album", itemId);

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

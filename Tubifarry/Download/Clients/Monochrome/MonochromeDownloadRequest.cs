using NzbDrone.Core.Music;
using NzbDrone.Core.Parser.Model;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Tubifarry.Core.Utilities;
using Tubifarry.Download.Base;
using Tubifarry.Indexers.Monochrome;

namespace Tubifarry.Download.Clients.Monochrome
{
    public class MonochromeDownloadRequest : BaseDownloadRequest<MonochromeDownloadOptions>
    {
        private static readonly HttpClient _httpClient = new();

        public MonochromeDownloadRequest(RemoteAlbum remoteAlbum, MonochromeDownloadOptions options)
            : base(remoteAlbum, options) { }

        protected override async Task ProcessDownloadAsync(CancellationToken token)
        {
            string baseUrl = Options.BaseUrl.TrimEnd('/');
            string albumId = Options.ItemId;

            MonochromeAlbumDetail? albumDetail = await FetchAlbumDetail(baseUrl, albumId, token);
            if (albumDetail?.Tracks?.Items == null || albumDetail.Tracks.Items.Count == 0)
                throw new Exception($"No tracks found for album {albumId}");

            _logger.Debug("Downloading {Count} tracks for album {Title}",
                albumDetail.Tracks.Items.Count, albumDetail.Title);

            _expectedTrackCount = albumDetail.Tracks.Items.Count;

            foreach (MonochromeTrack track in albumDetail.Tracks.Items)
            {
                token.ThrowIfCancellationRequested();
                await DownloadTrack(track, albumDetail, baseUrl, token);
            }
        }

        private async Task<MonochromeAlbumDetail?> FetchAlbumDetail(string baseUrl, string albumId, CancellationToken ct)
        {
            string url = $"{baseUrl}/album/?id={albumId}";
            _logger.Trace("Fetching album detail: {Url}", url);
            HttpResponseMessage response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            string json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<MonochromeAlbumDetail>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task DownloadTrack(MonochromeTrack track, MonochromeAlbumDetail album,
            string baseUrl, CancellationToken ct)
        {
            string manifestUrl = $"{baseUrl}/track/?id={track.Id}&quality={Options.Quality}";
            _logger.Trace("Fetching track manifest: {Url}", manifestUrl);

            HttpResponseMessage manifestResponse = await _httpClient.GetAsync(manifestUrl, ct);
            manifestResponse.EnsureSuccessStatusCode();

            string manifestJson = await manifestResponse.Content.ReadAsStringAsync(ct);
            MonochromeTrack? trackWithManifest = JsonSerializer.Deserialize<MonochromeTrack>(manifestJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (string.IsNullOrEmpty(trackWithManifest?.Manifest))
                throw new Exception($"No manifest returned for track {track.Id}");

            string decodedManifest = Encoding.UTF8.GetString(Convert.FromBase64String(trackWithManifest.Manifest));
            MonochromeManifest? manifest = JsonSerializer.Deserialize<MonochromeManifest>(decodedManifest,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            string? streamUrl = manifest?.Urls?.FirstOrDefault();
            if (string.IsNullOrEmpty(streamUrl))
                throw new Exception($"No stream URL in manifest for track {track.Id}");

            string codec = manifest?.Codecs ?? "flac";
            string ext = AudioFormatHelper.GetFileExtensionForCodec(codec);

            string trackFileName = $"{track.TrackNumber:D2} - {SanitizeFileName(track.Title ?? $"track{track.TrackNumber}")}{ext}";
            string dir = _destinationPath.FullPath;
            Directory.CreateDirectory(dir);
            string filePath = Path.Combine(dir, trackFileName);

            _logger.Debug("Downloading track {Number} '{Title}' → {Path}",
                track.TrackNumber, track.Title, filePath);

            using HttpResponseMessage streamResponse = await _httpClient.GetAsync(
                streamUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            streamResponse.EnsureSuccessStatusCode();
            using Stream contentStream = await streamResponse.Content.ReadAsStreamAsync(ct);
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write,
                FileShare.None, 81920, true);
            await contentStream.CopyToAsync(fileStream, ct);

            _logger.Debug("Finished track {Number} '{Title}'", track.TrackNumber, track.Title);
        }
    }
}

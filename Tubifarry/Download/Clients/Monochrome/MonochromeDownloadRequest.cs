using NzbDrone.Core.Parser.Model;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TagLib;
using Tubifarry.Core.Utilities;
using Tubifarry.Download.Base;
using Tubifarry.Indexers.Monochrome;
using NzbDrone.Core.Download;



namespace Tubifarry.Download.Clients.Monochrome
{
    public class MonochromeDownloadRequest : BaseDownloadRequest<MonochromeDownloadOptions>
    {
        private static readonly HttpClient _httpClient = new();
        private long _totalBytes = 0;
        private long _downloadedBytes = 0;
        private int _completedTracks = 0;
        private long _completedTrackBytes = 0;
        private MonochromeDownloadState _state = MonochromeDownloadState.Idle;
        private Task? _downloadTask;

        

        public MonochromeDownloadRequest(RemoteAlbum remoteAlbum, MonochromeDownloadOptions options)
            : base(remoteAlbum, options) { }

        public override void Start()
        {
            if (_state == MonochromeDownloadState.Running) return;
            _state = MonochromeDownloadState.Running;
            _downloadTask = Task.Run(async () =>
            {
                try
                {
                    await ProcessDownloadAsync(CancellationToken.None);
                    _state = MonochromeDownloadState.Completed;
                    _logger.Debug("Monochrome download completed for {Id}", Options.ItemId);
                }
                catch (Exception ex)
                {
                    _state = MonochromeDownloadState.Failed;
                    _logger.Error(ex, "Monochrome download failed for {Id}", Options.ItemId);
                }
            });
        }

        public override DownloadClientItem ClientItem
        {
            get
            {
                DownloadClientItem item = _clientItem;
                item.Status = GetDownloadItemStatus();
                item.Message = GetDistinctMessages();
                item.CanBeRemoved = _state == MonochromeDownloadState.Completed;
                item.CanMoveFiles = _state == MonochromeDownloadState.Completed;

                if (_expectedTrackCount <= 0)
                {
                    item.TotalSize = ReleaseInfo.Size;
                    item.RemainingSize = Math.Max(0, ReleaseInfo.Size - _downloadedBytes);
                    return item;
                }

                // Stable per-track byte estimate — use completed track average, fall back to ReleaseInfo
                long perTrack = _completedTracks > 0
                    ? _completedTrackBytes / _completedTracks
                    : (ReleaseInfo.Size > 0 ? ReleaseInfo.Size / _expectedTrackCount : 1024 * 1024 * 10);

                long totalSize = _expectedTrackCount * perTrack;

                // Current track in-progress bytes, capped at perTrack to avoid overshooting
                long inProgress = Math.Min(_downloadedBytes - _completedTrackBytes, perTrack);

                long remaining = Math.Max(0,
                    (_expectedTrackCount - _completedTracks) * perTrack - inProgress);

                item.TotalSize = totalSize;
                item.RemainingSize = remaining;
                item.RemainingTime = GetRemainingTime();
                return item;
            }
        }

        public override DownloadItemStatus GetDownloadItemStatus() => _state switch
        {
            MonochromeDownloadState.Idle      => DownloadItemStatus.Queued,
            MonochromeDownloadState.Running   => DownloadItemStatus.Downloading,
            MonochromeDownloadState.Completed => DownloadItemStatus.Completed,
            MonochromeDownloadState.Failed    => DownloadItemStatus.Failed,
            _                                 => DownloadItemStatus.Warning
        };

        protected override async Task ProcessDownloadAsync(CancellationToken token)
        {
            string baseUrl = Options.BaseUrl.TrimEnd('/');
            string albumId = Options.ItemId;

            MonochromeAlbumDetail? albumDetail = await FetchAlbumDetail(baseUrl, albumId, token);
            List<MonochromeTrack> tracks = albumDetail?.Items?
                .Where(i => i.Type == "track" && i.Item != null)
                .Select(i => i.Item!)
                .ToList() ?? new List<MonochromeTrack>();

            if (tracks.Count == 0)
                throw new Exception($"No tracks found for album {albumId}");

            _logger.Debug("Downloading {Count} tracks for album {Title}",
                tracks.Count, albumDetail!.Title);

            _expectedTrackCount = tracks.Count;

            foreach (MonochromeTrack track in tracks)
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
            MonochromeAlbumResponse? envelope = JsonSerializer.Deserialize<MonochromeAlbumResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return envelope?.Data;
        }

        private async Task DownloadTrack(MonochromeTrack track, MonochromeAlbumDetail album,
            string baseUrl, CancellationToken ct)
        {
            // Use LOSSLESS — HI_RES_LOSSLESS returns MPD XML requiring DASH parsing
            string quality = Options.Quality == "HI_RES_LOSSLESS" ? "LOSSLESS" : Options.Quality;

            string tidalUrl = $"https://api.tidal.com/v1/tracks/{track.Id}/playbackinfo" +
                $"?audioquality={quality}&playbackmode=STREAM&assetpresentation=FULL&countryCode={Options.CountryCode}";

            _logger.Debug("Fetching track playback info from Tidal: {Url} | Token present: {HasToken} | Token length: {Len}", tidalUrl, !string.IsNullOrEmpty(Options.TidalToken), Options.TidalToken?.Length ?? 0);

            using HttpRequestMessage request = new(HttpMethod.Get, tidalUrl);
            if (!string.IsNullOrEmpty(Options.TidalToken))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Options.TidalToken);

            HttpResponseMessage manifestResponse = await _httpClient.SendAsync(request, ct);
            manifestResponse.EnsureSuccessStatusCode();

            string responseJson = await manifestResponse.Content.ReadAsStringAsync(ct);

            // Tidal returns PlaybackInfo directly (no data envelope)
            TidalPlaybackInfo? playbackInfo = JsonSerializer.Deserialize<TidalPlaybackInfo>(responseJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            string? manifestBase64 = playbackInfo?.Manifest;
            string? mimeType = playbackInfo?.ManifestMimeType;

            if (string.IsNullOrEmpty(manifestBase64))
                throw new Exception($"No manifest returned for track {track.Id}");

            // Only handle BTS JSON manifests (LOSSLESS/AAC) — not MPD XML (HI_RES)
            if (mimeType != "application/vnd.tidal.bts")
                throw new Exception($"Unsupported manifest type '{mimeType}' for track {track.Id}. Use LOSSLESS quality.");

            string decodedManifest = Encoding.UTF8.GetString(Convert.FromBase64String(manifestBase64));
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

            // Track total size for progress reporting
            if (streamResponse.Content.Headers.ContentLength.HasValue)
                _totalBytes += streamResponse.Content.Headers.ContentLength.Value;

            using Stream contentStream = await streamResponse.Content.ReadAsStreamAsync(ct);
            using FileStream fileStream = new(filePath, FileMode.Create, FileAccess.Write,
                FileShare.None, 81920, true);

            // Copy with progress tracking
            byte[] buffer = new byte[81920];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                Interlocked.Add(ref _downloadedBytes, bytesRead);
            }
            fileStream.Close();

            long trackBytes = _downloadedBytes - _completedTrackBytes;
            Interlocked.Add(ref _completedTrackBytes, trackBytes);

            _completedTracks++;

            await EmbedTags(filePath, track, album, ct);

            _logger.Debug("Finished track {Number} '{Title}'", track.TrackNumber, track.Title);
        }
        private async Task EmbedTags(string filePath, MonochromeTrack track, MonochromeAlbumDetail album, CancellationToken ct)
        {
            try
            {
                using TagLib.File tagFile = TagLib.File.Create(filePath);

                tagFile.Tag.Title = track.Title ?? string.Empty;
                tagFile.Tag.Track = (uint)track.TrackNumber;
                tagFile.Tag.Disc = (uint)track.VolumeNumber;
                tagFile.Tag.Performers = [track.ArtistName];
                tagFile.Tag.AlbumArtists = [album.ArtistName];
                tagFile.Tag.Album = album.Title ?? string.Empty;
                tagFile.Tag.Year = ParseYear(album.ReleaseDate);

                // Embed cover art
                if (!string.IsNullOrEmpty(album.Cover))
                {
                    string coverUrl = $"https://resources.tidal.com/images/{album.Cover.Replace('-', '/')}/1280x1280.jpg";
                    try
                    {
                        byte[] coverBytes = await _httpClient.GetByteArrayAsync(coverUrl, ct);
                        tagFile.Tag.Pictures = [new TagLib.Picture(new TagLib.ByteVector(coverBytes))
                        {
                            Type = TagLib.PictureType.FrontCover,
                            MimeType = "image/jpeg"
                        }];
                    }
                    catch (Exception ex)
                    {
                        _logger.Warn("Failed to embed cover art for track {Id}: {Msg}", track.Id, ex.Message);
                    }
                }

                tagFile.Save();
                _logger.Trace("Tagged track {Number} '{Title}'", track.TrackNumber, track.Title);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Failed to tag file {Path}", filePath);
            }
        }

        private static uint ParseYear(string? releaseDate)
        {
            if (string.IsNullOrEmpty(releaseDate)) return 0;
            if (DateTime.TryParse(releaseDate, out DateTime d)) return (uint)d.Year;
            return 0;
        }

    }
}
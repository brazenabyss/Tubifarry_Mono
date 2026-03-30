using NLog;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using System.Text.Json;
using Tubifarry.Core.Model;
using Tubifarry.Core.Utilities;

namespace Tubifarry.Indexers.Monochrome
{
    public interface IMonochromeParser : IParseIndexerResponse { }

    public class MonochromeParser : IMonochromeParser
    {
        private readonly Logger _logger;

        public MonochromeParser(Logger logger) => _logger = logger;

        public IList<ReleaseInfo> ParseResponse(IndexerResponse indexerResponse)
        {
            List<ReleaseInfo> releases = new();
            try
            {
                MonochromeSearchResult? result = JsonSerializer.Deserialize<MonochromeSearchResult>(
                    indexerResponse.Content, IndexerParserHelper.StandardJsonOptions);

                if (result?.Albums?.Items == null || result.Albums.Items.Count == 0)
                {
                    _logger.Trace("No album results returned from Monochrome search");
                    return releases;
                }

                string quality = indexerResponse.Request.HttpRequest.Headers["X-Quality"] ?? MonochromeQuality.HI_RES_LOSSLESS.ToString();
                string baseUrl = $"{indexerResponse.Request.HttpRequest.Url.Scheme}://{indexerResponse.Request.HttpRequest.Url.Host}";

                IndexerParserHelper.ProcessItems(
                    result.Albums.Items,
                    album => MapAlbumToData(album, quality, baseUrl),
                    releases);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to parse Monochrome search response");
            }
            return releases;
        }

        private AlbumData MapAlbumToData(MonochromeAlbum album, string quality, string baseUrl)
        {
            (AudioFormat format, int bitrate, int bitDepth) = ResolveQuality(album.AudioQuality ?? quality);

            return new AlbumData("Monochrome", nameof(MonochromeDownloadProtocol))
            {
                Guid = $"Monochrome-album-{album.Id}",
                AlbumId = $"{baseUrl}/album/?id={album.Id}",
                AlbumName = album.Title ?? "Unknown Album",
                ArtistName = album.Artist?.Name ?? "Unknown Artist",
                InfoUrl = $"https://tidal.com/browse/album/{album.Id}",
                TotalTracks = album.NumberOfTracks,
                Duration = album.Duration,
                ReleaseDate = album.ReleaseDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ReleaseDatePrecision = "day",
                CustomString = album.Cover ?? string.Empty,
                Codec = format,
                Bitrate = bitrate,
                BitDepth = bitDepth,
                ExplicitContent = album.Explicit,
                Size = IndexerParserHelper.EstimateSize(0, album.Duration, bitrate, album.NumberOfTracks)
            };
        }

        private static (AudioFormat Format, int Bitrate, int BitDepth) ResolveQuality(string audioQuality) =>
            audioQuality switch
            {
                "HI_RES_LOSSLESS" => (AudioFormat.FLAC, 9216, 24),
                "LOSSLESS"        => (AudioFormat.FLAC, 1411, 16),
                "HIGH"            => (AudioFormat.AAC, 320, 0),
                _                 => (AudioFormat.AAC, 96, 0)
            };
    }
}

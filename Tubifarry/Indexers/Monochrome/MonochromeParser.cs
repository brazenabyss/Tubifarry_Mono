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
                MonochromeResponse? response = JsonSerializer.Deserialize<MonochromeResponse>(
                    indexerResponse.Content, IndexerParserHelper.StandardJsonOptions);

                List<MonochromeAlbum>? albums = response?.Data?.Albums?.Items;

                if (albums == null || albums.Count == 0)
                {
                    _logger.Trace("No album results from Monochrome search");
                    return releases;
                }

                // If the request carried an artist filter, narrow the results before processing
                string? artistFilter = indexerResponse.Request.HttpRequest.Headers["X-Artist-Filter"];
                if (!string.IsNullOrEmpty(artistFilter))
                {
                    int before = albums.Count;
                    albums = albums.Where(a => IsArtistMatch(a.ArtistName, artistFilter)).ToList();
                    _logger.Trace("Monochrome artist filter '{Filter}': {Before} → {After} results",
                        artistFilter, before, albums.Count);
                }

                string quality = indexerResponse.Request.HttpRequest.Headers["X-Quality"]
                    ?? MonochromeQuality.HI_RES_LOSSLESS.ToString();

                string baseUrl = $"{indexerResponse.Request.HttpRequest.Url.Scheme}://{indexerResponse.Request.HttpRequest.Url.Host}";

                IndexerParserHelper.ProcessItems(
                    albums,
                    album => MapAlbumToData(album, quality, baseUrl),
                    releases);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to parse Monochrome search response");
            }
            return releases;
        }

        /// <summary>
        /// Bidirectional case-insensitive contains match — handles both "Aer" matching "Aer"
        /// and edge cases where the stored name is a substring of the query or vice versa.
        /// Returns true if no artist name is present (don't filter out untagged results).
        /// </summary>
        private static bool IsArtistMatch(string? artistName, string filter) =>
            string.IsNullOrEmpty(artistName) ||
            artistName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
            filter.Contains(artistName, StringComparison.OrdinalIgnoreCase);

        private AlbumData MapAlbumToData(MonochromeAlbum album, string quality, string baseUrl)
        {
            // Prefer actual quality from API response, fall back to requested quality
            string effectiveQuality = album.IsHiRes ? "HI_RES_LOSSLESS"
                : album.AudioQuality ?? quality;

            (AudioFormat format, int bitrate, int bitDepth) = ResolveQuality(effectiveQuality);

            return new AlbumData("Monochrome", nameof(MonochromeDownloadProtocol))
            {
                Guid = $"Monochrome-album-{album.Id}",
                AlbumId = $"{baseUrl}/album/?id={album.Id}",
                AlbumName = album.Title ?? "Unknown Album",
                ArtistName = album.ArtistName,
                InfoUrl = $"https://tidal.com/browse/album/{album.Id}",
                TotalTracks = album.NumberOfTracks,
                Duration = album.Duration,
                ReleaseDate = album.ReleaseDate ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ReleaseDatePrecision = "day",
                CustomString = album.CoverUrl,
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
                "HI_RES_LOSSLESS" or "HIRES_LOSSLESS" => (AudioFormat.FLAC, 9216, 24),
                "LOSSLESS"                             => (AudioFormat.FLAC, 1411, 16),
                "HIGH"                                 => (AudioFormat.AAC, 320, 0),
                _                                      => (AudioFormat.AAC, 96, 0)
            };
    }
}

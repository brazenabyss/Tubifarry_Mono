using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using Tubifarry.Indexers.Monochrome;

namespace Tubifarry.Metadata.Proxy.MetadataProvider.Tidal
{
    public static class TidalMappingHelper
    {
        private const string Identifier = "@tidal";

        public static string ToForeignId(long id) => $"{id}{Identifier}";
        public static string? ExtractTidalId(string foreignId) =>
            foreignId.EndsWith(Identifier) ? foreignId[..^Identifier.Length] : null;

        public static Artist MapArtist(TidalArtistSearchItem item)
        {
            ArtistMetadata meta = new()
            {
                ForeignArtistId = ToForeignId(item.Id),
                Name = item.Name ?? string.Empty,
                Ratings = new Ratings { Votes = item.Popularity, Value = item.Popularity / 10m },
                Links = [new Links { Url = item.Url ?? $"https://tidal.com/artist/{item.Id}", Name = "Tidal" }],
                Images = string.IsNullOrEmpty(item.PictureUrl) ? [] :
                    [new MediaCover(MediaCoverTypes.Poster, item.PictureUrl)],
                Genres = [],
                Overview = $"Artist on Tidal"
            };
            return new Artist
            {
                ForeignArtistId = meta.ForeignArtistId,
                Name = meta.Name,
                Metadata = new(meta)
            };
        }

        public static Artist MapArtistFromData(TidalArtistData data, TidalArtistCover? cover,
            List<TidalAlbumSummary> albums)
        {
            string pictureUrl = cover?.Size750 ?? string.Empty;
            List<MediaCover> images = [];
            if (!string.IsNullOrEmpty(pictureUrl))
                images.Add(new MediaCover(MediaCoverTypes.Poster, pictureUrl));

            // Use first album cover as fanart if available
            TidalAlbumSummary? featuredAlbum = albums.OrderByDescending(a => a.Popularity).FirstOrDefault();
            if (featuredAlbum != null && !string.IsNullOrEmpty(featuredAlbum.CoverUrl))
                images.Add(new MediaCover(MediaCoverTypes.Fanart, featuredAlbum.CoverUrl));

            ArtistMetadata meta = new()
            {
                ForeignArtistId = ToForeignId(data.Id),
                Name = data.Name ?? string.Empty,
                Ratings = new Ratings { Votes = data.Popularity, Value = data.Popularity / 10m },
                Links = [new Links { Url = data.Url ?? $"https://tidal.com/artist/{data.Id}", Name = "Tidal" }],
                Images = images,
                Genres = [],
                Overview = $"Artist on Tidal with {albums.Count} releases."
            };

            return new Artist
            {
                ForeignArtistId = meta.ForeignArtistId,
                Name = meta.Name,
                Metadata = new(meta)
            };
        }

        public static Album MapAlbumFromSummary(TidalAlbumSummary album, Artist? artist = null)
        {
            Album result = new()
            {
                ForeignAlbumId = ToForeignId(album.Id),
                Title = album.Title ?? string.Empty,
                CleanTitle = album.Title.CleanArtistName(),
                ReleaseDate = ParseDate(album.ReleaseDate),
                AlbumType = MapAlbumType(album.Type),
                SecondaryTypes = [],
                Genres = [],
                AnyReleaseOk = true,
                Images = string.IsNullOrEmpty(album.CoverUrl) ? [] :
                    [new MediaCover(MediaCoverTypes.Cover, album.CoverUrl)],
                Links = [new Links { Url = album.Url ?? $"https://tidal.com/album/{album.Id}", Name = "Tidal" }],
                Overview = BuildAlbumOverview(album),
                Ratings = new Ratings { Votes = album.Popularity, Value = album.Popularity / 10m }
            };

            List<int> discs = Enumerable.Range(1, Math.Max(1, album.NumberOfVolumes)).ToList();
            AlbumRelease release = new()
            {
                ForeignReleaseId = ToForeignId(album.Id),
                Title = album.Title,
                ReleaseDate = ParseDate(album.ReleaseDate),
                Duration = album.Duration * 1000,
                TrackCount = album.NumberOfTracks,
                Status = "Official",
                Label = album.Copyright != null ? [album.Copyright] : [],
                Media = discs.Select(d => new Medium { Format = "Digital Media", Name = $"Disc {d}", Number = d }).ToList(),
                Album = result,
                Tracks = new(new List<Track>())
            };

            result.AlbumReleases = new([release]);

            if (artist != null)
            {
                result.Artist = artist;
                result.ArtistMetadata = artist.Metadata;
                result.ArtistMetadataId = artist.ArtistMetadataId;
            }

            return result;
        }

        public static Album MapAlbumFromDetail(MonochromeAlbumDetail detail, Artist? artist = null)
        {
            string coverUrl = string.IsNullOrEmpty(detail.Cover) ? string.Empty
                : $"https://resources.tidal.com/images/{detail.Cover.Replace('-', '/')}/1280x1280.jpg";

            Album result = new()
            {
                ForeignAlbumId = ToForeignId(detail.Id),
                Title = detail.Title ?? string.Empty,
                CleanTitle = detail.Title.CleanArtistName(),
                ReleaseDate = ParseDate(detail.ReleaseDate),
                AlbumType = "Album",
                SecondaryTypes = [],
                Genres = [],
                AnyReleaseOk = true,
                Images = string.IsNullOrEmpty(coverUrl) ? [] :
                    [new MediaCover(MediaCoverTypes.Cover, coverUrl)],
                Links = [new Links { Url = $"https://tidal.com/album/{detail.Id}", Name = "Tidal" }],
                Overview = $"Album on Tidal",
                Ratings = new Ratings()
            };

            List<Track> tracks = detail.Items?
                .Where(i => i.Type == "track" && i.Item != null)
                .Select((i, idx) => MapTrack(i.Item!, result))
                .ToList() ?? [];

            List<int> discNumbers = tracks.Select(t => t.MediumNumber).Distinct().OrderBy(x => x).ToList();
            if (discNumbers.Count == 0) discNumbers = [1];

            AlbumRelease release = new()
            {
                ForeignReleaseId = ToForeignId(detail.Id),
                Title = detail.Title,
                ReleaseDate = ParseDate(detail.ReleaseDate),
                Duration = detail.Duration * 1000,
                TrackCount = detail.NumberOfTracks,
                Status = "Official",
                Label = [],
                Media = discNumbers.Select(d => new Medium { Format = "Digital Media", Name = $"Disc {d}", Number = d }).ToList(),
                Album = result,
                Tracks = new(tracks)
            };

            result.AlbumReleases = new([release]);

            if (artist != null)
            {
                result.Artist = artist;
                result.ArtistMetadata = artist.Metadata;
                result.ArtistMetadataId = artist.ArtistMetadataId;
            }

            return result;
        }

        private static Track MapTrack(MonochromeTrack t, Album album) => new()
        {
            ForeignTrackId = ToForeignId(t.Id),
            Title = t.Title ?? string.Empty,
            TrackNumber = $"{t.TrackNumber}",
            AbsoluteTrackNumber = t.TrackNumber,
            MediumNumber = t.VolumeNumber,
            Duration = t.Duration * 1000,
            Explicit = false,
            Album = album
        };

        private static string MapAlbumType(string? type) => type?.ToUpperInvariant() switch
        {
            "SINGLE" => "Single",
            "EP" => "EP",
            _ => "Album"
        };

        private static string BuildAlbumOverview(TidalAlbumSummary album)
        {
            List<string> parts = [];
            if (album.NumberOfTracks > 0) parts.Add($"{album.NumberOfTracks} tracks");
            if (!string.IsNullOrEmpty(album.ReleaseDate)) parts.Add($"Released: {album.ReleaseDate}");
            if (!string.IsNullOrEmpty(album.Upc)) parts.Add($"UPC: {album.Upc}");
            return parts.Count > 0 ? string.Join(" • ", parts) : "Found on Tidal";
        }

        public static DateTime? ParseDate(string? date)
        {
            if (string.IsNullOrEmpty(date)) return null;
            return DateTime.TryParse(date, out DateTime d) ? d : null;
        }
    }
}

using Tubifarry.Download.Base;
using Tubifarry.Indexers.TripleTriple;

namespace Tubifarry.Download.Clients.TripleTriple
{
    public record TripleTripleDownloadOptions : BaseDownloadOptions
    {
        public string CountryCode { get; set; } = "US";
        public TripleTripleCodec Codec { get; set; } = TripleTripleCodec.FLAC;
        public bool DownloadLyrics { get; set; } = true;
        public bool CreateLrcFile { get; set; } = true;
        public bool EmbedLyrics { get; set; } = false;
        public int CoverSize { get; set; } = 1200;

        public TripleTripleDownloadOptions() : base() { }

        protected TripleTripleDownloadOptions(TripleTripleDownloadOptions options) : base(options)
        {
            CountryCode = options.CountryCode;
            Codec = options.Codec;
            DownloadLyrics = options.DownloadLyrics;
            CreateLrcFile = options.CreateLrcFile;
            EmbedLyrics = options.EmbedLyrics;
            CoverSize = options.CoverSize;
        }
    }
}

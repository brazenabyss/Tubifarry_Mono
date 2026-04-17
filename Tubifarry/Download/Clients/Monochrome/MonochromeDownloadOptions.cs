using Tubifarry.Download.Base;

namespace Tubifarry.Download.Clients.Monochrome
{
    public record MonochromeDownloadOptions : BaseDownloadOptions
    {
        public string Quality { get; set; } = "HI_RES_LOSSLESS";
        public string TidalToken { get; set; } = string.Empty;
        public string CountryCode { get; set; } = "US";
    }
}

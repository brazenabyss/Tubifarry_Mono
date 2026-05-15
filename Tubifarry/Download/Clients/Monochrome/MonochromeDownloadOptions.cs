using Tubifarry.Download.Base;

namespace Tubifarry.Download.Clients.Monochrome
{
    public record MonochromeDownloadOptions : BaseDownloadOptions
    {
        public string Quality { get; set; } = "HI_RES_LOSSLESS";
    }
}

using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Validation;

namespace Tubifarry.Download.Clients.Monochrome
{
    public class MonochromeProviderSettingsValidator : AbstractValidator<MonochromeProviderSettings>
    {
        public MonochromeProviderSettingsValidator()
        {
            RuleFor(x => x.BaseUrl).NotEmpty().WithMessage("A Monochrome/HiFi API instance URL is required.");
            RuleFor(x => x.DownloadPath).NotEmpty().WithMessage("A download path is required.");
            RuleFor(x => x.MaxDownloadSpeed).GreaterThanOrEqualTo(0);
            RuleFor(x => x.ConnectionRetries).InclusiveBetween(0, 10);
        }
    }

    public class MonochromeProviderSettings : NzbDrone.Core.ThingiProvider.IProviderConfig
    {
        private static readonly MonochromeProviderSettingsValidator _validator = new();

        public MonochromeProviderSettings()
        {
            BaseUrl = "https://frankfurt-1.monochrome.tf";
            Quality = (int)MonochromeQuality.HI_RES_LOSSLESS;
            DownloadPath = "/downloads/monochrome";
            MaxDownloadSpeed = 0;
            ConnectionRetries = 3;
            MaxParallelDownloads = 2;
        }

        [FieldDefinition(0, Label = "API Instance URL", Type = FieldType.Textbox,
            HelpText = "URL of the HiFi API instance (must match your indexer setting)",
            Placeholder = "https://hifi.402d65.dev")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Quality", Type = FieldType.Select,
            SelectOptions = typeof(MonochromeQuality),
            HelpText = "Audio quality for downloads")]
        public int Quality { get; set; }

        [FieldDefinition(2, Label = "Download Path", Type = FieldType.Path,
            HelpText = "Folder where Monochrome downloads will be saved")]
        public string DownloadPath { get; set; }

        [FieldDefinition(3, Label = "Max Download Speed", Type = FieldType.Number,
            Unit = "KB/s", HelpText = "Maximum download speed. 0 = unlimited", Advanced = true)]
        public int MaxDownloadSpeed { get; set; }

        [FieldDefinition(4, Label = "Connection Retries", Type = FieldType.Number,
            HelpText = "Number of times to retry a failed download", Advanced = true)]
        public int ConnectionRetries { get; set; }

        [FieldDefinition(5, Label = "Max Parallel Downloads", Type = FieldType.Number,
            HelpText = "Maximum number of tracks to download simultaneously", Advanced = true)]
        public int MaxParallelDownloads { get; set; }

        public NzbDroneValidationResult Validate() => new(_validator.Validate(this));
    }

    public enum MonochromeQuality
    {
        [FieldOption(Label = "Hi-Res Lossless (24-bit/192kHz)")]
        HI_RES_LOSSLESS,
        [FieldOption(Label = "Lossless (16-bit)")]
        LOSSLESS,
        [FieldOption(Label = "High (AAC 320kbps)")]
        HIGH,
        [FieldOption(Label = "Normal (AAC 96kbps)")]
        NORMAL
    }
}

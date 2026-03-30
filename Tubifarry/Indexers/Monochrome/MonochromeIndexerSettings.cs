using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Validation;

namespace Tubifarry.Indexers.Monochrome
{
    public class MonochromeIndexerSettingsValidator : AbstractValidator<MonochromeIndexerSettings>
    {
        public MonochromeIndexerSettingsValidator()
        {
            RuleFor(x => x.BaseUrl)
                .NotEmpty().WithMessage("A Monochrome/HiFi API instance URL is required.");
            RuleFor(x => x.SearchLimit)
                .InclusiveBetween(1, 100).WithMessage("Search limit must be between 1 and 100.");
            RuleFor(x => x.RequestTimeout)
                .InclusiveBetween(10, 300).WithMessage("Request timeout must be between 10 and 300 seconds.");
        }
    }

    public class MonochromeIndexerSettings : IIndexerSettings
    {
        private static readonly MonochromeIndexerSettingsValidator _validator = new();

        public MonochromeIndexerSettings()
        {
            BaseUrl = "https://hifi.402d65.dev";
            Quality = "HI_RES_LOSSLESS";
            SearchLimit = 20;
            RequestTimeout = 60;
        }

        [FieldDefinition(0, Label = "API Instance URL", Type = FieldType.Textbox,
            HelpText = "URL of the HiFi API instance (e.g. https://hifi.402d65.dev)",
            Placeholder = "https://hifi.402d65.dev")]
        public string BaseUrl { get; set; }

        [FieldDefinition(1, Label = "Quality", Type = FieldType.Select,
            SelectOptions = typeof(MonochromeQuality),
            HelpText = "Preferred audio quality tier")]
        public string Quality { get; set; }

        [FieldDefinition(2, Label = "Search Limit", Type = FieldType.Number,
            HelpText = "Maximum number of results per search", Advanced = true)]
        public int SearchLimit { get; set; }

        [FieldDefinition(3, Label = "Request Timeout", Type = FieldType.Number,
            Unit = "seconds", HelpText = "Timeout for API requests", Advanced = true)]
        public int RequestTimeout { get; set; }

        [FieldDefinition(4, Label = "Early Download Limit", Type = FieldType.Number,
            Unit = "days", HelpText = "Time before release date Lidarr will download from this indexer, empty is no limit",
            Advanced = true)]
        public int? EarlyReleaseLimit { get; set; }

        public NzbDroneValidationResult Validate() => new(_validator.Validate(this));
    }

    public enum MonochromeQuality
    {
        [FieldOption(Label = "Hi-Res Lossless (24-bit/192kHz)", Hint = "HI_RES_LOSSLESS")]
        HI_RES_LOSSLESS,

        [FieldOption(Label = "Lossless (16-bit)", Hint = "LOSSLESS")]
        LOSSLESS,

        [FieldOption(Label = "High (AAC 320kbps)", Hint = "HIGH")]
        HIGH,

        [FieldOption(Label = "Normal (AAC 96kbps)", Hint = "NORMAL")]
        NORMAL
    }
}

using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace Tubifarry.Metadata.Proxy.MetadataProvider.Tidal
{
    public class TidalMetadataProxySettingsValidator : AbstractValidator<TidalMetadataProxySettings>
    {
        public TidalMetadataProxySettingsValidator()
        {
            RuleFor(x => x.BaseUrl)
                .NotEmpty()
                .WithMessage("Base URL is required.");
        }
    }

    public class TidalMetadataProxySettings : IProviderConfig
    {
        private static readonly TidalMetadataProxySettingsValidator Validator = new();

        [FieldDefinition(1, Label = "Base URL", Section = MetadataSectionType.Metadata, Type = FieldType.Textbox,
            HelpText = "URL of your Monochrome/HiFi API instance.", Placeholder = "https://frankfurt-1.monochrome.tf")]
        public string BaseUrl { get; set; } = "https://frankfurt-1.monochrome.tf";

        public TidalMetadataProxySettings() => Instance = this;
        public static TidalMetadataProxySettings? Instance { get; private set; }
        public NzbDroneValidationResult Validate() => new(Validator.Validate(this));
    }
}

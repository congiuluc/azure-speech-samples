using Microsoft.Extensions.Configuration;

namespace SpeechSamples.Shared
{
    /// <summary>
    /// Configuration settings for Azure Speech Services.
    /// </summary>
    public class SpeechSettings
    {
        #region Properties

        /// <summary>
        /// Azure Speech Service subscription key.
        /// </summary>
        public string SubscriptionKey { get; set; } = string.Empty;

        /// <summary>
        /// Azure Speech Service region (e.g., "westeurope", "eastus").
        /// </summary>
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Default language for speech recognition (e.g., "it-IT", "en-US").
        /// </summary>
        public string Language { get; set; } = "it-IT";

        /// <summary>
        /// Target language for translation (e.g., "en", "de", "fr").
        /// </summary>
        public string TargetLanguage { get; set; } = "en";

        /// <summary>
        /// Voice name for text-to-speech (e.g., "it-IT-ElsaNeural").
        /// </summary>
        public string VoiceName { get; set; } = "it-IT-ElsaNeural";

        #endregion

        #region Factory Methods

        /// <summary>
        /// Loads speech settings from appsettings.json and environment variables.
        /// </summary>
        public static SpeechSettings Load()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "AZURE_SPEECH_")
                .Build();

            var settings = new SpeechSettings();
            configuration.GetSection("SpeechSettings").Bind(settings);

            // Allow environment variables to override
            var envKey = Environment.GetEnvironmentVariable("AZURE_SPEECH_KEY");
            var envRegion = Environment.GetEnvironmentVariable("AZURE_SPEECH_REGION");

            if (!string.IsNullOrEmpty(envKey))
                settings.SubscriptionKey = envKey;
            if (!string.IsNullOrEmpty(envRegion))
                settings.Region = envRegion;

            return settings;
        }

        /// <summary>
        /// Validates that required settings are present.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(SubscriptionKey))
                throw new InvalidOperationException(
                    "Speech subscription key is required. Set it in appsettings.json " +
                    "or via AZURE_SPEECH_KEY environment variable.");

            if (string.IsNullOrWhiteSpace(Region))
                throw new InvalidOperationException(
                    "Speech region is required. Set it in appsettings.json " +
                    "or via AZURE_SPEECH_REGION environment variable.");
        }

        #endregion
    }
}

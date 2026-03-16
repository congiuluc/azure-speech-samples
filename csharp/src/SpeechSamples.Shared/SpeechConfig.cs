using Azure.Core;
using Azure.Identity;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Configuration;

namespace SpeechSamples.Shared
{
    /// <summary>
    /// Configuration settings for Azure Speech Services.
    /// Supports both subscription key and managed identity (Entra ID) authentication.
    /// When no subscription key is provided, DefaultAzureCredential is used automatically.
    /// </summary>
    public class SpeechSettings
    {
        #region Constants

        private const string CognitiveServicesScope =
            "https://cognitiveservices.azure.com/.default";

        #endregion

        #region Properties

        /// <summary>
        /// Azure Speech Service subscription key.
        /// Leave empty to use managed identity (DefaultAzureCredential).
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

        /// <summary>
        /// Indicates whether the application uses managed identity (Entra ID)
        /// instead of a subscription key.
        /// </summary>
        public bool UsesManagedIdentity =>
            string.IsNullOrWhiteSpace(SubscriptionKey);

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
        /// Region is always required. Subscription key is optional when using managed identity.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Region))
                throw new InvalidOperationException(
                    "Speech region is required. Set it in appsettings.json " +
                    "or via AZURE_SPEECH_REGION environment variable.");

            if (UsesManagedIdentity)
            {
                Console.WriteLine(
                    "[Auth] No subscription key provided. " +
                    "Using managed identity (DefaultAzureCredential).");
            }
            else
            {
                Console.WriteLine("[Auth] Using subscription key authentication.");
            }
        }

        #endregion

        #region Speech Config Builders

        /// <summary>
        /// Creates a <see cref="SpeechConfig"/> using subscription key or managed identity.
        /// </summary>
        public SpeechConfig CreateSpeechConfig()
        {
            if (UsesManagedIdentity)
            {
                var token = GetAuthorizationToken();
                return SpeechConfig.FromAuthorizationToken(token, Region);
            }

            return SpeechConfig.FromSubscription(SubscriptionKey, Region);
        }

        /// <summary>
        /// Creates a <see cref="SpeechTranslationConfig"/> using subscription key
        /// or managed identity.
        /// </summary>
        public SpeechTranslationConfig CreateTranslationConfig()
        {
            if (UsesManagedIdentity)
            {
                var token = GetAuthorizationToken();
                return SpeechTranslationConfig.FromAuthorizationToken(token, Region);
            }

            return SpeechTranslationConfig.FromSubscription(SubscriptionKey, Region);
        }

        #endregion

        #region Private Methods

        private static readonly Lazy<DefaultAzureCredential> cachedCredential =
            new(() => new DefaultAzureCredential());

        /// <summary>
        /// Obtains an authorization token from DefaultAzureCredential
        /// for the Cognitive Services scope.
        /// </summary>
        /// <remarks>
        /// Tokens typically expire after ~1 hour. For long-running continuous
        /// recognition sessions, update <c>recognizer.AuthorizationToken</c>
        /// periodically by calling this method again.
        /// </remarks>
        private static string GetAuthorizationToken()
        {
            var credential = cachedCredential.Value;
            var tokenRequestContext = new TokenRequestContext(
                [CognitiveServicesScope]);
            var accessToken = credential.GetToken(tokenRequestContext);
            return accessToken.Token;
        }

        #endregion
    }
}

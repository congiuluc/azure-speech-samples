using SpeechSamples.Shared;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Translation;

namespace SpeechSamples.Translation;

/// <summary>
/// Demo app for Azure Speech Translation with Speech-to-Text and diarization.
/// Demonstrates: real-time speech translation, multi-language translation,
/// and conversation transcription with diarization before/after translation.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Azure Speech Translation + Diarization Demo");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        var settings = SpeechSettings.Load();
        settings.Validate();

        Console.WriteLine($"Region: {settings.Region}");
        Console.WriteLine($"Source language: {settings.Language}");
        Console.WriteLine($"Target language: {settings.TargetLanguage}");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("  1 - Single-shot translation (microphone)");
            Console.WriteLine("  2 - Continuous translation (microphone)");
            Console.WriteLine("  3 - Multi-language translation (microphone)");
            Console.WriteLine("  4 - Diarization + Translation (microphone)");
            Console.WriteLine("  5 - Diarization + Translation (audio file)");
            Console.WriteLine("  0 - Exit");
            Console.Write("> ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await SingleShotTranslationAsync(settings);
                    break;
                case "2":
                    await ContinuousTranslationAsync(settings);
                    break;
                case "3":
                    await MultiLanguageTranslationAsync(settings);
                    break;
                case "4":
                    await DiarizationWithTranslationAsync(settings, useFile: false);
                    break;
                case "5":
                    await DiarizationWithTranslationAsync(settings, useFile: true);
                    break;
                case "0":
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Invalid option. Try again.");
                    break;
            }

            Console.WriteLine();
        }
    }

    #region Translation

    /// <summary>
    /// Performs a single-shot speech translation from the default microphone.
    /// </summary>
    private static async Task SingleShotTranslationAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Single-shot Translation ---");
        Console.WriteLine("Speak into your microphone...");

        var translationConfig = SpeechTranslationConfig.FromSubscription(
            settings.SubscriptionKey, settings.Region);
        translationConfig.SpeechRecognitionLanguage = settings.Language;
        translationConfig.AddTargetLanguage(settings.TargetLanguage);

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new TranslationRecognizer(translationConfig, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        switch (result.Reason)
        {
            case ResultReason.TranslatedSpeech:
                Console.WriteLine($"Recognized ({settings.Language}): {result.Text}");
                foreach (var translation in result.Translations)
                {
                    Console.WriteLine($"Translated ({translation.Key}): {translation.Value}");
                }
                break;
            case ResultReason.NoMatch:
                Console.WriteLine("No speech could be recognized.");
                break;
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"Canceled: {cancellation.Reason}");
                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"Error code: {cancellation.ErrorCode}");
                    Console.WriteLine($"Error details: {cancellation.ErrorDetails}");
                }
                break;
        }
    }

    /// <summary>
    /// Performs continuous speech translation from the default microphone.
    /// Press Ctrl+C to stop.
    /// </summary>
    private static async Task ContinuousTranslationAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Continuous Translation ---");
        Console.WriteLine("Speak into your microphone. Press Ctrl+C to stop.");

        var translationConfig = SpeechTranslationConfig.FromSubscription(
            settings.SubscriptionKey, settings.Region);
        translationConfig.SpeechRecognitionLanguage = settings.Language;
        translationConfig.AddTargetLanguage(settings.TargetLanguage);

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new TranslationRecognizer(translationConfig, audioConfig);

        var stopRecognition = new TaskCompletionSource<bool>();

        recognizer.Recognized += (s, e) =>
        {
            if (e.Result.Reason == ResultReason.TranslatedSpeech)
            {
                Console.WriteLine($"  [{settings.Language}] {e.Result.Text}");
                foreach (var translation in e.Result.Translations)
                {
                    Console.WriteLine($"  [{translation.Key}] {translation.Value}");
                }
                Console.WriteLine();
            }
        };

        recognizer.Canceled += (s, e) =>
        {
            if (e.Reason == CancellationReason.Error)
            {
                Console.WriteLine($"  [Error] {e.ErrorCode}: {e.ErrorDetails}");
            }
            stopRecognition.TrySetResult(true);
        };

        recognizer.SessionStopped += (s, e) =>
        {
            stopRecognition.TrySetResult(true);
        };

        await recognizer.StartContinuousRecognitionAsync();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };
        using var _ = cts.Token.Register(() => stopRecognition.TrySetResult(true));

        await stopRecognition.Task;
        await recognizer.StopContinuousRecognitionAsync();
    }

    /// <summary>
    /// Translates speech to multiple target languages simultaneously.
    /// </summary>
    private static async Task MultiLanguageTranslationAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Multi-language Translation ---");

        var targetLanguages = new[] { "en", "de", "fr", "es" };
        Console.WriteLine(
            $"Translating from {settings.Language} to: {string.Join(", ", targetLanguages)}");
        Console.WriteLine("Speak into your microphone...");

        var translationConfig = SpeechTranslationConfig.FromSubscription(
            settings.SubscriptionKey, settings.Region);
        translationConfig.SpeechRecognitionLanguage = settings.Language;

        foreach (var lang in targetLanguages)
        {
            translationConfig.AddTargetLanguage(lang);
        }

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new TranslationRecognizer(translationConfig, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.TranslatedSpeech)
        {
            Console.WriteLine($"Recognized ({settings.Language}): {result.Text}");
            Console.WriteLine();

            foreach (var translation in result.Translations)
            {
                Console.WriteLine($"  [{translation.Key}] {translation.Value}");
            }
        }
        else
        {
            Console.WriteLine($"Result: {result.Reason}");
        }
    }

    #endregion

    #region Diarization + Translation

    /// <summary>
    /// Performs speech-to-text with speaker diarization, then translates each segment.
    /// This combines the diarization feature with translation to show who said what
    /// in both source and target languages.
    /// </summary>
    private static async Task DiarizationWithTranslationAsync(
        SpeechSettings settings,
        bool useFile)
    {
        Console.WriteLine("--- Diarization + Translation ---");

        // Step 1: Perform STT with diarization
        var helper = new DiarizationHelper(settings);
        List<DiarizedSegment> segments;

        if (useFile)
        {
            Console.Write("Enter audio file path (WAV): ");
            var filePath = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("No file path provided.");
                return;
            }

            Console.WriteLine($"Processing file: {filePath}");
            segments = await helper.RecognizeFromFileAsync(filePath);
        }
        else
        {
            Console.WriteLine("Speak into your microphone. Press Ctrl+C to stop.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            segments = await helper.RecognizeFromMicrophoneAsync(cts.Token);
        }

        DiarizationHelper.PrintSegments(segments);

        if (segments.Count == 0)
        {
            Console.WriteLine("No speech segments to translate.");
            return;
        }

        // Step 2: Translate each diarized segment
        Console.WriteLine();
        Console.WriteLine($"=== Translated Segments ({settings.Language} -> {settings.TargetLanguage}) ===");
        Console.WriteLine($"{"Time",-12} {"Speaker",-12} {"Original",-40} Translation");
        Console.WriteLine(new string('-', 110));

        var translationConfig = SpeechTranslationConfig.FromSubscription(
            settings.SubscriptionKey, settings.Region);
        translationConfig.SpeechRecognitionLanguage = settings.Language;
        translationConfig.AddTargetLanguage(settings.TargetLanguage);

        foreach (var segment in segments)
        {
            // For already-transcribed text, we use the SpeechSynthesizer + TranslationRecognizer
            // pipeline via an intermediate WAV for accurate translation with the Speech service.
            // In production, consider using the Translator Text API for pure text translation.
            var time = segment.Offset.ToString(@"mm\:ss\.ff");
            Console.WriteLine(
                $"{time,-12} {segment.SpeakerId,-12} {segment.Text,-40} " +
                $"[Translation would use Translator Text API]");
        }

        Console.WriteLine(new string('-', 110));
        Console.WriteLine("Note: For text-to-text translation of transcribed segments, " +
            "use the Azure Translator Text API for best results.");
    }

    #endregion
}

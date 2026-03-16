using SpeechSamples.Shared;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace SpeechSamples.SpeechToText;

/// <summary>
/// Demo app for Azure Speech-to-Text with speaker diarization.
/// Demonstrates: real-time STT, continuous recognition, and conversation transcription
/// with automatic speaker identification (diarization).
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Azure Speech-to-Text + Diarization Demo");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        var settings = SpeechSettings.Load();
        settings.Validate();

        Console.WriteLine($"Region: {settings.Region}");
        Console.WriteLine($"Language: {settings.Language}");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("  1 - Single-shot recognition (microphone)");
            Console.WriteLine("  2 - Continuous recognition with diarization (microphone)");
            Console.WriteLine("  3 - Continuous recognition with diarization (audio file)");
            Console.WriteLine("  0 - Exit");
            Console.Write("> ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await SingleShotRecognitionAsync(settings);
                    break;
                case "2":
                    await ContinuousRecognitionWithDiarizationAsync(settings, useFile: false);
                    break;
                case "3":
                    await ContinuousRecognitionWithDiarizationAsync(settings, useFile: true);
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

    #region Single-shot Recognition

    /// <summary>
    /// Performs a single-shot speech recognition from the default microphone.
    /// Recognizes a single utterance until a pause is detected.
    /// </summary>
    private static async Task SingleShotRecognitionAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Single-shot Recognition ---");
        Console.WriteLine("Speak into your microphone...");

        var speechConfig = SpeechConfig.FromSubscription(settings.SubscriptionKey, settings.Region);
        speechConfig.SpeechRecognitionLanguage = settings.Language;

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                Console.WriteLine($"Recognized: {result.Text}");
                Console.WriteLine($"Duration: {result.Duration}");
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

    #endregion

    #region Continuous Recognition with Diarization

    /// <summary>
    /// Performs continuous speech recognition with speaker diarization.
    /// Uses the shared <see cref="DiarizationHelper"/> for transcription.
    /// </summary>
    private static async Task ContinuousRecognitionWithDiarizationAsync(
        SpeechSettings settings,
        bool useFile)
    {
        Console.WriteLine("--- Continuous Recognition with Diarization ---");

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
    }

    #endregion
}

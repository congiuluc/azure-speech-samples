using SpeechSamples.Shared;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;

namespace SpeechSamples.SpeakerRecognition;

/// <summary>
/// Demo app for Azure Conversation Transcription and Pronunciation Assessment
/// with Speech-to-Text and speaker diarization.
/// Demonstrates: multi-speaker conversation transcription, speaker identification
/// via diarization, pronunciation assessment scoring, and keyword recognition.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("================================================");
        Console.WriteLine("  Azure Conversation Transcription + ");
        Console.WriteLine("  Pronunciation Assessment + Diarization Demo");
        Console.WriteLine("================================================");
        Console.WriteLine();

        var settings = SpeechSettings.Load();
        settings.Validate();

        Console.WriteLine($"Region: {settings.Region}");
        Console.WriteLine($"Language: {settings.Language}");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("  1 - Conversation transcription with diarization (microphone)");
            Console.WriteLine("  2 - Conversation transcription with diarization (audio file)");
            Console.WriteLine("  3 - Pronunciation assessment (microphone)");
            Console.WriteLine("  4 - Pronunciation assessment (audio file)");
            Console.WriteLine("  5 - Keyword recognition + STT with diarization");
            Console.WriteLine("  0 - Exit");
            Console.Write("> ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await ConversationTranscriptionAsync(settings, useFile: false);
                    break;
                case "2":
                    await ConversationTranscriptionAsync(settings, useFile: true);
                    break;
                case "3":
                    await PronunciationAssessmentAsync(settings, useFile: false);
                    break;
                case "4":
                    await PronunciationAssessmentAsync(settings, useFile: true);
                    break;
                case "5":
                    await KeywordRecognitionWithDiarizationAsync(settings);
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

    #region Conversation Transcription

    /// <summary>
    /// Performs multi-speaker conversation transcription with diarization.
    /// Uses the shared <see cref="DiarizationHelper"/> for speaker-labeled transcription.
    /// </summary>
    private static async Task ConversationTranscriptionAsync(SpeechSettings settings, bool useFile)
    {
        Console.WriteLine("--- Conversation Transcription with Diarization ---");

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
            Console.WriteLine("Start a conversation with multiple speakers.");
            Console.WriteLine("Press Ctrl+C to stop.");
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            segments = await helper.RecognizeFromMicrophoneAsync(cts.Token);
        }

        DiarizationHelper.PrintSegments(segments);

        // Print summary statistics
        if (segments.Count > 0)
        {
            PrintConversationSummary(segments);
        }
    }

    /// <summary>
    /// Prints a summary of the conversation: speaker stats, word counts, and timing.
    /// </summary>
    private static void PrintConversationSummary(List<DiarizedSegment> segments)
    {
        Console.WriteLine();
        Console.WriteLine("=== Conversation Summary ===");

        var bySpeaker = segments.GroupBy(s => s.SpeakerId).ToList();
        Console.WriteLine($"Total speakers: {bySpeaker.Count}");
        Console.WriteLine($"Total segments: {segments.Count}");
        Console.WriteLine();

        foreach (var group in bySpeaker.OrderBy(g => g.Key))
        {
            var wordCount = group.Sum(s => s.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            var totalDuration = group.Aggregate(TimeSpan.Zero, (sum, s) => sum + s.Duration);
            Console.WriteLine($"  {group.Key}:");
            Console.WriteLine($"    Segments: {group.Count()}");
            Console.WriteLine($"    Words: {wordCount}");
            Console.WriteLine($"    Total speaking time: {totalDuration:mm\\:ss\\.ff}");
        }
    }

    #endregion

    #region Pronunciation Assessment

    /// <summary>
    /// Performs pronunciation assessment using the Azure Speech SDK.
    /// Evaluates accuracy, fluency, completeness, and pronunciation scores.
    /// Also includes STT with diarization for context.
    /// </summary>
    private static async Task PronunciationAssessmentAsync(SpeechSettings settings, bool useFile)
    {
        Console.WriteLine("--- Pronunciation Assessment ---");

        Console.Write("Enter the reference text to assess pronunciation against: ");
        var referenceText = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(referenceText))
        {
            referenceText = "Buongiorno, come stai oggi? Spero che tu stia bene.";
            Console.WriteLine($"Using default reference text: \"{referenceText}\"");
        }

        var speechConfig = SpeechConfig.FromSubscription(settings.SubscriptionKey, settings.Region);
        speechConfig.SpeechRecognitionLanguage = settings.Language;

        var pronunciationConfig = new PronunciationAssessmentConfig(
            referenceText,
            GradingSystem.HundredMark,
            Granularity.Phoneme,
            enableMiscue: true);

        AudioConfig audioConfig;
        if (useFile)
        {
            Console.Write("Enter audio file path (WAV): ");
            var filePath = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(filePath))
            {
                Console.WriteLine("No file path provided.");
                return;
            }
            audioConfig = AudioConfig.FromWavFileInput(filePath);
        }
        else
        {
            Console.WriteLine($"Read the following text aloud:");
            Console.WriteLine($"  \"{referenceText}\"");
            Console.WriteLine();
            Console.WriteLine("Speak now...");
            audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        }

        using (audioConfig)
        using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
        {
            pronunciationConfig.ApplyTo(recognizer);

            var result = await recognizer.RecognizeOnceAsync();

            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                Console.WriteLine();
                Console.WriteLine($"Recognized text: {result.Text}");

                var assessment = PronunciationAssessmentResult.FromResult(result);
                Console.WriteLine();
                Console.WriteLine("=== Pronunciation Assessment Results ===");
                Console.WriteLine($"  Accuracy score:     {assessment.AccuracyScore:F1}/100");
                Console.WriteLine($"  Fluency score:      {assessment.FluencyScore:F1}/100");
                Console.WriteLine($"  Completeness score: {assessment.CompletenessScore:F1}/100");
                Console.WriteLine($"  Pronunciation score:{assessment.PronunciationScore:F1}/100");

                Console.WriteLine();
                Console.WriteLine("Word-level details:");
                Console.WriteLine($"  {"Word",-20} {"Accuracy",-12} {"Error Type"}");
                Console.WriteLine(new string('-', 50));

                foreach (var word in assessment.Words)
                {
                    Console.WriteLine(
                        $"  {word.Word,-20} {word.AccuracyScore,-12:F1} {word.ErrorType}");
                }
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Console.WriteLine("No speech was recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = CancellationDetails.FromResult(result);
                Console.WriteLine($"Canceled: {cancellation.Reason}");
                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"Error: {cancellation.ErrorDetails}");
                }
            }
        }

        // Also run diarization for context
        Console.WriteLine();
        Console.WriteLine("--- Running STT with diarization for additional context ---");
        if (useFile)
        {
            Console.Write("Re-process same file with diarization? (y/N): ");
            var answer = Console.ReadLine()?.Trim();
            if (string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase))
            {
                Console.Write("Enter audio file path (WAV): ");
                var diarizePath = Console.ReadLine()?.Trim();
                if (!string.IsNullOrEmpty(diarizePath))
                {
                    var helper = new DiarizationHelper(settings);
                    var segments = await helper.RecognizeFromFileAsync(diarizePath);
                    DiarizationHelper.PrintSegments(segments);
                }
            }
        }
    }

    #endregion

    #region Keyword Recognition

    /// <summary>
    /// Demonstrates keyword spotting: listens for a keyword before starting
    /// full STT with diarization. This is useful for wake-word scenarios.
    /// </summary>
    private static async Task KeywordRecognitionWithDiarizationAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Keyword Recognition + STT with Diarization ---");
        Console.WriteLine();
        Console.WriteLine("Note: Keyword recognition requires a keyword model file (.table).");
        Console.WriteLine("You can create one at https://speech.microsoft.com/portal");
        Console.WriteLine();

        Console.Write("Enter keyword model file path (.table), or press Enter to skip to STT: ");
        var keywordModelPath = Console.ReadLine()?.Trim();

        if (!string.IsNullOrEmpty(keywordModelPath) && File.Exists(keywordModelPath))
        {
            var speechConfig = SpeechConfig.FromSubscription(settings.SubscriptionKey, settings.Region);
            speechConfig.SpeechRecognitionLanguage = settings.Language;

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            var model = KeywordRecognitionModel.FromFile(keywordModelPath);

            Console.WriteLine("Listening for keyword... Speak the keyword to activate.");

            var result = await recognizer.RecognizeOnceAsync();
            if (result.Reason == ResultReason.RecognizedKeyword)
            {
                Console.WriteLine($"Keyword detected: {result.Text}");
                Console.WriteLine("Starting full STT with diarization...");
            }
        }
        else
        {
            Console.WriteLine("No keyword model provided. Starting STT with diarization directly.");
        }

        // Run diarization
        Console.WriteLine();
        Console.WriteLine("Speak into your microphone. Press Ctrl+C to stop.");
        var helper = new DiarizationHelper(settings);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var segments = await helper.RecognizeFromMicrophoneAsync(cts.Token);
        DiarizationHelper.PrintSegments(segments);

        if (segments.Count > 0)
        {
            PrintConversationSummary(segments);
        }
    }

    #endregion
}

using SpeechSamples.Shared;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

namespace SpeechSamples.TextToSpeech;

/// <summary>
/// Demo app for Azure Text-to-Speech with Speech-to-Text verification and diarization.
/// Demonstrates: neural TTS synthesis, SSML support, voice listing,
/// and a round-trip scenario (STT with diarization -> TTS -> STT verification).
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("  Azure Text-to-Speech + Diarization Demo");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        var settings = SpeechSettings.Load();
        settings.Validate();

        Console.WriteLine($"Region: {settings.Region}");
        Console.WriteLine($"Voice: {settings.VoiceName}");
        Console.WriteLine($"Language: {settings.Language}");
        Console.WriteLine();

        while (true)
        {
            Console.WriteLine("Select an option:");
            Console.WriteLine("  1 - Synthesize text to speaker");
            Console.WriteLine("  2 - Synthesize text to WAV file");
            Console.WriteLine("  3 - Synthesize with SSML");
            Console.WriteLine("  4 - List available voices");
            Console.WriteLine("  5 - Round-trip: STT with diarization -> TTS -> STT verification");
            Console.WriteLine("  0 - Exit");
            Console.Write("> ");

            var choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    await SynthesizeToSpeakerAsync(settings);
                    break;
                case "2":
                    await SynthesizeToFileAsync(settings);
                    break;
                case "3":
                    await SynthesizeWithSsmlAsync(settings);
                    break;
                case "4":
                    await ListVoicesAsync(settings);
                    break;
                case "5":
                    await RoundTripDemoAsync(settings);
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

    #region Text-to-Speech

    /// <summary>
    /// Synthesizes text to the default speaker output.
    /// </summary>
    private static async Task SynthesizeToSpeakerAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Synthesize to Speaker ---");
        Console.Write("Enter text to synthesize: ");
        var text = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine("No text provided.");
            return;
        }

        var speechConfig = settings.CreateSpeechConfig();
        speechConfig.SpeechSynthesisVoiceName = settings.VoiceName;

        using var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

        var result = await synthesizer.SpeakTextAsync(text);
        HandleSynthesisResult(result);
    }

    /// <summary>
    /// Synthesizes text to a WAV file.
    /// </summary>
    private static async Task SynthesizeToFileAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Synthesize to WAV File ---");
        Console.Write("Enter text to synthesize: ");
        var text = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            Console.WriteLine("No text provided.");
            return;
        }

        Console.Write("Enter output file path (e.g., output.wav): ");
        var outputPath = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(outputPath))
        {
            outputPath = "output.wav";
        }

        var speechConfig = settings.CreateSpeechConfig();
        speechConfig.SpeechSynthesisVoiceName = settings.VoiceName;

        using var audioConfig = AudioConfig.FromWavFileOutput(outputPath);
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

        var result = await synthesizer.SpeakTextAsync(text);
        HandleSynthesisResult(result);

        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
        {
            Console.WriteLine($"Audio saved to: {Path.GetFullPath(outputPath)}");
        }
    }

    /// <summary>
    /// Synthesizes text using SSML for fine-grained control over pronunciation,
    /// pitch, rate, volume, and pauses.
    /// </summary>
    private static async Task SynthesizeWithSsmlAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Synthesize with SSML ---");
        var ssml = $@"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{settings.Language}'>
    <voice name='{settings.VoiceName}'>
        <prosody rate='medium' pitch='default'>
            Benvenuti alla demo di sintesi vocale di Azure.
        </prosody>
        <break time='500ms'/>
        <prosody rate='slow' pitch='low'>
            Questo testo viene pronunciato più lentamente e con un tono più basso.
        </prosody>
        <break time='500ms'/>
        <prosody rate='fast' pitch='high'>
            E questo più velocemente con un tono più alto!
        </prosody>
    </voice>
</speak>";

        Console.WriteLine("SSML content:");
        Console.WriteLine(ssml);
        Console.WriteLine();

        var speechConfig = settings.CreateSpeechConfig();

        using var audioConfig = AudioConfig.FromDefaultSpeakerOutput();
        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);

        var result = await synthesizer.SpeakSsmlAsync(ssml);
        HandleSynthesisResult(result);
    }

    /// <summary>
    /// Lists all available neural voices for the configured region.
    /// </summary>
    private static async Task ListVoicesAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Available Voices ---");
        var speechConfig = settings.CreateSpeechConfig();

        using var synthesizer = new SpeechSynthesizer(speechConfig, null);
        var voicesResult = await synthesizer.GetVoicesAsync();

        if (voicesResult.Reason == ResultReason.VoicesListRetrieved)
        {
            var voices = voicesResult.Voices
                .Where(v => v.Locale.StartsWith(settings.Language[..2], StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v.ShortName)
                .ToList();

            Console.WriteLine(
                $"Found {voices.Count} voices for locale '{settings.Language[..2]}':");

            foreach (var voice in voices)
            {
                Console.WriteLine(
                    $"  {voice.ShortName,-30} {voice.Gender,-8} {voice.VoiceType}");
            }
        }
        else
        {
            Console.WriteLine("Failed to retrieve voice list.");
        }
    }

    #endregion

    #region Round-trip Demo (STT + Diarization -> TTS -> STT)

    /// <summary>
    /// Demonstrates a round-trip:
    /// 1. Captures speech from microphone with diarization (STT).
    /// 2. Synthesizes recognized text back as audio (TTS).
    /// 3. Re-recognizes the synthesized audio (STT verification).
    /// </summary>
    private static async Task RoundTripDemoAsync(SpeechSettings settings)
    {
        Console.WriteLine("--- Round-trip Demo: STT + Diarization -> TTS -> STT ---");
        Console.WriteLine();

        // Step 1: Speech-to-Text with diarization
        Console.WriteLine("[Step 1] Speak into your microphone. Press Ctrl+C to stop.");
        var helper = new DiarizationHelper(settings);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var segments = await helper.RecognizeFromMicrophoneAsync(cts.Token);
        DiarizationHelper.PrintSegments(segments);

        if (segments.Count == 0)
        {
            Console.WriteLine("No speech was recognized. Cannot continue round-trip.");
            return;
        }

        // Step 2: Synthesize the recognized text to a WAV file
        var combinedText = string.Join(" ", segments.Select(s => s.Text));
        Console.WriteLine();
        Console.WriteLine($"[Step 2] Synthesizing combined text: \"{combinedText}\"");

        var tempWav = Path.Combine(Path.GetTempPath(), $"roundtrip_{Guid.NewGuid()}.wav");
        var speechConfig = settings.CreateSpeechConfig();
        speechConfig.SpeechSynthesisVoiceName = settings.VoiceName;

        using (var audioConfig = AudioConfig.FromWavFileOutput(tempWav))
        using (var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig))
        {
            var result = await synthesizer.SpeakTextAsync(combinedText);
            HandleSynthesisResult(result);
        }

        // Step 3: Re-transcribe the synthesized audio with diarization
        Console.WriteLine();
        Console.WriteLine("[Step 3] Re-transcribing synthesized audio with diarization...");
        var verificationSegments = await helper.RecognizeFromFileAsync(tempWav);
        DiarizationHelper.PrintSegments(verificationSegments);

        // Cleanup temp file
        try { File.Delete(tempWav); }
        catch { /* best effort cleanup */ }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Handles and displays the result of a speech synthesis operation.
    /// </summary>
    private static void HandleSynthesisResult(SpeechSynthesisResult result)
    {
        switch (result.Reason)
        {
            case ResultReason.SynthesizingAudioCompleted:
                Console.WriteLine("Synthesis completed successfully.");
                Console.WriteLine($"Audio length: {result.AudioData.Length} bytes");
                break;
            case ResultReason.Canceled:
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"Synthesis canceled: {cancellation.Reason}");
                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"Error code: {cancellation.ErrorCode}");
                    Console.WriteLine($"Error details: {cancellation.ErrorDetails}");
                }
                break;
        }
    }

    #endregion
}

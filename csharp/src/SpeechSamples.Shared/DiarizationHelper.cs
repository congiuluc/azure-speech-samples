using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;

namespace SpeechSamples.Shared
{
    /// <summary>
    /// Shared helper for Speech-to-Text recognition with speaker diarization.
    /// All demo apps use this to provide consistent STT + diarization functionality.
    /// </summary>
    public class DiarizationHelper
    {
        #region Fields

        private readonly SpeechSettings settings;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of <see cref="DiarizationHelper"/>.
        /// </summary>
        /// <param name="settings">Azure Speech settings.</param>
        public DiarizationHelper(SpeechSettings settings)
        {
            this.settings = settings;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Performs continuous speech-to-text recognition with speaker diarization from a file.
        /// </summary>
        /// <param name="audioFilePath">Path to the audio file (WAV format).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recognized segments with speaker information.</returns>
        public async Task<List<DiarizedSegment>> RecognizeFromFileAsync(
            string audioFilePath,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Audio file not found.", audioFilePath);

            var speechConfig = settings.CreateSpeechConfig();
            speechConfig.SpeechRecognitionLanguage = settings.Language;
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

            using var audioConfig = AudioConfig.FromWavFileInput(audioFilePath);
            return await RunConversationTranscriptionAsync(speechConfig, audioConfig, cancellationToken);
        }

        /// <summary>
        /// Performs continuous speech-to-text recognition with speaker diarization from the default microphone.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of recognized segments with speaker information.</returns>
        public async Task<List<DiarizedSegment>> RecognizeFromMicrophoneAsync(
            CancellationToken cancellationToken = default)
        {
            var speechConfig = settings.CreateSpeechConfig();
            speechConfig.SpeechRecognitionLanguage = settings.Language;
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true");

            using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            return await RunConversationTranscriptionAsync(speechConfig, audioConfig, cancellationToken);
        }

        /// <summary>
        /// Prints diarized segments to the console in a formatted way.
        /// </summary>
        /// <param name="segments">The list of diarized segments.</param>
        public static void PrintSegments(List<DiarizedSegment> segments)
        {
            Console.WriteLine();
            Console.WriteLine("=== Diarization Results ===");
            Console.WriteLine($"{"Time",-12} {"Speaker",-12} Text");
            Console.WriteLine(new string('-', 80));

            foreach (var segment in segments)
            {
                var time = segment.Offset.ToString(@"mm\:ss\.ff");
                Console.WriteLine($"{time,-12} {segment.SpeakerId,-12} {segment.Text}");
            }

            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"Total segments: {segments.Count}");

            var speakers = segments.Select(s => s.SpeakerId).Distinct().ToList();
            Console.WriteLine($"Speakers identified: {string.Join(", ", speakers)}");
        }

        #endregion

        #region Private Methods

        private async Task<List<DiarizedSegment>> RunConversationTranscriptionAsync(
            SpeechConfig speechConfig,
            AudioConfig audioConfig,
            CancellationToken cancellationToken)
        {
            var segments = new List<DiarizedSegment>();
            var stopRecognition = new TaskCompletionSource<bool>();

            using var recognizer = new ConversationTranscriber(speechConfig, audioConfig);

            recognizer.Transcribing += (s, e) =>
            {
                Console.Write($"\r  [Transcribing] Speaker {e.Result.SpeakerId}: {e.Result.Text}");
            };

            recognizer.Transcribed += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrEmpty(e.Result.Text))
                {
                    var segment = new DiarizedSegment
                    {
                        SpeakerId = e.Result.SpeakerId ?? "Unknown",
                        Text = e.Result.Text,
                        Offset = TimeSpan.FromTicks(e.Result.OffsetInTicks),
                        Duration = e.Result.Duration
                    };

                    segments.Add(segment);
                    Console.WriteLine();
                    Console.WriteLine($"  [Recognized] Speaker {segment.SpeakerId}: {segment.Text}");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                if (e.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"\n  [Error] Code: {e.ErrorCode}, Details: {e.ErrorDetails}");
                }
                stopRecognition.TrySetResult(true);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                Console.WriteLine("\n  [Session stopped]");
                stopRecognition.TrySetResult(true);
            };

            await recognizer.StartTranscribingAsync();

            using var cts = CancellationTokenRegistration(cancellationToken, stopRecognition);
            await stopRecognition.Task;

            await recognizer.StopTranscribingAsync();
            return segments;
        }

        private static CancellationTokenRegistration CancellationTokenRegistration(
            CancellationToken cancellationToken,
            TaskCompletionSource<bool> stopRecognition)
        {
            return cancellationToken.Register(() => stopRecognition.TrySetResult(true));
        }

        #endregion
    }

    /// <summary>
    /// Represents a single segment of recognized speech with speaker information.
    /// </summary>
    public class DiarizedSegment
    {
        /// <summary>
        /// The speaker identifier assigned during diarization.
        /// </summary>
        public string SpeakerId { get; set; } = string.Empty;

        /// <summary>
        /// The recognized text content.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The time offset from the start of the audio.
        /// </summary>
        public TimeSpan Offset { get; set; }

        /// <summary>
        /// The duration of this segment.
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}

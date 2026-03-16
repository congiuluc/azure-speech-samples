"""Azure Conversation Transcription and Pronunciation Assessment demo.

Demonstrates: multi-speaker conversation transcription, speaker identification
via diarization, pronunciation assessment scoring, and keyword recognition.
"""

import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import azure.cognitiveservices.speech as speechsdk

from shared.speech_settings import SpeechSettings
from shared.diarization_helper import DiarizationHelper, DiarizedSegment


def conversation_transcription(settings: SpeechSettings, use_file: bool) -> None:
    """Perform multi-speaker conversation transcription with diarization.

    Uses the shared DiarizationHelper for speaker-labeled transcription.
    """
    print("--- Conversation Transcription with Diarization ---")

    helper = DiarizationHelper(settings)

    if use_file:
        file_path = input("Enter audio file path (WAV): ").strip()
        if not file_path:
            print("No file path provided.")
            return

        print(f"Processing file: {file_path}")
        segments = helper.recognize_from_file(file_path)
    else:
        print("Start a conversation with multiple speakers.")
        print("Press Ctrl+C to stop.")
        segments = helper.recognize_from_microphone()

    DiarizationHelper.print_segments(segments)

    if segments:
        _print_conversation_summary(segments)


def _print_conversation_summary(segments: list[DiarizedSegment]) -> None:
    """Print a summary of the conversation: speaker stats, word counts, and timing."""
    print()
    print("=== Conversation Summary ===")

    by_speaker: dict[str, list[DiarizedSegment]] = {}
    for segment in segments:
        by_speaker.setdefault(segment.speaker_id, []).append(segment)

    print(f"Total speakers: {len(by_speaker)}")
    print(f"Total segments: {len(segments)}")
    print()

    for speaker_id in sorted(by_speaker.keys()):
        speaker_segments = by_speaker[speaker_id]
        word_count = sum(len(s.text.split()) for s in speaker_segments)
        total_duration = sum(s.duration_seconds for s in speaker_segments)
        minutes = int(total_duration // 60)
        seconds = total_duration % 60

        print(f"  {speaker_id}:")
        print(f"    Segments: {len(speaker_segments)}")
        print(f"    Words: {word_count}")
        print(f"    Total speaking time: {minutes:02d}:{seconds:05.2f}")


def pronunciation_assessment(settings: SpeechSettings, use_file: bool) -> None:
    """Perform pronunciation assessment using the Azure Speech SDK.

    Evaluates accuracy, fluency, completeness, and pronunciation scores.
    Also includes STT with diarization for context.
    """
    print("--- Pronunciation Assessment ---")

    reference_text = input("Enter the reference text to assess pronunciation against: ").strip()
    if not reference_text:
        reference_text = "Buongiorno, come stai oggi? Spero che tu stia bene."
        print(f'Using default reference text: "{reference_text}"')

    speech_config = settings.create_speech_config()
    speech_config.speech_recognition_language = settings.language

    pronunciation_config = speechsdk.PronunciationAssessmentConfig(
        reference_text=reference_text,
        grading_system=speechsdk.PronunciationAssessmentGradingSystem.HundredMark,
        granularity=speechsdk.PronunciationAssessmentGranularity.Phoneme,
        enable_miscue=True,
    )

    if use_file:
        file_path = input("Enter audio file path (WAV): ").strip()
        if not file_path:
            print("No file path provided.")
            return
        audio_config = speechsdk.audio.AudioConfig(filename=file_path)
    else:
        print(f"Read the following text aloud:")
        print(f'  "{reference_text}"')
        print()
        print("Speak now...")
        audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)

    recognizer = speechsdk.SpeechRecognizer(
        speech_config=speech_config, audio_config=audio_config
    )
    pronunciation_config.apply_to(recognizer)

    result = recognizer.recognize_once()

    if result.reason == speechsdk.ResultReason.RecognizedSpeech:
        print()
        print(f"Recognized text: {result.text}")

        assessment = speechsdk.PronunciationAssessmentResult(result)
        print()
        print("=== Pronunciation Assessment Results ===")
        print(f"  Accuracy score:      {assessment.accuracy_score:.1f}/100")
        print(f"  Fluency score:       {assessment.fluency_score:.1f}/100")
        print(f"  Completeness score:  {assessment.completeness_score:.1f}/100")
        print(f"  Pronunciation score: {assessment.pronunciation_score:.1f}/100")

        print()
        print("Word-level details:")
        print(f"  {'Word':<20} {'Accuracy':<12} Error Type")
        print("-" * 50)

        for word in assessment.words:
            print(f"  {word.word:<20} {word.accuracy_score:<12.1f} {word.error_type}")

    elif result.reason == speechsdk.ResultReason.NoMatch:
        print("No speech was recognized.")
    elif result.reason == speechsdk.ResultReason.Canceled:
        cancellation = result.cancellation_details
        print(f"Canceled: {cancellation.reason}")
        if cancellation.reason == speechsdk.CancellationReason.Error:
            print(f"Error: {cancellation.error_details}")

    # Also run diarization for context
    if use_file:
        print()
        print("--- Running STT with diarization for additional context ---")
        answer = input("Re-process same file with diarization? (y/N): ").strip()
        if answer.lower() == "y":
            diarize_path = input("Enter audio file path (WAV): ").strip()
            if diarize_path:
                helper = DiarizationHelper(settings)
                segments = helper.recognize_from_file(diarize_path)
                DiarizationHelper.print_segments(segments)


def keyword_recognition_with_diarization(settings: SpeechSettings) -> None:
    """Demonstrate keyword spotting followed by full STT with diarization.

    Listens for a keyword before starting full STT with diarization.
    Useful for wake-word scenarios.
    """
    print("--- Keyword Recognition + STT with Diarization ---")
    print()
    print("Note: Keyword recognition requires a keyword model file (.table).")
    print("You can create one at https://speech.microsoft.com/portal")
    print()

    keyword_model_path = input(
        "Enter keyword model file path (.table), or press Enter to skip to STT: "
    ).strip()

    if keyword_model_path and os.path.exists(keyword_model_path):
        speech_config = settings.create_speech_config()
        speech_config.speech_recognition_language = settings.language

        audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)
        recognizer = speechsdk.SpeechRecognizer(
            speech_config=speech_config, audio_config=audio_config
        )

        model = speechsdk.KeywordRecognitionModel(keyword_model_path)
        print("Listening for keyword... Speak the keyword to activate.")

        result = recognizer.recognize_once()
        if result.reason == speechsdk.ResultReason.RecognizedKeyword:
            print(f"Keyword detected: {result.text}")
            print("Starting full STT with diarization...")
    else:
        print("No keyword model provided. Starting STT with diarization directly.")

    # Run diarization
    print()
    print("Speak into your microphone. Press Ctrl+C to stop.")
    helper = DiarizationHelper(settings)
    segments = helper.recognize_from_microphone()
    DiarizationHelper.print_segments(segments)

    if segments:
        _print_conversation_summary(segments)


def main() -> None:
    """Main entry point for the Conversation Transcription demo."""
    print("================================================")
    print("  Azure Conversation Transcription +")
    print("  Pronunciation Assessment + Diarization Demo")
    print("================================================")
    print()

    settings = SpeechSettings.load()
    settings.validate()

    print(f"Region: {settings.region}")
    print(f"Language: {settings.language}")
    print()

    while True:
        print("Select an option:")
        print("  1 - Conversation transcription with diarization (microphone)")
        print("  2 - Conversation transcription with diarization (audio file)")
        print("  3 - Pronunciation assessment (microphone)")
        print("  4 - Pronunciation assessment (audio file)")
        print("  5 - Keyword recognition + STT with diarization")
        print("  0 - Exit")

        choice = input("> ").strip()
        print()

        if choice == "1":
            conversation_transcription(settings, use_file=False)
        elif choice == "2":
            conversation_transcription(settings, use_file=True)
        elif choice == "3":
            pronunciation_assessment(settings, use_file=False)
        elif choice == "4":
            pronunciation_assessment(settings, use_file=True)
        elif choice == "5":
            keyword_recognition_with_diarization(settings)
        elif choice == "0":
            print("Goodbye!")
            return
        else:
            print("Invalid option. Try again.")

        print()


if __name__ == "__main__":
    main()

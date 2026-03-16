"""Azure Speech-to-Text demo with speaker diarization.

Demonstrates: real-time STT, continuous recognition, and conversation transcription
with automatic speaker identification (diarization).
"""

import sys
import os

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import azure.cognitiveservices.speech as speechsdk

from shared.speech_settings import SpeechSettings
from shared.diarization_helper import DiarizationHelper


def single_shot_recognition(settings: SpeechSettings) -> None:
    """Perform a single-shot speech recognition from the default microphone.

    Recognizes a single utterance until a pause is detected.
    """
    print("--- Single-shot Recognition ---")
    print("Speak into your microphone...")

    speech_config = settings.create_speech_config()
    speech_config.speech_recognition_language = settings.language

    audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)
    recognizer = speechsdk.SpeechRecognizer(
        speech_config=speech_config, audio_config=audio_config
    )

    result = recognizer.recognize_once()

    if result.reason == speechsdk.ResultReason.RecognizedSpeech:
        print(f"Recognized: {result.text}")
        print(f"Duration: {result.duration / 10_000_000:.2f}s")
    elif result.reason == speechsdk.ResultReason.NoMatch:
        print("No speech could be recognized.")
    elif result.reason == speechsdk.ResultReason.Canceled:
        cancellation = result.cancellation_details
        print(f"Canceled: {cancellation.reason}")
        if cancellation.reason == speechsdk.CancellationReason.Error:
            print(f"Error code: {cancellation.error_code}")
            print(f"Error details: {cancellation.error_details}")


def continuous_recognition_with_diarization(settings: SpeechSettings, use_file: bool) -> None:
    """Perform continuous speech recognition with speaker diarization.

    Uses the shared DiarizationHelper for transcription.
    """
    print("--- Continuous Recognition with Diarization ---")

    helper = DiarizationHelper(settings)

    if use_file:
        file_path = input("Enter audio file path (WAV): ").strip()
        if not file_path:
            print("No file path provided.")
            return

        print(f"Processing file: {file_path}")
        segments = helper.recognize_from_file(file_path)
    else:
        print("Speak into your microphone. Press Ctrl+C to stop.")
        segments = helper.recognize_from_microphone()

    DiarizationHelper.print_segments(segments)


def main() -> None:
    """Main entry point for the Speech-to-Text demo."""
    print("===========================================")
    print("  Azure Speech-to-Text + Diarization Demo")
    print("===========================================")
    print()

    settings = SpeechSettings.load()
    settings.validate()

    print(f"Region: {settings.region}")
    print(f"Language: {settings.language}")
    print()

    while True:
        print("Select an option:")
        print("  1 - Single-shot recognition (microphone)")
        print("  2 - Continuous recognition with diarization (microphone)")
        print("  3 - Continuous recognition with diarization (audio file)")
        print("  0 - Exit")

        choice = input("> ").strip()
        print()

        if choice == "1":
            single_shot_recognition(settings)
        elif choice == "2":
            continuous_recognition_with_diarization(settings, use_file=False)
        elif choice == "3":
            continuous_recognition_with_diarization(settings, use_file=True)
        elif choice == "0":
            print("Goodbye!")
            return
        else:
            print("Invalid option. Try again.")

        print()


if __name__ == "__main__":
    main()

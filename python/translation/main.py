"""Azure Speech Translation demo with Speech-to-Text and diarization.

Demonstrates: real-time speech translation, multi-language translation,
and conversation transcription with diarization before/after translation.
"""

import sys
import os
import time

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import azure.cognitiveservices.speech as speechsdk

from shared.speech_settings import SpeechSettings
from shared.diarization_helper import DiarizationHelper


def single_shot_translation(settings: SpeechSettings) -> None:
    """Perform a single-shot speech translation from the default microphone."""
    print("--- Single-shot Translation ---")
    print("Speak into your microphone...")

    translation_config = settings.create_translation_config()
    translation_config.speech_recognition_language = settings.language
    translation_config.add_target_language(settings.target_language)

    audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)
    recognizer = speechsdk.translation.TranslationRecognizer(
        translation_config=translation_config, audio_config=audio_config
    )

    result = recognizer.recognize_once()

    if result.reason == speechsdk.ResultReason.TranslatedSpeech:
        print(f"Recognized ({settings.language}): {result.text}")
        for lang, translation in result.translations.items():
            print(f"Translated ({lang}): {translation}")
    elif result.reason == speechsdk.ResultReason.NoMatch:
        print("No speech could be recognized.")
    elif result.reason == speechsdk.ResultReason.Canceled:
        cancellation = result.cancellation_details
        print(f"Canceled: {cancellation.reason}")
        if cancellation.reason == speechsdk.CancellationReason.Error:
            print(f"Error code: {cancellation.error_code}")
            print(f"Error details: {cancellation.error_details}")


def continuous_translation(settings: SpeechSettings) -> None:
    """Perform continuous speech translation from the default microphone."""
    print("--- Continuous Translation ---")
    print("Speak into your microphone. Press Ctrl+C to stop.")

    translation_config = settings.create_translation_config()
    translation_config.speech_recognition_language = settings.language
    translation_config.add_target_language(settings.target_language)

    audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)
    recognizer = speechsdk.translation.TranslationRecognizer(
        translation_config=translation_config, audio_config=audio_config
    )

    done = False

    def _recognized(evt):
        if evt.result.reason == speechsdk.ResultReason.TranslatedSpeech:
            print(f"  [{settings.language}] {evt.result.text}")
            for lang, translation in evt.result.translations.items():
                print(f"  [{lang}] {translation}")
            print()

    def _canceled(evt):
        nonlocal done
        if evt.cancellation_details.reason == speechsdk.CancellationReason.Error:
            print(f"  [Error] {evt.cancellation_details.error_code}: "
                  f"{evt.cancellation_details.error_details}")
        done = True

    def _session_stopped(evt):
        nonlocal done
        done = True

    recognizer.recognized.connect(_recognized)
    recognizer.canceled.connect(_canceled)
    recognizer.session_stopped.connect(_session_stopped)

    recognizer.start_continuous_recognition()

    try:
        while not done:
            time.sleep(0.5)
    except KeyboardInterrupt:
        print("\n  [Stopped by user]")

    recognizer.stop_continuous_recognition()


def multi_language_translation(settings: SpeechSettings) -> None:
    """Translate speech to multiple target languages simultaneously."""
    print("--- Multi-language Translation ---")

    target_languages = ["en", "de", "fr", "es"]
    print(f"Translating from {settings.language} to: {', '.join(target_languages)}")
    print("Speak into your microphone...")

    translation_config = settings.create_translation_config()
    translation_config.speech_recognition_language = settings.language

    for lang in target_languages:
        translation_config.add_target_language(lang)

    audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)
    recognizer = speechsdk.translation.TranslationRecognizer(
        translation_config=translation_config, audio_config=audio_config
    )

    result = recognizer.recognize_once()

    if result.reason == speechsdk.ResultReason.TranslatedSpeech:
        print(f"Recognized ({settings.language}): {result.text}")
        print()
        for lang, translation in result.translations.items():
            print(f"  [{lang}] {translation}")
    else:
        print(f"Result: {result.reason}")


def diarization_with_translation(settings: SpeechSettings, use_file: bool) -> None:
    """Perform STT with speaker diarization, then show each segment for translation.

    Combines the diarization feature with translation to show who said what
    in both source and target languages.
    """
    print("--- Diarization + Translation ---")

    # Step 1: Perform STT with diarization
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

    if not segments:
        print("No speech segments to translate.")
        return

    # Step 2: Show each diarized segment with translation note
    print()
    print(f"=== Translated Segments ({settings.language} -> {settings.target_language}) ===")
    print(f"{'Time':<12} {'Speaker':<12} {'Original':<40} Translation")
    print("-" * 110)

    for segment in segments:
        print(
            f"{segment.offset_formatted:<12} {segment.speaker_id:<12} "
            f"{segment.text:<40} [Translation would use Translator Text API]"
        )

    print("-" * 110)
    print(
        "Note: For text-to-text translation of transcribed segments, "
        "use the Azure Translator Text API for best results."
    )


def main() -> None:
    """Main entry point for the Speech Translation demo."""
    print("===========================================")
    print("  Azure Speech Translation + Diarization Demo")
    print("===========================================")
    print()

    settings = SpeechSettings.load()
    settings.validate()

    print(f"Region: {settings.region}")
    print(f"Source language: {settings.language}")
    print(f"Target language: {settings.target_language}")
    print()

    while True:
        print("Select an option:")
        print("  1 - Single-shot translation (microphone)")
        print("  2 - Continuous translation (microphone)")
        print("  3 - Multi-language translation (microphone)")
        print("  4 - Diarization + Translation (microphone)")
        print("  5 - Diarization + Translation (audio file)")
        print("  0 - Exit")

        choice = input("> ").strip()
        print()

        if choice == "1":
            single_shot_translation(settings)
        elif choice == "2":
            continuous_translation(settings)
        elif choice == "3":
            multi_language_translation(settings)
        elif choice == "4":
            diarization_with_translation(settings, use_file=False)
        elif choice == "5":
            diarization_with_translation(settings, use_file=True)
        elif choice == "0":
            print("Goodbye!")
            return
        else:
            print("Invalid option. Try again.")

        print()


if __name__ == "__main__":
    main()

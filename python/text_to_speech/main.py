"""Azure Text-to-Speech demo with Speech-to-Text verification and diarization.

Demonstrates: neural TTS synthesis, SSML support, voice listing,
and a round-trip scenario (STT with diarization -> TTS -> STT verification).
"""

import sys
import os
import tempfile
import uuid

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import azure.cognitiveservices.speech as speechsdk

from shared.speech_settings import SpeechSettings
from shared.diarization_helper import DiarizationHelper


def synthesize_to_speaker(settings: SpeechSettings) -> None:
    """Synthesize text to the default speaker output."""
    print("--- Synthesize to Speaker ---")
    text = input("Enter text to synthesize: ").strip()
    if not text:
        print("No text provided.")
        return

    speech_config = settings.create_speech_config()
    speech_config.speech_synthesis_voice_name = settings.voice_name

    audio_config = speechsdk.audio.AudioOutputConfig(use_default_speaker=True)
    synthesizer = speechsdk.SpeechSynthesizer(
        speech_config=speech_config, audio_config=audio_config
    )

    result = synthesizer.speak_text(text)
    _handle_synthesis_result(result)


def synthesize_to_file(settings: SpeechSettings) -> None:
    """Synthesize text to a WAV file."""
    print("--- Synthesize to WAV File ---")
    text = input("Enter text to synthesize: ").strip()
    if not text:
        print("No text provided.")
        return

    output_path = input("Enter output file path (e.g., output.wav): ").strip()
    if not output_path:
        output_path = "output.wav"

    speech_config = settings.create_speech_config()
    speech_config.speech_synthesis_voice_name = settings.voice_name

    audio_config = speechsdk.audio.AudioOutputConfig(filename=output_path)
    synthesizer = speechsdk.SpeechSynthesizer(
        speech_config=speech_config, audio_config=audio_config
    )

    result = synthesizer.speak_text(text)
    _handle_synthesis_result(result)

    if result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
        print(f"Audio saved to: {os.path.abspath(output_path)}")


def synthesize_with_ssml(settings: SpeechSettings) -> None:
    """Synthesize text using SSML for fine-grained control."""
    print("--- Synthesize with SSML ---")
    ssml = f"""<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='{settings.language}'>
    <voice name='{settings.voice_name}'>
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
</speak>"""

    print("SSML content:")
    print(ssml)
    print()

    speech_config = settings.create_speech_config()

    audio_config = speechsdk.audio.AudioOutputConfig(use_default_speaker=True)
    synthesizer = speechsdk.SpeechSynthesizer(
        speech_config=speech_config, audio_config=audio_config
    )

    result = synthesizer.speak_ssml(ssml)
    _handle_synthesis_result(result)


def list_voices(settings: SpeechSettings) -> None:
    """List all available neural voices for the configured region."""
    print("--- Available Voices ---")
    speech_config = settings.create_speech_config()

    synthesizer = speechsdk.SpeechSynthesizer(speech_config=speech_config, audio_config=None)
    voices_result = synthesizer.get_voices_async().get()

    if voices_result.reason == speechsdk.ResultReason.VoicesListRetrieved:
        locale_prefix = settings.language[:2]
        voices = sorted(
            [v for v in voices_result.voices if v.locale.lower().startswith(locale_prefix)],
            key=lambda v: v.short_name,
        )

        print(f"Found {len(voices)} voices for locale '{locale_prefix}':")
        for voice in voices:
            print(f"  {voice.short_name:<30} {voice.gender:<8} {voice.voice_type}")
    else:
        print("Failed to retrieve voice list.")


def round_trip_demo(settings: SpeechSettings) -> None:
    """Round-trip demo: STT with diarization -> TTS -> STT verification."""
    print("--- Round-trip Demo: STT + Diarization -> TTS -> STT ---")
    print()

    # Step 1: Speech-to-Text with diarization
    print("[Step 1] Speak into your microphone. Press Ctrl+C to stop.")
    helper = DiarizationHelper(settings)
    segments = helper.recognize_from_microphone()
    DiarizationHelper.print_segments(segments)

    if not segments:
        print("No speech was recognized. Cannot continue round-trip.")
        return

    # Step 2: Synthesize the recognized text to a WAV file
    combined_text = " ".join(s.text for s in segments)
    print()
    print(f'[Step 2] Synthesizing combined text: "{combined_text}"')

    temp_wav = os.path.join(tempfile.gettempdir(), f"roundtrip_{uuid.uuid4()}.wav")
    speech_config = settings.create_speech_config()
    speech_config.speech_synthesis_voice_name = settings.voice_name

    audio_config = speechsdk.audio.AudioOutputConfig(filename=temp_wav)
    synthesizer = speechsdk.SpeechSynthesizer(
        speech_config=speech_config, audio_config=audio_config
    )

    result = synthesizer.speak_text(combined_text)
    _handle_synthesis_result(result)

    # Step 3: Re-transcribe the synthesized audio with diarization
    print()
    print("[Step 3] Re-transcribing synthesized audio with diarization...")
    verification_segments = helper.recognize_from_file(temp_wav)
    DiarizationHelper.print_segments(verification_segments)

    # Cleanup temp file
    try:
        os.remove(temp_wav)
    except OSError:
        pass


def _handle_synthesis_result(result: speechsdk.SpeechSynthesisResult) -> None:
    """Handle and display the result of a speech synthesis operation."""
    if result.reason == speechsdk.ResultReason.SynthesizingAudioCompleted:
        print("Synthesis completed successfully.")
        print(f"Audio length: {len(result.audio_data)} bytes")
    elif result.reason == speechsdk.ResultReason.Canceled:
        cancellation = result.cancellation_details
        print(f"Synthesis canceled: {cancellation.reason}")
        if cancellation.reason == speechsdk.CancellationReason.Error:
            print(f"Error code: {cancellation.error_code}")
            print(f"Error details: {cancellation.error_details}")


def main() -> None:
    """Main entry point for the Text-to-Speech demo."""
    print("===========================================")
    print("  Azure Text-to-Speech + Diarization Demo")
    print("===========================================")
    print()

    settings = SpeechSettings.load()
    settings.validate()

    print(f"Region: {settings.region}")
    print(f"Voice: {settings.voice_name}")
    print(f"Language: {settings.language}")
    print()

    while True:
        print("Select an option:")
        print("  1 - Synthesize text to speaker")
        print("  2 - Synthesize text to WAV file")
        print("  3 - Synthesize with SSML")
        print("  4 - List available voices")
        print("  5 - Round-trip: STT with diarization -> TTS -> STT verification")
        print("  0 - Exit")

        choice = input("> ").strip()
        print()

        if choice == "1":
            synthesize_to_speaker(settings)
        elif choice == "2":
            synthesize_to_file(settings)
        elif choice == "3":
            synthesize_with_ssml(settings)
        elif choice == "4":
            list_voices(settings)
        elif choice == "5":
            round_trip_demo(settings)
        elif choice == "0":
            print("Goodbye!")
            return
        else:
            print("Invalid option. Try again.")

        print()


if __name__ == "__main__":
    main()

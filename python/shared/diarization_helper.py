"""Shared helper for Speech-to-Text recognition with speaker diarization.

All demo apps use this to provide consistent STT + diarization functionality.
"""

import time
from dataclasses import dataclass, field
from typing import List

import azure.cognitiveservices.speech as speechsdk

from .speech_settings import SpeechSettings


@dataclass
class DiarizedSegment:
    """Represents a single segment of recognized speech with speaker information."""

    speaker_id: str = ""
    text: str = ""
    offset_ticks: int = 0
    duration_ticks: int = 0

    @property
    def offset_seconds(self) -> float:
        """Offset from the start of the audio in seconds."""
        return self.offset_ticks / 10_000_000

    @property
    def duration_seconds(self) -> float:
        """Duration of this segment in seconds."""
        return self.duration_ticks / 10_000_000

    @property
    def offset_formatted(self) -> str:
        """Offset formatted as mm:ss.ff."""
        total_seconds = self.offset_seconds
        minutes = int(total_seconds // 60)
        seconds = total_seconds % 60
        return f"{minutes:02d}:{seconds:05.2f}"


class DiarizationHelper:
    """Shared helper for Speech-to-Text recognition with speaker diarization.

    All demo apps use this to provide consistent STT + diarization functionality.
    """

    def __init__(self, settings: SpeechSettings):
        self._settings = settings

    def recognize_from_file(self, audio_file_path: str) -> List[DiarizedSegment]:
        """Perform continuous speech-to-text recognition with speaker diarization from a file.

        Args:
            audio_file_path: Path to the audio file (WAV format).

        Returns:
            List of recognized segments with speaker information.
        """
        speech_config = self._settings.create_speech_config()
        speech_config.speech_recognition_language = self._settings.language
        speech_config.set_property(
            speechsdk.PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true"
        )

        audio_config = speechsdk.audio.AudioConfig(filename=audio_file_path)
        return self._run_conversation_transcription(speech_config, audio_config)

    def recognize_from_microphone(self) -> List[DiarizedSegment]:
        """Perform continuous speech-to-text recognition with speaker diarization from the microphone.

        Returns:
            List of recognized segments with speaker information.
        """
        speech_config = self._settings.create_speech_config()
        speech_config.speech_recognition_language = self._settings.language
        speech_config.set_property(
            speechsdk.PropertyId.SpeechServiceResponse_DiarizeIntermediateResults, "true"
        )

        audio_config = speechsdk.audio.AudioConfig(use_default_microphone=True)
        return self._run_conversation_transcription(speech_config, audio_config)

    @staticmethod
    def print_segments(segments: List[DiarizedSegment]) -> None:
        """Print diarized segments to the console in a formatted way."""
        print()
        print("=== Diarization Results ===")
        print(f"{'Time':<12} {'Speaker':<12} Text")
        print("-" * 80)

        for segment in segments:
            print(f"{segment.offset_formatted:<12} {segment.speaker_id:<12} {segment.text}")

        print("-" * 80)
        print(f"Total segments: {len(segments)}")

        speakers = sorted(set(s.speaker_id for s in segments))
        print(f"Speakers identified: {', '.join(speakers)}")

    def _run_conversation_transcription(
        self,
        speech_config: speechsdk.SpeechConfig,
        audio_config: speechsdk.audio.AudioConfig,
    ) -> List[DiarizedSegment]:
        """Run conversation transcription with diarization."""
        segments: List[DiarizedSegment] = []
        done = False

        transcriber = speechsdk.transcription.ConversationTranscriber(
            speech_config=speech_config, audio_config=audio_config
        )

        def _transcribing(evt: speechsdk.SpeechRecognitionEventArgs):
            print(f"\r  [Transcribing] Speaker {evt.result.speaker_id}: {evt.result.text}", end="")

        def _transcribed(evt: speechsdk.SpeechRecognitionEventArgs):
            if evt.result.reason == speechsdk.ResultReason.RecognizedSpeech and evt.result.text:
                segment = DiarizedSegment(
                    speaker_id=evt.result.speaker_id or "Unknown",
                    text=evt.result.text,
                    offset_ticks=evt.result.offset,
                    duration_ticks=evt.result.duration,
                )
                segments.append(segment)
                print()
                print(f"  [Recognized] Speaker {segment.speaker_id}: {segment.text}")

        def _canceled(evt: speechsdk.SpeechRecognitionCanceledEventArgs):
            nonlocal done
            if evt.cancellation_details.reason == speechsdk.CancellationReason.Error:
                print(f"\n  [Error] Code: {evt.cancellation_details.error_code}, "
                      f"Details: {evt.cancellation_details.error_details}")
            done = True

        def _session_stopped(evt):
            nonlocal done
            print("\n  [Session stopped]")
            done = True

        transcriber.transcribing.connect(_transcribing)
        transcriber.transcribed.connect(_transcribed)
        transcriber.canceled.connect(_canceled)
        transcriber.session_stopped.connect(_session_stopped)

        transcriber.start_transcribing_async().get()

        try:
            while not done:
                time.sleep(0.5)
        except KeyboardInterrupt:
            print("\n  [Stopped by user]")

        transcriber.stop_transcribing_async().get()
        return segments

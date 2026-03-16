# SpeechSamples.TextToSpeech

## Description

Azure Text-to-Speech demo with neural voice synthesis and integrated Speech-to-Text with diarization for round-trip verification.

## Features

- **Text to speaker**: Synthesize text through the default audio output
- **Text to WAV file**: Save synthesized audio to WAV files
- **SSML synthesis**: Fine-grained control over pronunciation, pitch, rate, and pauses
- **Voice listing**: Browse available neural voices for your locale
- **Round-trip demo**: STT with diarization → TTS synthesis → STT verification

## Installation

```bash
dotnet build
```

Configure `appsettings.json` with your Azure Speech Service credentials.

## Usage Examples

```bash
dotnet run --project SpeechSamples.TextToSpeech.csproj
```

Select from the interactive menu:
1. Synthesize text to speaker
2. Synthesize text to WAV file
3. Synthesize with SSML
4. List available voices
5. Round-trip: STT with diarization → TTS → STT verification

## License

Demo/sample code for Azure Speech Services evaluation.

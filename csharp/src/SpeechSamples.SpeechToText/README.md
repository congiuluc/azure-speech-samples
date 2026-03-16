# SpeechSamples.SpeechToText

## Description

Azure Speech-to-Text demo with speaker diarization support. Demonstrates real-time and file-based speech recognition with automatic speaker identification.

## Features

- **Single-shot recognition**: Recognize a single utterance from the microphone
- **Continuous recognition with diarization**: Real-time transcription identifying who is speaking
- **File-based processing**: Process WAV audio files with speaker diarization

## Installation

```bash
dotnet build
```

Configure `appsettings.json` with your Azure Speech Service credentials.

## Usage Examples

```bash
dotnet run --project SpeechSamples.SpeechToText.csproj
```

Select from the interactive menu:
1. Single-shot recognition (microphone)
2. Continuous recognition with diarization (microphone)
3. Continuous recognition with diarization (audio file)

## License

Demo/sample code for Azure Speech Services evaluation.

# SpeechSamples.Translation

## Description

Azure Speech Translation demo with real-time translation, multi-language support, and integrated Speech-to-Text with speaker diarization.

## Features

- **Single-shot translation**: Translate a single utterance from microphone
- **Continuous translation**: Real-time ongoing speech translation
- **Multi-language translation**: Translate to multiple target languages simultaneously (en, de, fr, es)
- **Diarization + Translation**: Speaker-labeled transcription followed by translation of each segment

## Installation

```bash
dotnet build
```

Configure `appsettings.json` with your Azure Speech Service credentials.

## Usage Examples

```bash
dotnet run --project SpeechSamples.Translation.csproj
```

Select from the interactive menu:
1. Single-shot translation (microphone)
2. Continuous translation (microphone)
3. Multi-language translation (microphone)
4. Diarization + Translation (microphone)
5. Diarization + Translation (audio file)

## License

Demo/sample code for Azure Speech Services evaluation.

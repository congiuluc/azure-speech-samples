# SpeechSamples.SpeakerRecognition

## Description

Azure Conversation Transcription and Pronunciation Assessment demo with Speech-to-Text and speaker diarization. Demonstrates multi-speaker conversation transcription, pronunciation scoring, and keyword recognition.

## Features

- **Conversation transcription**: Multi-speaker transcription with automatic speaker identification via diarization
- **Conversation summary**: Speaker statistics including word count and speaking time per speaker
- **Pronunciation assessment**: Evaluate accuracy, fluency, completeness, and overall pronunciation scores with word-level details
- **Keyword recognition**: Wake-word detection followed by full STT with diarization

## Installation

```bash
dotnet build
```

Configure `appsettings.json` with your Azure Speech Service credentials.

## Usage Examples

```bash
dotnet run --project SpeechSamples.SpeakerRecognition.csproj
```

Select from the interactive menu:
1. Conversation transcription with diarization (microphone)
2. Conversation transcription with diarization (audio file)
3. Pronunciation assessment (microphone)
4. Pronunciation assessment (audio file)
5. Keyword recognition + STT with diarization

## License

Demo/sample code for Azure Speech Services evaluation.

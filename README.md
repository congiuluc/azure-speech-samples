# Azure Speech Services Demo Samples

A collection of .NET 10 console applications demonstrating Azure Cognitive Services Speech SDK features, with a focus on **Speech-to-Text** and **speaker diarization** across all demos.

## Description

This solution contains 4 demo applications, each showcasing a different Azure Speech Service capability. All demos include Speech-to-Text recognition with speaker diarization (identifying who spoke when).

| Demo | Service | Features |
|------|---------|----------|
| **SpeechToText** | Speech-to-Text | Single-shot & continuous recognition, real-time diarization |
| **TextToSpeech** | Text-to-Speech | Neural TTS, SSML, voice listing, round-trip STT→TTS→STT |
| **Translation** | Speech Translation | Real-time translation, multi-language, diarized translation |
| **SpeakerRecognition** | Conversation Transcription | Multi-speaker transcription, pronunciation assessment, keyword recognition |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An [Azure Speech Service](https://learn.microsoft.com/azure/ai-services/speech-service/overview) resource
- A working microphone (for real-time demos) or WAV audio files

## Installation

1. Clone this repository
2. Copy the settings template and configure your Azure credentials:

   ```bash
   # Edit appsettings.json in any demo project folder
   # Set your SubscriptionKey and Region
   ```

   Or set environment variables:
   ```bash
   set AZURE_SPEECH_KEY=your-subscription-key
   set AZURE_SPEECH_REGION=westeurope
   ```

3. Restore and build:
   ```bash
   dotnet build SpeechSamples.slnx
   ```

## Usage Examples

### Speech-to-Text with Diarization
```bash
dotnet run --project src/SpeechSamples.SpeechToText/SpeechSamples.SpeechToText.csproj
```
- Option 1: Single-shot recognition from microphone
- Option 2: Continuous recognition with speaker diarization (microphone)
- Option 3: Continuous recognition with speaker diarization (audio file)

### Text-to-Speech
```bash
dotnet run --project src/SpeechSamples.TextToSpeech/SpeechSamples.TextToSpeech.csproj
```
- Option 1-3: Synthesize to speaker, WAV file, or with SSML
- Option 4: List available neural voices
- Option 5: Round-trip demo (STT with diarization → TTS → STT verification)

### Speech Translation
```bash
dotnet run --project src/SpeechSamples.Translation/SpeechSamples.Translation.csproj
```
- Option 1-2: Single-shot and continuous translation
- Option 3: Multi-language translation (it→en, de, fr, es)
- Option 4-5: Diarization + Translation (microphone/file)

### Conversation Transcription & Pronunciation Assessment
```bash
dotnet run --project src/SpeechSamples.SpeakerRecognition/SpeechSamples.SpeakerRecognition.csproj
```
- Option 1-2: Multi-speaker conversation transcription with diarization
- Option 3-4: Pronunciation assessment with scoring
- Option 5: Keyword recognition + STT with diarization

## Project Structure

```
SpeechSamples/
├── SpeechSamples.slnx
├── src/
│   ├── SpeechSamples.Shared/          # Shared settings, DiarizationHelper
│   ├── SpeechSamples.SpeechToText/    # Speech-to-Text demo
│   ├── SpeechSamples.TextToSpeech/    # Text-to-Speech demo
│   ├── SpeechSamples.Translation/     # Speech Translation demo
│   └── SpeechSamples.SpeakerRecognition/ # Conversation Transcription demo
├── tests/
├── docs/
└── README.md
```

## Configuration

Edit `appsettings.json` in any demo project:

```json
{
  "SpeechSettings": {
    "SubscriptionKey": "YOUR_AZURE_SPEECH_KEY",
    "Region": "westeurope",
    "Language": "it-IT",
    "TargetLanguage": "en",
    "VoiceName": "it-IT-ElsaNeural"
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `SubscriptionKey` | Azure Speech Service key | - |
| `Region` | Azure region | `westeurope` |
| `Language` | Source language (BCP-47) | `it-IT` |
| `TargetLanguage` | Translation target | `en` |
| `VoiceName` | TTS neural voice | `it-IT-ElsaNeural` |

## License

This project is provided as demo/sample code for Azure Speech Services evaluation.

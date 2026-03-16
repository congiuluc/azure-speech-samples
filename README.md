# Azure Speech Services Demo Samples

A collection of demo applications in **C# (.NET 10)** and **Python** demonstrating Azure Cognitive Services Speech SDK features, with a focus on **Speech-to-Text** and **speaker diarization** across all demos.

## Description

This repository contains 4 demo applications in two languages (C# and Python), each showcasing a different Azure Speech Service capability. All demos include Speech-to-Text recognition with speaker diarization (identifying who spoke when).

| Demo | Service | Features |
|------|---------|----------|
| **SpeechToText** | Speech-to-Text | Single-shot & continuous recognition, real-time diarization |
| **TextToSpeech** | Text-to-Speech | Neural TTS, SSML, voice listing, round-trip STT→TTS→STT |
| **Translation** | Speech Translation | Real-time translation, multi-language, diarized translation |
| **ConversationTranscription** | Conversation Transcription | Multi-speaker transcription, pronunciation assessment, keyword recognition |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (for C# demos)
- [Python 3.9+](https://www.python.org/downloads/) (for Python demos)
- An [Azure Speech Service](https://learn.microsoft.com/azure/ai-services/speech-service/overview) resource
- A working microphone (for real-time demos) or WAV audio files

## Quick Start

### C# Version

```bash
cd csharp
dotnet build SpeechSamples.slnx

# Edit appsettings.json in any demo project with your Azure key, then:
dotnet run --project src/SpeechSamples.SpeechToText/SpeechSamples.SpeechToText.csproj
```

### Python Version

```bash
cd python
python -m venv .venv
.venv\Scripts\activate          # Windows
# source .venv/bin/activate     # Linux/macOS
pip install -r requirements.txt

# Copy .env.template to .env and fill in your Azure key, then:
python speech_to_text/main.py
```

## Project Structure

```
SpeechSamples/
├── README.md
├── .gitignore
├── csharp/                              # C# (.NET 10) demos
│   ├── SpeechSamples.slnx
│   ├── src/
│   │   ├── SpeechSamples.Shared/        # Shared settings, DiarizationHelper
│   │   ├── SpeechSamples.SpeechToText/  # Speech-to-Text demo
│   │   ├── SpeechSamples.TextToSpeech/  # Text-to-Speech demo
│   │   ├── SpeechSamples.Translation/   # Speech Translation demo
│   │   └── SpeechSamples.SpeakerRecognition/ # Conversation Transcription demo
│   ├── tests/
│   └── docs/
└── python/                              # Python demos
    ├── requirements.txt
    ├── .env.template
    ├── shared/                          # Shared settings, DiarizationHelper
    ├── speech_to_text/                  # Speech-to-Text demo
    ├── text_to_speech/                  # Text-to-Speech demo
    ├── translation/                     # Speech Translation demo
    └── conversation_transcription/      # Conversation Transcription demo
```

## Configuration

### C# — appsettings.json

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

### Python — .env

```env
AZURE_SPEECH_KEY=your-subscription-key
AZURE_SPEECH_REGION=westeurope
AZURE_SPEECH_LANGUAGE=it-IT
AZURE_SPEECH_TARGET_LANGUAGE=en
AZURE_SPEECH_VOICE_NAME=it-IT-ElsaNeural
```

| Setting | Description | Default |
|---------|-------------|---------|
| Subscription Key | Azure Speech Service key | - |
| Region | Azure region | `westeurope` |
| Language | Source language (BCP-47) | `it-IT` |
| Target Language | Translation target | `en` |
| Voice Name | TTS neural voice | `it-IT-ElsaNeural` |

## Demo Details

See the language-specific READMEs for detailed usage:
- [C# README](csharp/src/SpeechSamples.SpeechToText/README.md)
- [Python README](python/README.md)

## License

This project is provided as demo/sample code for Azure Speech Services evaluation.

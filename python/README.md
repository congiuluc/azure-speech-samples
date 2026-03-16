# Azure Speech Services Demo Samples (Python)

Python console applications demonstrating Azure Cognitive Services Speech SDK features, with a focus on **Speech-to-Text** and **speaker diarization** across all demos.

## Description

This folder contains 4 demo applications, each showcasing a different Azure Speech Service capability. All demos include Speech-to-Text recognition with speaker diarization.

| Demo | Service | Features |
|------|---------|----------|
| **speech_to_text** | Speech-to-Text | Single-shot & continuous recognition, real-time diarization |
| **text_to_speech** | Text-to-Speech | Neural TTS, SSML, voice listing, round-trip STT->TTS->STT |
| **translation** | Speech Translation | Real-time translation, multi-language, diarized translation |
| **conversation_transcription** | Conversation Transcription | Multi-speaker transcription, pronunciation assessment, keyword recognition |

## Prerequisites

- Python 3.9+
- An [Azure Speech Service](https://learn.microsoft.com/azure/ai-services/speech-service/overview) resource
- A working microphone (for real-time demos) or WAV audio files

## Installation

1. Create and activate a virtual environment:
   ```bash
   cd python
   python -m venv .venv
   # Windows
   .venv\Scripts\activate
   # Linux/macOS
   source .venv/bin/activate
   ```

2. Install dependencies:
   ```bash
   pip install -r requirements.txt
   ```

3. Configure your Azure credentials — copy `.env.template` to `.env` and fill in your values:
   ```bash
   cp .env.template .env
   # Edit .env with your Azure Speech key and region
   ```

## Usage Examples

### Speech-to-Text with Diarization
```bash
python speech_to_text/main.py
```

### Text-to-Speech
```bash
python text_to_speech/main.py
```

### Speech Translation
```bash
python translation/main.py
```

### Conversation Transcription & Pronunciation Assessment
```bash
python conversation_transcription/main.py
```

## Project Structure

```
python/
├── requirements.txt
├── .env.template
├── shared/
│   ├── __init__.py
│   ├── speech_settings.py
│   └── diarization_helper.py
├── speech_to_text/
│   └── main.py
├── text_to_speech/
│   └── main.py
├── translation/
│   └── main.py
└── conversation_transcription/
    └── main.py
```

## Configuration

Create a `.env` file in the `python/` folder:

```env
AZURE_SPEECH_KEY=your-subscription-key
AZURE_SPEECH_REGION=westeurope
AZURE_SPEECH_LANGUAGE=it-IT
AZURE_SPEECH_TARGET_LANGUAGE=en
AZURE_SPEECH_VOICE_NAME=it-IT-ElsaNeural
```

| Variable | Description | Default |
|----------|-------------|---------|
| `AZURE_SPEECH_KEY` | Azure Speech Service subscription key | - |
| `AZURE_SPEECH_REGION` | Azure region | `westeurope` |
| `AZURE_SPEECH_LANGUAGE` | Source language (BCP-47) | `it-IT` |
| `AZURE_SPEECH_TARGET_LANGUAGE` | Translation target language | `en` |
| `AZURE_SPEECH_VOICE_NAME` | TTS neural voice name | `it-IT-ElsaNeural` |

## License

Demo/sample code for Azure Speech Services evaluation.

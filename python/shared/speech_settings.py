"""Configuration settings for Azure Speech Services.

Supports both subscription key and managed identity (Entra ID) authentication.
When no subscription key is provided, DefaultAzureCredential is used automatically.
"""

import os
from dataclasses import dataclass
from dotenv import load_dotenv

import azure.cognitiveservices.speech as speechsdk

_COGNITIVE_SERVICES_SCOPE = "https://cognitiveservices.azure.com/.default"


@dataclass
class SpeechSettings:
    """Azure Speech Service configuration settings.

    Loads from environment variables or .env file.
    When subscription_key is empty, managed identity is used via DefaultAzureCredential.
    """

    subscription_key: str = ""
    region: str = ""
    language: str = "it-IT"
    target_language: str = "en"
    voice_name: str = "it-IT-ElsaNeural"

    @property
    def uses_managed_identity(self) -> bool:
        """Whether managed identity (DefaultAzureCredential) is used."""
        return not self.subscription_key

    @staticmethod
    def load() -> "SpeechSettings":
        """Load speech settings from .env file and environment variables."""
        load_dotenv()

        return SpeechSettings(
            subscription_key=os.getenv("AZURE_SPEECH_KEY", ""),
            region=os.getenv("AZURE_SPEECH_REGION", ""),
            language=os.getenv("AZURE_SPEECH_LANGUAGE", "it-IT"),
            target_language=os.getenv("AZURE_SPEECH_TARGET_LANGUAGE", "en"),
            voice_name=os.getenv("AZURE_SPEECH_VOICE_NAME", "it-IT-ElsaNeural"),
        )

    def validate(self) -> None:
        """Validate that required settings are present.

        Region is always required. Subscription key is optional when using managed identity.

        Raises:
            ValueError: If region is missing.
        """
        if not self.region:
            raise ValueError(
                "Speech region is required. "
                "Set AZURE_SPEECH_REGION in .env or as an environment variable."
            )

        if self.uses_managed_identity:
            print(
                "[Auth] No subscription key provided. "
                "Using managed identity (DefaultAzureCredential)."
            )
        else:
            print("[Auth] Using subscription key authentication.")

    def create_speech_config(self) -> speechsdk.SpeechConfig:
        """Create a SpeechConfig using subscription key or managed identity."""
        if self.uses_managed_identity:
            token = self._get_authorization_token()
            return speechsdk.SpeechConfig(auth_token=token, region=self.region)

        return speechsdk.SpeechConfig(
            subscription=self.subscription_key, region=self.region
        )

    def create_translation_config(
        self,
    ) -> speechsdk.translation.SpeechTranslationConfig:
        """Create a SpeechTranslationConfig using subscription key or managed identity."""
        if self.uses_managed_identity:
            token = self._get_authorization_token()
            return speechsdk.translation.SpeechTranslationConfig(
                auth_token=token, region=self.region
            )

        return speechsdk.translation.SpeechTranslationConfig(
            subscription=self.subscription_key, region=self.region
        )

    _cached_credential = None

    def _get_authorization_token(self) -> str:
        """Obtain an authorization token from DefaultAzureCredential.

        Tokens typically expire after ~1 hour. For long-running continuous
        recognition sessions, create a new SpeechConfig periodically
        to refresh the token.
        """
        from azure.identity import DefaultAzureCredential

        if SpeechSettings._cached_credential is None:
            SpeechSettings._cached_credential = DefaultAzureCredential()
        token = SpeechSettings._cached_credential.get_token(_COGNITIVE_SERVICES_SCOPE)
        return token.token

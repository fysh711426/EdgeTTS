namespace EdgeTTS
{
    /// <summary>
    /// Constants for the Edge TTS project.
    /// </summary>
    internal static class Constants
    {
        public static readonly string TRUSTED_CLIENT_TOKEN =
            "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

        public static readonly string WSS_URL =
            "wss://speech.platform.bing.com/consumer/speech/synthesize/" +
            "readaloud/edge/v1?TrustedClientToken=" +
            TRUSTED_CLIENT_TOKEN;

        public static readonly string VOICE_LIST =
            "https://speech.platform.bing.com/consumer/speech/synthesize/" +
            "readaloud/voices/list?trustedclienttoken=" +
            TRUSTED_CLIENT_TOKEN;
    }
}

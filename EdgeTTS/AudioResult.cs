using System.IO;

namespace EdgeTTS
{
    public class AudioResult
    {
        public string Type { get; set; } = "";
        public int Offset { get; set; }
        public int Duration { get; set; }
        public string Text { get; set; } = "";
        public Stream? Data { get; set; } = null;
    }
}

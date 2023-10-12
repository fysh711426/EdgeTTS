using System.Collections.Generic;

namespace EdgeTTS
{
    internal class MetadataModel
    {
        public List<Metadata> Metadata { get; set; } = new();
    }

    internal class Metadata
    {
        public string Type { get; set; } = "";
        public MetadataData Data { get; set; } = new();
    }

    internal class MetadataData
    {
        public int Offset { get; set; }
        public int Duration { get; set; }
        public MetadataDataText text { get; set; } = new();
    }

    internal class MetadataDataText
    {
        public string Text { get; set; } = "";
        public int Length { get; set; }
        public string BoundaryType { get; set; } = "";
    }
}

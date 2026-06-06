using System.Drawing;

namespace FWEledit
{
    public sealed class DescriptionPreviewSegment
    {
        public string Text { get; set; }
        public Color Color { get; set; }
        public float FontScale { get; set; }
        public FontStyle FontStyle { get; set; }
        public bool Underline { get; set; }
    }
}

using System.Drawing;

namespace FWEledit
{
    public sealed class ToolPreviewData
    {
        public string TitleText { get; set; } = string.Empty;
        public Color TitleColor { get; set; } = Color.White;
        public Image IconImage { get; set; }
        public string PreviewText { get; set; } = string.Empty;
    }
}

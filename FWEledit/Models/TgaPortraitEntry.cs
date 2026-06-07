using System.Drawing;

namespace FWEledit
{
    public sealed class TgaPortraitEntry
    {
        public int PathId { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public Bitmap Thumbnail { get; set; }

        public override string ToString()
        {
            return PathId + " - " + (Name ?? string.Empty);
        }
    }
}

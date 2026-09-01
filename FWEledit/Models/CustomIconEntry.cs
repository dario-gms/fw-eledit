using System.Drawing;

namespace FWEledit
{
    public sealed class CustomIconEntry
    {
        public int PathId { get; set; }

        public string Path { get; set; }

        public string Name { get; set; }

        public Bitmap Thumbnail { get; set; }
    }
}

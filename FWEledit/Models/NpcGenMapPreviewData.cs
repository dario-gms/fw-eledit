using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcGenMapPreviewData
    {
        public NpcGenMapPreviewData()
        {
            Markers = new List<NpcGenMapMarker>();
            InitialAreaIndex = -1;
            InitialEntryIndex = -1;
        }

        public string MapDirectory { get; set; }
        public string MapName { get; set; }
        public Bitmap HeightMapImage { get; set; }
        public RectangleF WorldBounds { get; set; }
        public float GridSize { get; set; }
        public string Status { get; set; }
        public int InitialAreaIndex { get; set; }
        public int InitialEntryIndex { get; set; }
        public List<NpcGenMapMarker> Markers { get; private set; }
    }

    public sealed class NpcGenMapMarker
    {
        public int AreaIndex { get; set; }
        public int EntryIndex { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Kind { get; set; }
        public PointF3 Position { get; set; }
        public PointF3 Extents { get; set; }
        public Color Color { get; set; }
        public Bitmap Icon { get; set; }
        public int Count { get; set; }
    }
}

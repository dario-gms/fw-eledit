using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class ModelPickerEntry
    {
        public int Index { get; set; }
        public int PathId { get; set; }
        public string Package { get; set; }
        public string RelativePath { get; set; }
        public string MappedPath { get; set; }
        public Bitmap Icon { get; set; }
        public string IconKey { get; set; }
        public string SearchKey { get; set; }
        public string IndexSearch { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Uses { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string TagText { get; set; }
        public float PreviewScale { get; set; } = 1f;
    }
}

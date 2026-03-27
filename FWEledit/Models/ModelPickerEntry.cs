using System.Drawing;

namespace FWEledit
{
    public sealed class ModelPickerEntry
    {
        public int Index { get; set; }
        public int PathId { get; set; }
        public string Package { get; set; }
        public string RelativePath { get; set; }
        public Bitmap Icon { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int Uses { get; set; }
    }
}

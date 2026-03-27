using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class IconPickerData
    {
        public Bitmap AtlasBitmap { get; set; }
        public int IconWidth { get; set; }
        public int IconHeight { get; set; }
        public int AtlasCols { get; set; }
        public List<IconEntryModel> Entries { get; set; } = new List<IconEntryModel>();
        public Dictionary<int, int> UsageByPathId { get; set; } = new Dictionary<int, int>();
        public Dictionary<string, int> UsageByIconKey { get; set; } = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        public int PendingSelectPathId { get; set; }
    }
}

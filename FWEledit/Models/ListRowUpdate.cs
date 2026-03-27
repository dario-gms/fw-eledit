using System.Drawing;

namespace FWEledit
{
    public sealed class ListRowUpdate
    {
        public int GridRowIndex { get; set; }
        public int ElementIndex { get; set; }
        public object IdValue { get; set; }
        public Image Icon { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool UpdateQualityColor { get; set; }
    }
}

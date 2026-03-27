using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemSelectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
        public List<ValueRowDisplay> Rows { get; set; }
    }
}

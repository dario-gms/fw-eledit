using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ListSelectionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ListIndex { get; set; }
        public string OffsetText { get; set; }
        public bool HasXref { get; set; }
        public List<object[]> Rows { get; set; }
    }
}

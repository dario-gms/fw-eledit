using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ValueChangeResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
        public bool MarkInvalid { get; set; }
        public bool MarkDirty { get; set; }
        public List<ListRowUpdate> ListRowUpdates { get; } = new List<ListRowUpdate>();
    }
}

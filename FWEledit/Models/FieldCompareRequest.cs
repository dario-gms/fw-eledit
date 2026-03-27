using System.Collections.Generic;

namespace FWEledit
{
    public sealed class FieldCompareRequest
    {
        public int ListIndex { get; set; }
        public string LogDirectory { get; set; }
        public List<int> ExcludedFieldIndices { get; set; } = new List<int>();
    }
}

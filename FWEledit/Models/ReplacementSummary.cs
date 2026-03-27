using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ReplacementSummary
    {
        public int ReplacementCount { get; set; }
        public List<string> Warnings { get; } = new List<string>();
    }
}

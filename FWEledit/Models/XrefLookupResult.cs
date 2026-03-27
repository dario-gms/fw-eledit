using System.Collections.Generic;

namespace FWEledit
{
    public sealed class XrefLookupResult
    {
        public bool Success { get; set; }
        public bool HasResults { get; set; }
        public bool HasRules { get; set; }
        public bool IsConversationList { get; set; }
        public string ErrorMessage { get; set; }
        public List<XrefResultRow> Rows { get; } = new List<XrefResultRow>();
    }
}

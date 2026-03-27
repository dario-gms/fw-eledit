namespace FWEledit
{
    public sealed class ElementSearchRequest
    {
        public eListCollection ListCollection { get; set; }
        public string Query { get; set; } = string.Empty;
        public bool MatchCase { get; set; }
        public bool ExactMatch { get; set; }
        public bool SearchAllFields { get; set; }
        public int StartListIndex { get; set; }
        public int StartElementIndex { get; set; }
        public int StartFieldIndex { get; set; }
    }
}

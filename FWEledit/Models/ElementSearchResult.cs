namespace FWEledit
{
    public sealed class ElementSearchResult
    {
        public bool Found { get; set; }
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
        public int FieldIndex { get; set; } = -1;
    }
}

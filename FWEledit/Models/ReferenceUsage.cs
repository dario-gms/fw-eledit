namespace FWEledit
{
    public sealed class ReferenceUsage
    {
        public int SourceListIndex { get; set; }
        public int SourceElementIndex { get; set; }
        public int SourceFieldIndex { get; set; }
        public string SourceListName { get; set; }
        public string SourceItemId { get; set; }
        public string SourceItemName { get; set; }
        public string SourceFieldName { get; set; }
        public string RawValue { get; set; }
    }
}

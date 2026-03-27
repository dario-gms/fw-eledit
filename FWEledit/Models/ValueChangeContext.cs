namespace FWEledit
{
    public sealed class ValueChangeContext
    {
        public ValueChangeRequest Request { get; set; }
        public string EditedField { get; set; } = string.Empty;
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
        public int FieldIndex { get; set; }
        public int GridRow { get; set; }
    }
}

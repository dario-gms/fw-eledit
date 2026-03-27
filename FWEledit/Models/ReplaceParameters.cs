namespace FWEledit
{
    public sealed class ReplaceParameters
    {
        public string ListIndexText { get; set; }
        public string ItemIndexText { get; set; }
        public string FieldIndexText { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Operation { get; set; }
        public decimal Operand { get; set; }
        public bool ReplaceMode { get; set; }
        public bool RecalculateMode { get; set; }
    }
}

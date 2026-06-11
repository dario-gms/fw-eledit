using System.Drawing;

namespace FWEledit
{
    public sealed class ValueRowDisplay
    {
        public int FieldIndex { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string DisplayFieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
        public string RawValue { get; set; } = string.Empty;
        public ItemReferenceOption ResolvedReferenceOption { get; set; }
        public bool IsDirty { get; set; }
        public bool IsInvalid { get; set; }
    }
}

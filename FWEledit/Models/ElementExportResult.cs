namespace FWEledit
{
    public sealed class ElementExportResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}

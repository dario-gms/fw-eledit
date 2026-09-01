namespace FWEledit
{
    public sealed class ElementImportResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int OldId { get; set; }
        public int NewId { get; set; }
        public bool IdChanged { get; set; }
    }
}

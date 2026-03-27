namespace FWEledit
{
    public sealed class ClipboardCopyResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public string ErrorMessage { get; set; }
        public string ClipboardText { get; set; }
    }
}

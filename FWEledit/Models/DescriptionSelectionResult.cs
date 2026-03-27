namespace FWEledit
{
    public sealed class DescriptionSelectionResult
    {
        public bool HasDescription { get; set; }
        public bool IsConversation { get; set; }
        public int ItemId { get; set; }
        public string EditorText { get; set; } = string.Empty;
    }
}

namespace FWEledit
{
    public sealed class ElementBatchAddResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int[] NewIndices { get; set; } = new int[0];
    }
}

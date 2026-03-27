namespace FWEledit
{
    public sealed class ElementCloneResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public int[] NewIndices { get; set; } = new int[0];
    }
}

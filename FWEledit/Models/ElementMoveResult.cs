namespace FWEledit
{
    public sealed class ElementMoveResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public int[] NewSelectedIndices { get; set; } = new int[0];
    }
}

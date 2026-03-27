namespace FWEledit
{
    public sealed class ElementDeleteResult
    {
        public bool Success { get; set; }
        public bool IsConversationList { get; set; }
        public bool DeleteAllBlocked { get; set; }
        public int[] DeletedIndices { get; set; } = new int[0];
    }
}

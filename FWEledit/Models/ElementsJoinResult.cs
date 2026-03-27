namespace FWEledit
{
    public sealed class ElementsJoinResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string WarningMessage { get; set; }
        public bool ShouldUpdateConversationList { get; set; }
        public eListConversation ConversationList { get; set; }
    }
}

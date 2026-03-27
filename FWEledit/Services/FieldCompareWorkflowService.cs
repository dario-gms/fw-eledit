namespace FWEledit
{
    public sealed class FieldCompareWorkflowService
    {
        public bool CanOpen(eListCollection listCollection)
        {
            return listCollection != null;
        }

        public void Show(ISessionService sessionService, eListCollection listCollection, eListConversation conversationList, ref ColorProgressBar.ColorProgressBar progressBar)
        {
            FieldCompare window = new FieldCompare(sessionService, listCollection, conversationList, ref progressBar);
            window.Show();
        }
    }
}

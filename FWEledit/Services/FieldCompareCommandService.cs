using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldCompareCommandService
    {
        public void OpenFieldCompare(
            FieldCompareWorkflowService workflowService,
            ISessionService sessionService,
            eListCollection listCollection,
            eListConversation conversationList,
            ref ColorProgressBar.ColorProgressBar progressBar,
            System.Action<string> showMessage)
        {
            if (workflowService == null)
            {
                return;
            }

            if (!workflowService.CanOpen(listCollection))
            {
                if (showMessage != null)
                {
                    showMessage("No File Loaded!");
                }
                return;
            }

            workflowService.Show(sessionService, listCollection, conversationList, ref progressBar);
        }
    }
}

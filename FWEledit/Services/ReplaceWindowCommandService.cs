using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ReplaceWindowCommandService
    {
        public void OpenReplaceWindow(
            EditorWindowService editorWindowService,
            eListCollection listCollection,
            System.Action<int> refreshListAction,
            System.Action<string> showMessage)
        {
            if (editorWindowService == null)
            {
                return;
            }

            WindowOpenResult result = editorWindowService.ShowReplaceWindow(listCollection);
            if (!result.Success)
            {
                if (showMessage != null)
                {
                    showMessage(result.ErrorMessage ?? "No File Loaded!");
                }
                return;
            }

            if (refreshListAction != null)
            {
                refreshListAction(0);
            }
        }

        public void OpenFieldReplaceWindow(
            EditorWindowService editorWindowService,
            eListCollection listCollection,
            eListConversation conversationList,
            ref ColorProgressBar.ColorProgressBar progressBar,
            System.Action<string> showMessage)
        {
            if (editorWindowService == null)
            {
                return;
            }

            WindowOpenResult result = editorWindowService.ShowFieldReplaceWindow(listCollection, conversationList, ref progressBar);
            if (!result.Success && showMessage != null)
            {
                showMessage(result.ErrorMessage ?? "No File Loaded!");
            }
        }
    }
}

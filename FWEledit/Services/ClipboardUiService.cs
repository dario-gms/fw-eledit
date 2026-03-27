using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ClipboardUiService
    {
        public void CopySelectedElement(
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            ElementClipboardService clipboardService,
            Action<string> showMessage,
            Action<string> setClipboard)
        {
            if (clipboardService == null)
            {
                return;
            }

            ClipboardCopyResult result = clipboardService.BuildClipboardText(listCollection, listIndex, rowIndex);
            if (result.IsConversationList)
            {
                if (showMessage != null && listCollection != null)
                {
                    showMessage("Operation not supported in List " + listCollection.ConversationListIndex.ToString());
                }
                return;
            }
            if (!result.Success)
            {
                if (showMessage != null)
                {
                    showMessage(string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Invalid List" : result.ErrorMessage);
                }
                return;
            }

            if (setClipboard != null)
            {
                setClipboard(result.ClipboardText);
            }
        }
    }
}

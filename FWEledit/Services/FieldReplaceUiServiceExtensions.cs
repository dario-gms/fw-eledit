using System;

namespace FWEledit
{
    public static class FieldReplaceUiServiceExtensions
    {
        public static void OpenFieldReplace(
            this FieldReplaceUiService service,
            ReplaceWindowCommandService replaceWindowCommandService,
            EditorWindowService editorWindowService,
            eListCollection listCollection,
            eListConversation conversationList,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<string> showMessage)
        {
            if (replaceWindowCommandService == null)
            {
                return;
            }

            replaceWindowCommandService.OpenFieldReplaceWindow(
                editorWindowService,
                listCollection,
                conversationList,
                ref progressBar,
                showMessage);
        }
    }
}

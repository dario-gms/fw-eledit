using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowDialogActionsUiService
    {
        public void ShowInfo(
            InfoDialogUiService infoDialogUiService,
            eListCollection listCollection,
            InfoMessageService infoMessageService,
            Action<string> showMessage)
        {
            if (infoDialogUiService == null)
            {
                return;
            }

            infoDialogUiService.ShowInfo(listCollection, infoMessageService, showMessage);
        }

        public void ShowElementsInfo(
            InfoDialogUiService infoDialogUiService,
            ElementsInfoDialogService elementsInfoDialogService,
            ElementsFileInfoService elementsFileInfoService,
            DialogService dialogService,
            IWin32Window owner)
        {
            if (infoDialogUiService == null)
            {
                return;
            }

            infoDialogUiService.ShowElementsInfo(elementsInfoDialogService, elementsFileInfoService, dialogService, owner);
        }

        public void OpenConfig(
            ConfigWindowUiService configWindowUiService,
            ConfigWindowCommandService configWindowCommandService,
            EditorWindowService editorWindowService)
        {
            if (configWindowUiService == null)
            {
                return;
            }

            configWindowUiService.OpenConfig(configWindowCommandService, editorWindowService);
        }

        public void ShowClassMask(
            ClassMaskUiService classMaskUiService,
            ClassMaskCommandService commandService,
            EditorWindowService editorWindowService)
        {
            if (classMaskUiService == null)
            {
                return;
            }

            classMaskUiService.ShowClassMask(commandService, editorWindowService);
        }

        public void ShowRules(
            RulesWindowUiService rulesWindowUiService,
            RulesWindowService rulesWindowService,
            ISessionService sessionService,
            ref ColorProgressBar.ColorProgressBar progressBar)
        {
            if (rulesWindowUiService == null)
            {
                return;
            }

            rulesWindowUiService.ShowRules(rulesWindowService, sessionService, ref progressBar);
        }

        public void OpenFieldCompare(
            FieldCompareCommandService commandService,
            FieldCompareWorkflowService workflowService,
            ISessionService sessionService,
            eListCollection listCollection,
            eListConversation conversationList,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<string> showMessage)
        {
            if (commandService == null)
            {
                return;
            }

            commandService.OpenFieldCompare(
                workflowService,
                sessionService,
                listCollection,
                conversationList,
                ref progressBar,
                showMessage);
        }

        public void ExecuteJoin(
            CursorScopeService cursorScopeService,
            Action<Cursor> setCursor,
            ElementsJoinUiService joinUiService,
            ElementsJoinWorkflowService joinWorkflowService,
            ISessionService sessionService,
            eListCollection listCollection,
            ComboBox listCombo,
            ref ColorProgressBar.ColorProgressBar progressBar,
            Action<int> refreshList,
            Action<eListConversation> updateConversationList,
            Action<string> showMessage,
            Action<string> showWarning)
        {
            if (joinUiService == null)
            {
                return;
            }

            try
            {
                if (setCursor != null)
                {
                    setCursor(Cursors.WaitCursor);
                }

                joinUiService.ExecuteJoin(
                    joinWorkflowService,
                    sessionService,
                    listCollection,
                    listCombo,
                    ref progressBar,
                    refreshList,
                    updateConversationList,
                    showMessage,
                    showWarning);
            }
            finally
            {
                if (setCursor != null)
                {
                    setCursor(Cursors.Default);
                }
            }
        }

        public void ShowAbout(
            AboutWindowCommandService aboutWindowCommandService,
            EditorWindowService editorWindowService)
        {
            if (aboutWindowCommandService == null)
            {
                return;
            }

            aboutWindowCommandService.ShowAbout(editorWindowService);
        }
    }
}

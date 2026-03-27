using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsJoinUiService
    {
        public void ExecuteJoin(
            ElementsJoinWorkflowService workflowService,
            ISessionService sessionService,
            eListCollection listCollection,
            ComboBox listComboBox,
            ref ColorProgressBar.ColorProgressBar progressBar,
            System.Action<int> refreshListAction,
            System.Action<eListConversation> updateConversationList,
            System.Action<string> showMessage,
            System.Action<string> showWarning)
        {
            if (workflowService == null)
            {
                return;
            }

            using (JoinWindow joinWindow = new JoinWindow(sessionService))
            {
                if (joinWindow.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                ElementsJoinOptions options = new ElementsJoinOptions
                {
                    SourceFilePath = joinWindow.FileName,
                    LogDirectory = joinWindow.LogDirectory,
                    AddNew = joinWindow.AddNew,
                    ReplaceChanged = joinWindow.ReplaceChanged,
                    RemoveMissing = joinWindow.RemoveMissing,
                    BackupNew = joinWindow.BackupNew,
                    BackupChanged = joinWindow.BackupChanged,
                    BackupMissing = joinWindow.BackupMissing
                };

                ElementsJoinResult result = workflowService.Join(listCollection, options, ref progressBar);
                if (!result.Success)
                {
                    if (showMessage != null)
                    {
                        showMessage(result.ErrorMessage ?? "LOADING ERROR!\nThis error mostly occurs of configuration and elements.data mismatch");
                    }
                    return;
                }

                if (!string.IsNullOrWhiteSpace(result.WarningMessage) && showWarning != null)
                {
                    showWarning(result.WarningMessage);
                }

                if (result.ShouldUpdateConversationList && updateConversationList != null)
                {
                    updateConversationList(result.ConversationList);
                }

                if (listCollection != null && listComboBox != null)
                {
                    for (int l = 0; l < listCollection.Lists.Length; l++)
                    {
                        listComboBox.Items[l] = "[" + l + "]: " + listCollection.Lists[l].listName + " (" + listCollection.Lists[l].elementValues.Length + ")";
                    }
                }

                if (listComboBox != null && listComboBox.SelectedIndex > -1 && refreshListAction != null)
                {
                    refreshListAction(listComboBox.SelectedIndex);
                }

                if (progressBar != null)
                {
                    progressBar.Value = 0;
                }
            }
        }
    }
}

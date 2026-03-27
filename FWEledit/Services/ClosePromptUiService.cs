using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ClosePromptUiService
    {
        public void HandleClosing(
            bool suppressPrompt,
            bool hasUnsavedChanges,
            DescriptionViewModel descriptionViewModel,
            ClosePromptService closePromptService,
            System.Func<bool> saveCurrentSession,
            System.Action persistNavigation,
            System.Action flushNavigation,
            FormClosingEventArgs e)
        {
            if (suppressPrompt || e == null)
            {
                return;
            }

            persistNavigation?.Invoke();
            flushNavigation?.Invoke();

            bool cancel = closePromptService.ShouldCancelClose(
                hasUnsavedChanges,
                descriptionViewModel != null && descriptionViewModel.HasPendingChanges,
                saveCurrentSession,
                () => MessageBox.Show(
                    "There are pending changes. Save before closing?",
                    "FWEledit",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question));

            if (cancel)
            {
                e.Cancel = true;
            }
        }
    }
}

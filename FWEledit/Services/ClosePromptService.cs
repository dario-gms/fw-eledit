using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ClosePromptService
    {
        public bool ShouldCancelClose(
            bool hasUnsavedChanges,
            bool hasPendingDescriptionChanges,
            Func<bool> saveCurrentSession,
            Func<DialogResult> prompt)
        {
            if (!hasUnsavedChanges && !hasPendingDescriptionChanges)
            {
                return false;
            }

            DialogResult result = prompt != null ? prompt() : DialogResult.No;
            if (result == DialogResult.Cancel)
            {
                return true;
            }
            if (result == DialogResult.No)
            {
                return false;
            }

            if (saveCurrentSession == null)
            {
                return false;
            }

            bool saved = saveCurrentSession();
            return !saved;
        }
    }
}

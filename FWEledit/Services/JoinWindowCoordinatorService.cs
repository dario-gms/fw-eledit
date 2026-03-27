using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class JoinWindowCoordinatorService
    {
        public void ApplyTheme(
            JoinWindowThemeUiService themeUiService,
            CacheSave database,
            Form owner,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            Label label5,
            TextBox elementFileBox,
            TextBox logDirBox,
            CheckBox addNewBox,
            CheckBox backupNewBox,
            CheckBox replaceChangedBox,
            CheckBox backupChangedBox,
            CheckBox removeMissingBox,
            CheckBox backupMissingBox,
            Button browseElementButton,
            Button browseLogButton,
            Button cancelButton,
            Button okButton)
        {
            if (themeUiService == null)
            {
                return;
            }

            themeUiService.ApplyTheme(
                database,
                owner,
                label1,
                label2,
                label3,
                label4,
                label5,
                elementFileBox,
                logDirBox,
                addNewBox,
                backupNewBox,
                replaceChangedBox,
                backupChangedBox,
                removeMissingBox,
                backupMissingBox,
                browseElementButton,
                browseLogButton,
                cancelButton,
                okButton);
        }

        public void HandleOk(Form owner)
        {
            if (owner == null)
            {
                return;
            }

            owner.DialogResult = DialogResult.OK;
            owner.Close();
        }

        public void HandleCancel(Form owner)
        {
            if (owner == null)
            {
                return;
            }

            owner.DialogResult = DialogResult.Cancel;
            owner.Close();
        }

        public void HandleBrowseElement(
            JoinWindowDialogUiService dialogUiService,
            DialogService dialogService,
            TextBox elementFileBox,
            string initialDirectory,
            IWin32Window owner)
        {
            if (dialogUiService == null)
            {
                return;
            }

            dialogUiService.BrowseElementFile(dialogService, elementFileBox, initialDirectory, owner);
        }

        public void HandleBrowseLog(
            JoinWindowDialogUiService dialogUiService,
            GameFolderDialogService folderDialogService,
            TextBox logDirBox)
        {
            if (dialogUiService == null)
            {
                return;
            }

            dialogUiService.BrowseLogDirectory(folderDialogService, logDirBox);
        }
    }
}

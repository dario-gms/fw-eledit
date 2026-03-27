using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldReplaceWindowCoordinatorService
    {
        public void ApplyTheme(
            FieldReplaceThemeUiService themeUiService,
            CacheSave database,
            Form owner,
            ColorProgressBar.ColorProgressBar progressBar,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            ComboBox fieldCombo,
            ComboBox listCombo,
            TextBox elementPathBox,
            TextBox logDirBox,
            Button browseElementButton,
            Button browseLogButton,
            Button loadButton,
            Button cancelButton,
            Button replaceButton)
        {
            if (themeUiService == null)
            {
                return;
            }

            themeUiService.ApplyTheme(
                database,
                owner,
                progressBar,
                label1,
                label2,
                label3,
                label4,
                fieldCombo,
                listCombo,
                elementPathBox,
                logDirBox,
                browseElementButton,
                browseLogButton,
                loadButton,
                cancelButton,
                replaceButton);
        }

        public void DrawComboBoxItem(
            ThemeComboBoxDrawService drawService,
            ComboBoxThemeRendererService rendererService,
            CacheSave database,
            object sender,
            DrawItemEventArgs e)
        {
            if (drawService == null || database == null || database.arrTheme == null)
            {
                return;
            }

            drawService.DrawItem(rendererService, sender, e, database.arrTheme);
        }

        public void HandleBrowseElement(
            FieldReplaceUiService uiService,
            DialogService dialogService,
            TextBox elementPathBox,
            CacheSave database,
            string initialDirectory,
            IWin32Window owner)
        {
            if (uiService == null)
            {
                return;
            }

            uiService.BrowseElementFile(dialogService, elementPathBox, database, initialDirectory, owner);
        }

        public void HandleBrowseLogDir(
            FieldReplaceUiService uiService,
            GameFolderDialogService folderDialogService,
            TextBox logDirBox)
        {
            if (uiService == null)
            {
                return;
            }

            uiService.BrowseLogDirectory(folderDialogService, logDirBox);
        }

        public void HandleLoadElement(
            FieldReplaceUiService uiService,
            FieldReplaceViewModel viewModel,
            string elementPath,
            ref ColorProgressBar.ColorProgressBar progressBar,
            TextBox elementPathBox,
            Action<string> showMessage,
            Action<Cursor> setCursor)
        {
            if (uiService == null || viewModel == null)
            {
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);
            uiService.LoadOtherCollection(
                viewModel,
                elementPath,
                ref progressBar,
                elementPathBox,
                showMessage);
            setCursor?.Invoke(Cursors.Default);
        }

        public void HandleListChanged(
            FieldReplaceUiService uiService,
            FieldReplaceViewModel viewModel,
            int listIndex,
            ComboBox fieldCombo)
        {
            if (uiService == null || viewModel == null)
            {
                return;
            }

            uiService.PopulateFieldCombo(viewModel, listIndex, fieldCombo);
        }

        public void HandleReplace(
            FieldReplaceUiService uiService,
            FieldReplaceViewModel viewModel,
            int listIndex,
            int fieldIndex,
            string logDir,
            ColorProgressBar.ColorProgressBar progressBar,
            Action incrementProgress,
            Action<string> showMessage)
        {
            if (uiService == null || viewModel == null)
            {
                return;
            }

            uiService.ExecuteReplace(
                viewModel,
                listIndex,
                fieldIndex,
                logDir,
                progressBar,
                incrementProgress,
                showMessage);
        }

        public bool CanReplace(
            FieldReplaceUiService uiService,
            FieldReplaceViewModel viewModel,
            int listIndex,
            int fieldIndex,
            TextBox elementPathBox)
        {
            if (uiService == null || viewModel == null)
            {
                return false;
            }

            return uiService.CanReplace(viewModel, listIndex, fieldIndex, elementPathBox);
        }
    }
}

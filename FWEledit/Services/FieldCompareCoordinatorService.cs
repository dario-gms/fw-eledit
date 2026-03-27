using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldCompareCoordinatorService
    {
        public void ApplyTheme(
            FieldCompareThemeUiService themeUiService,
            CacheSave database,
            Form owner,
            ComboBox listCombo,
            ColorProgressBar.ColorProgressBar progressBar,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            TextBox elementPathBox,
            TextBox logDirBox,
            DataGridView fieldsGrid,
            Button browseElementButton,
            Button browseLogButton,
            Button cancelButton,
            Button compareButton,
            Button loadButton)
        {
            if (themeUiService == null)
            {
                return;
            }

            themeUiService.ApplyTheme(
                database,
                owner,
                listCombo,
                progressBar,
                label1,
                label2,
                label3,
                label4,
                elementPathBox,
                logDirBox,
                fieldsGrid,
                browseElementButton,
                browseLogButton,
                cancelButton,
                compareButton,
                loadButton);
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
            FieldCompareUiService uiService,
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
            FieldCompareUiService uiService,
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
            FieldCompareUiService uiService,
            FieldCompareViewModel viewModel,
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
            FieldCompareUiService uiService,
            FieldCompareViewModel viewModel,
            int listIndex,
            DataGridView fieldsGrid)
        {
            if (uiService == null || viewModel == null)
            {
                return;
            }

            uiService.UpdateFieldGrid(viewModel, listIndex, fieldsGrid);
        }

        public void HandleCompare(
            FieldCompareUiService uiService,
            FieldCompareViewModel viewModel,
            int listIndex,
            string logDir,
            DataGridView fieldsGrid,
            ColorProgressBar.ColorProgressBar progressBar,
            Action incrementProgress,
            Action<string> showMessage)
        {
            if (uiService == null || viewModel == null)
            {
                return;
            }

            uiService.ExecuteCompare(
                viewModel,
                listIndex,
                logDir,
                fieldsGrid,
                progressBar,
                incrementProgress,
                showMessage);
        }

        public bool CanCompare(
            FieldCompareUiService uiService,
            FieldCompareViewModel viewModel,
            int listIndex,
            DataGridView fieldsGrid,
            TextBox elementPathBox)
        {
            if (uiService == null || viewModel == null)
            {
                return false;
            }

            return uiService.CanCompare(viewModel, listIndex, fieldsGrid, elementPathBox);
        }
    }
}

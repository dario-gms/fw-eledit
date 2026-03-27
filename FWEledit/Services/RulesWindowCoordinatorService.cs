using System;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class RulesWindowCoordinatorService
    {
        public void ApplyTheme(
            RulesThemeUiService themeUiService,
            CacheSave database,
            Form owner,
            ComboBox listCombo,
            GroupBox groupBox1,
            GroupBox groupBox2,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            RadioButton baseOffsetRadio,
            RadioButton recentOffsetRadio,
            CheckBox removeListBox,
            TextBox baseFileBox,
            TextBox baseOffsetBox,
            TextBox baseVersionBox,
            TextBox recentFileBox,
            TextBox recentOffsetBox,
            TextBox recentVersionBox,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            DataGridViewColumn column6,
            Button browseBaseButton,
            Button browseRecentButton,
            Button exportButton,
            Button importButton,
            Button viewButton)
        {
            if (themeUiService == null)
            {
                return;
            }

            themeUiService.ApplyTheme(
                database,
                owner,
                listCombo,
                groupBox1,
                groupBox2,
                label1,
                label2,
                label3,
                label4,
                baseOffsetRadio,
                recentOffsetRadio,
                removeListBox,
                baseFileBox,
                baseOffsetBox,
                baseVersionBox,
                recentFileBox,
                recentOffsetBox,
                recentVersionBox,
                fieldsGrid,
                valuesGrid,
                column6,
                browseBaseButton,
                browseRecentButton,
                exportButton,
                importButton,
                viewButton);
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

        public void HandleBrowse(
            DialogService dialogService,
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            string initialDirectory,
            IWin32Window owner,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            ComboBox listCombo,
            DataGridViewColumn column1,
            DataGridViewColumn column2,
            DataGridViewColumn column7,
            DataGridViewColumn column8,
            bool isBase,
            ref ColorProgressBar.ColorProgressBar progressBar,
            TextBox baseFileBox,
            TextBox baseVersionBox,
            TextBox recentFileBox,
            TextBox recentVersionBox,
            Action<Cursor> setCursor)
        {
            if (dialogService == null || actionsService == null || viewModel == null)
            {
                return;
            }

            string filePath = dialogService.ShowOpenFile(
                "Elements File",
                "Elements File (*.data)|*.data|All Files (*.*)|*.*",
                initialDirectory,
                owner);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);
            actionsService.PrepareForLoad(
                fieldsGrid,
                valuesGrid,
                listCombo,
                column1,
                column2,
                column7,
                column8);

            actionsService.LoadCollection(
                viewModel,
                filePath,
                isBase,
                ref progressBar,
                baseFileBox,
                baseVersionBox,
                recentFileBox,
                recentVersionBox);
            actionsService.PopulateListsIfReady(viewModel, listCombo);
            if (progressBar != null)
            {
                progressBar.Value = 0;
            }
            setCursor?.Invoke(Cursors.Default);
        }

        public void HandleListChanged(
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            int listIndex,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            DataGridViewColumn column1,
            DataGridViewColumn column2,
            DataGridViewColumn column7,
            DataGridViewColumn column8,
            CheckBox removeListBox,
            RadioButton baseOffsetRadio,
            RadioButton recentOffsetRadio,
            TextBox baseOffsetBox,
            TextBox recentOffsetBox,
            Action<Cursor> setCursor)
        {
            if (actionsService == null || viewModel == null)
            {
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);
            actionsService.ApplyListSelection(
                viewModel,
                listIndex,
                fieldsGrid,
                valuesGrid,
                column1,
                column2,
                column7,
                column8,
                removeListBox,
                baseOffsetRadio,
                recentOffsetRadio,
                baseOffsetBox,
                recentOffsetBox);
            setCursor?.Invoke(Cursors.Default);
        }

        public void HandleRemoveListChanged(
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            int listIndex,
            bool removeList)
        {
            if (actionsService == null || viewModel == null)
            {
                return;
            }

            actionsService.ToggleRemoveList(viewModel, listIndex, removeList);
        }

        public void HandleOffsetChanged(
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            int listIndex,
            bool useBaseOffset)
        {
            if (actionsService == null || viewModel == null)
            {
                return;
            }

            actionsService.ToggleOffset(viewModel, listIndex, useBaseOffset);
        }

        public void HandleFieldClick(
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            int listIndex,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            DataGridViewColumn column1,
            DataGridViewColumn column2,
            DataGridViewColumn column7,
            DataGridViewColumn column8,
            DataGridViewCellEventArgs args,
            CheckBox removeListBox,
            RadioButton baseOffsetRadio,
            RadioButton recentOffsetRadio,
            TextBox baseOffsetBox,
            TextBox recentOffsetBox,
            Action<Cursor> setCursor)
        {
            if (actionsService == null || viewModel == null || args == null)
            {
                return;
            }

            if (args.ColumnIndex == 3 && fieldsGrid.FirstDisplayedCell != null)
            {
                int displayRow = fieldsGrid.FirstDisplayedCell.RowIndex;
                int displayCol = fieldsGrid.FirstDisplayedCell.ColumnIndex;
                actionsService.HandleFieldClick(
                    viewModel,
                    listIndex,
                    fieldsGrid,
                    valuesGrid,
                    column7,
                    column8,
                    args.RowIndex,
                    args.ColumnIndex);
                actionsService.ApplyListSelection(
                    viewModel,
                    listIndex,
                    fieldsGrid,
                    valuesGrid,
                    column1,
                    column2,
                    column7,
                    column8,
                    removeListBox,
                    baseOffsetRadio,
                    recentOffsetRadio,
                    baseOffsetBox,
                    recentOffsetBox);
                if (fieldsGrid.Rows.Count > 0)
                {
                    fieldsGrid.FirstDisplayedCell = fieldsGrid.Rows[displayRow].Cells[displayCol];
                }
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);
            actionsService.HandleFieldClick(
                viewModel,
                listIndex,
                fieldsGrid,
                valuesGrid,
                column7,
                column8,
                args.RowIndex,
                args.ColumnIndex);
            setCursor?.Invoke(Cursors.Default);
        }

        public void HandleShowRules(RulesWindowActionsService actionsService, RulesWindowViewModel viewModel)
        {
            if (actionsService == null || viewModel == null)
            {
                return;
            }

            actionsService.ShowRules(viewModel);
        }

        public void HandleImportRules(
            DialogService dialogService,
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            string initialDirectory,
            IWin32Window owner,
            Action refreshList,
            Action<Cursor> setCursor)
        {
            if (dialogService == null || actionsService == null || viewModel == null)
            {
                return;
            }

            if (viewModel.Rules == null)
            {
                return;
            }

            string filePath = dialogService.ShowOpenFile(
                "Rules File",
                "Rules File (*.rules)|*.rules|All Files (*.*)|*.*",
                initialDirectory,
                owner);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);
            actionsService.ImportRules(viewModel, filePath);
            refreshList?.Invoke();
            setCursor?.Invoke(Cursors.Default);
        }

        public void HandleExportRules(
            DialogService dialogService,
            RulesWindowActionsService actionsService,
            RulesWindowViewModel viewModel,
            string initialDirectory,
            IWin32Window owner,
            Action<Cursor> setCursor)
        {
            if (dialogService == null || actionsService == null || viewModel == null)
            {
                return;
            }

            if (viewModel.Rules == null || viewModel.BaseCollection == null || viewModel.RecentCollection == null)
            {
                return;
            }

            string defaultName = "PW_v" + viewModel.RecentCollection.Version.ToString()
                + " = PW_v" + viewModel.BaseCollection.Version.ToString() + ".rules";
            string filePath = dialogService.ShowSaveFile(
                "Rules File",
                "Rules File (*.rules)|*.rules|All Files (*.*)|*.*",
                initialDirectory,
                defaultName,
                owner);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            setCursor?.Invoke(Cursors.AppStarting);
            actionsService.ExportRules(viewModel, filePath);
            setCursor?.Invoke(Cursors.Default);
        }
    }
}

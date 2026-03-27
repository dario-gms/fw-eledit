using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigWindowCoordinatorService
    {
        public void HandleLoad(
            ConfigFileLoadUiService loadUiService,
            IDialogService dialogService,
            ConfigWindowViewModel viewModel,
            ComboBox listCombo,
            TextBox conversationIndexBox,
            string configsPath,
            IWin32Window owner)
        {
            if (loadUiService == null)
            {
                return;
            }

            loadUiService.LoadConfig(
                dialogService,
                viewModel,
                listCombo,
                conversationIndexBox,
                configsPath,
                owner);
        }

        public void HandleSave(
            ConfigFileSaveUiService saveUiService,
            IDialogService dialogService,
            ConfigWindowViewModel viewModel,
            ConfigData data,
            string configsPath,
            IWin32Window owner)
        {
            if (saveUiService == null || data == null)
            {
                return;
            }

            saveUiService.SaveConfig(
                dialogService,
                viewModel,
                data,
                configsPath,
                owner);
        }

        public void HandleListChanged(
            ConfigListDisplayUiService listDisplayUiService,
            ConfigData data,
            int listIndex,
            TextBox listNameBox,
            TextBox offsetBox,
            DataGridView grid)
        {
            if (listDisplayUiService == null || data == null)
            {
                return;
            }

            listDisplayUiService.ApplySelection(
                data,
                listIndex,
                listNameBox,
                offsetBox,
                grid);
        }

        public void HandleRowChanged(
            ConfigRowUpdateUiService rowUpdateUiService,
            ConfigData data,
            int listIndex,
            DataGridViewCellEventArgs args,
            DataGridView grid,
            IDialogService dialogService,
            IWin32Window owner)
        {
            if (rowUpdateUiService == null || data == null || args == null)
            {
                return;
            }

            rowUpdateUiService.ApplyChange(
                data,
                listIndex,
                args.RowIndex,
                args.ColumnIndex,
                grid,
                dialogService,
                owner);
        }

        public void HandleAddRow(
            ConfigRowInsertUiService rowInsertUiService,
            ConfigData data,
            int listIndex,
            DataGridView grid,
            Action refreshList)
        {
            if (rowInsertUiService == null || data == null || listIndex < 0)
            {
                return;
            }

            int currentRowIndex = rowInsertUiService.InsertRow(data, listIndex, grid);

            refreshList?.Invoke();
            if (currentRowIndex > -1 && currentRowIndex < grid.Rows.Count)
            {
                grid.CurrentCell = grid.Rows[currentRowIndex].Cells[0];
            }
        }

        public void HandleCopyRow(
            ConfigRowCopyService rowCopyService,
            ConfigData data,
            int listIndex,
            DataGridView grid,
            GridCellSelectionService selectionService,
            Action<string[], string[]> setCopiedFields)
        {
            if (rowCopyService == null || data == null || listIndex < 0)
            {
                return;
            }

            string[] names;
            string[] types;
            rowCopyService.CopySelection(
                data,
                listIndex,
                grid,
                selectionService,
                out names,
                out types);

            setCopiedFields?.Invoke(names, types);
        }

        public void HandlePasteRow(
            ConfigRowPasteUiService rowPasteUiService,
            ConfigData data,
            int listIndex,
            DataGridView grid,
            string[] copiedNames,
            string[] copiedTypes,
            Action refreshList)
        {
            if (rowPasteUiService == null || data == null || listIndex < 0)
            {
                return;
            }

            int currentRowIndex = rowPasteUiService.PasteSelection(
                data,
                listIndex,
                grid,
                copiedNames,
                copiedTypes);

            refreshList?.Invoke();
            if (currentRowIndex > -1 && currentRowIndex < grid.Rows.Count)
            {
                grid.CurrentCell = grid.Rows[currentRowIndex].Cells[0];
            }
        }

        public void HandleDeleteRow(
            ConfigRowDeleteUiService rowDeleteUiService,
            ConfigData data,
            int listIndex,
            DataGridView grid,
            GridCellSelectionService selectionService,
            Action refreshList)
        {
            if (rowDeleteUiService == null || data == null || listIndex < 0)
            {
                return;
            }

            int currentRowIndex = rowDeleteUiService.DeleteSelection(
                data,
                listIndex,
                grid,
                selectionService);

            refreshList?.Invoke();
            if (currentRowIndex > -1 && currentRowIndex < grid.Rows.Count)
            {
                grid.CurrentCell = grid.Rows[currentRowIndex].Cells[0];
            }
        }

        public void HandleAddList(
            ConfigListMutationUiService listMutationUiService,
            ConfigData data,
            ComboBox listCombo)
        {
            if (listMutationUiService == null || data == null)
            {
                return;
            }

            listMutationUiService.AddList(data, listCombo);
        }

        public void HandleDeleteList(
            ConfigListMutationUiService listMutationUiService,
            ConfigData data,
            ComboBox listCombo,
            DataGridView grid)
        {
            if (listMutationUiService == null || data == null)
            {
                return;
            }

            listMutationUiService.DeleteList(data, listCombo, grid);
        }

        public void HandleSetSelectedNames(
            ConfigFieldSetUiService fieldSetUiService,
            ConfigData data,
            DataGridView grid,
            string value)
        {
            if (fieldSetUiService == null || data == null)
            {
                return;
            }

            fieldSetUiService.SetSelectedFieldNames(grid, value);
        }

        public void HandleSetSelectedTypes(
            ConfigFieldSetUiService fieldSetUiService,
            ConfigData data,
            DataGridView grid,
            string value)
        {
            if (fieldSetUiService == null || data == null)
            {
                return;
            }

            fieldSetUiService.SetSelectedFieldTypes(grid, value);
        }

        public void HandleConversationIndexChanged(
            ConfigListUpdateUiService listUpdateUiService,
            ConfigData data,
            string value)
        {
            if (listUpdateUiService == null || data == null)
            {
                return;
            }

            listUpdateUiService.UpdateConversationListIndex(data, value);
        }

        public void HandleListNameChanged(
            ConfigListUpdateUiService listUpdateUiService,
            ConfigData data,
            ComboBox listCombo,
            string value)
        {
            if (listUpdateUiService == null || data == null)
            {
                return;
            }

            listUpdateUiService.UpdateListName(data, listCombo, value);
        }

        public void HandleListOffsetChanged(
            ConfigListUpdateUiService listUpdateUiService,
            ConfigData data,
            int listIndex,
            string value)
        {
            if (listUpdateUiService == null || data == null)
            {
                return;
            }

            listUpdateUiService.UpdateListOffset(data, listIndex, value);
        }

        public void HandleScanSequel(
            ConfigSequelScannerService scannerService,
            ConfigData data,
            string configsPath,
            IWin32Window owner)
        {
            if (scannerService == null || data == null)
            {
                return;
            }

            scannerService.RunScan(data, configsPath, owner);
        }
    }
}

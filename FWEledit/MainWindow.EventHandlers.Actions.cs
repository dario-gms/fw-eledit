using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void click_deleteItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleDeleteSelected(
                mainWindowElementActionsUiService,
                elementDeleteCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementDeletionUiService,
                comboBox_lists,
                viewModel,
                () => change_item(null, null));
		}


        private void click_cloneItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleCloneSelected(
                mainWindowElementActionsUiService,
                elementCloneCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementCloneUiService,
                comboBox_lists,
                viewModel,
                rowIndex => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                () => change_list(null, null),
                () => change_item(null, null),
                index => listDisplayService.GetFriendlyListName(sessionService.ListCollection.Lists[index].listName));

            InvalidateItemReferenceOptionCaches();
            if (!referenceIndexReady)
            {
                ScheduleVisibleReferenceCountRefresh();
                return;
            }

            int[] selectedGridRows = gridSelectionService != null
                ? gridSelectionService.GetSelectedIndices(dataGridView_elems)
                : new int[0];

            if (selectedGridRows.Length == 0)
            {
                ScheduleVisibleReferenceCountRefresh();
                return;
            }

            for (int i = 0; i < selectedGridRows.Length; i++)
            {
                int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    selectedGridRows[i],
                    dataGridView_elems);

                if (elementIndex < 0)
                {
                    continue;
                }

                UpdateReferenceIndexForEditedElement(listIndex, elementIndex);
            }
		}


        private void click_exportItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleExportSelected(
                mainWindowElementActionsUiService,
                elementExportUiService,
                elementExportCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                cpb2);
		}


        private void click_importItem(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleImportSingle(
                mainWindowElementActionsUiService,
                elementImportUiService,
                elementImportCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                rowIndex => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                () => change_list(null, null),
                rowIndex =>
                {
                    if (rowIndex >= 0 && rowIndex < dataGridView_elems.Rows.Count)
                    {
                        dataGridView_elems.Rows[rowIndex].Selected = true;
                    }
                },
                viewModel);
		}


        private void click_addItems(object sender, EventArgs ea)
		{
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleAddMultiple(
                mainWindowElementActionsUiService,
                elementImportUiService,
                elementBatchAddCommandService,
                sessionService,
                listIndex,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                cpb2,
                comboBox_lists,
                dataGridView_elems,
                viewModel,
                rowIndex => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                () => change_list(null, null),
                () => change_item(null, null),
                index => "[" + index + "]: " + sessionService.ListCollection.Lists[index].listName + " (" + sessionService.ListCollection.Lists[index].elementValues.Length + ")");
		}


        private void click_moveItemsToTop(object sender, EventArgs ea)
        {
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleMoveToTop(
                mainWindowElementActionsUiService,
                elementMoveCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementMoveUiService,
                viewModel,
                () => change_list(null, null));
        }


        private void click_moveItemsToEnd(object sender, EventArgs ea)
        {
            int listIndex = comboBox_lists.SelectedIndex;
            mainWindowActionsCoordinatorService.HandleMoveToEnd(
                mainWindowElementActionsUiService,
                elementMoveCommandService,
                sessionService,
                listIndex,
                dataGridView_elems,
                gridSelectionService,
                elementListMutationService,
                elementMoveUiService,
                viewModel,
                () => change_list(null, null));
        }


        private void click_SetValue(object sender, EventArgs e)
		{
            ApplyRawValueEditorToCurrentCell();
		}


        private async void click_exportItemPackage(object sender, EventArgs ea)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || sessionService.AssetManager == null
                || dataGridView_elems == null)
            {
                MessageBox.Show("Load elements.data before exporting an item package.");
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (!elementImportExportUiService.ValidateNotConversationList(sessionService.ListCollection, listIndex))
            {
                return;
            }

            int rowIndex = dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1;
            if (rowIndex < 0)
            {
                int[] selected = gridSelectionService.GetSelectedIndices(dataGridView_elems);
                rowIndex = selected.Length > 0 ? selected[0] : -1;
            }

            int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                sessionService.ListCollection,
                listIndex,
                rowIndex,
                dataGridView_elems);
            if (elementIndex < 0)
            {
                MessageBox.Show("Select one item to export.");
                return;
            }

            string id = sessionService.ListCollection.GetValue(listIndex, elementIndex, 0);
            string name = ResolveItemNameForTransferPackage(listIndex, elementIndex);
            string safeName = BuildSafeItemTransferFileName(id + " - " + name);

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "FWEledit item package (*.fweitem)|*.fweitem|All files (*.*)|*.*";
                dialog.FileName = safeName + ".fweitem";
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                using (ItemTransferProgressWindow progressWindow = new ItemTransferProgressWindow("Export Item Package"))
                {
                    progressWindow.StartPosition = FormStartPosition.CenterParent;
                    progressWindow.Show(this);

                    ItemTransferExportResult result;
                    try
                    {
                        result = await Task.Run(() => itemTransferPackageService.ExportItemPackage(
                            sessionService.ListCollection,
                            sessionService.Database,
                            sessionService.AssetManager,
                            listIndex,
                            elementIndex,
                            dialog.FileName,
                            progressWindow.UpdateProgress,
                            progressWindow.Cancellation.Token));
                    }
                    finally
                    {
                        progressWindow.AllowCloseAndClose();
                    }

                    if (!result.Success)
                    {
                        MessageBox.Show(result.ErrorMessage ?? "Failed to export item package.");
                        return;
                    }

                    string message = "Item package exported.\nAssets: " + result.AssetCount.ToString();
                    if (result.MissingAssetCount > 0)
                    {
                        message += "\nMissing assets: " + result.MissingAssetCount.ToString();
                    }
                    MessageBox.Show(message);
                }
            }
        }


        private void click_importItemPackage(object sender, EventArgs ea)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || sessionService.AssetManager == null)
            {
                MessageBox.Show("Load elements.data before importing an item package.");
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "FWEledit item package (*.fweitem)|*.fweitem|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK || !File.Exists(dialog.FileName))
                {
                    return;
                }

                Cursor previousCursor = Cursor;
                Cursor = Cursors.AppStarting;
                try
                {
                    ItemTransferImportResult result = itemTransferPackageService.ImportItemPackage(
                        sessionService.ListCollection,
                        sessionService.Database,
                        sessionService.AssetManager,
                        idGenerationService,
                        dialog.FileName);

                    if (!result.Success)
                    {
                        MessageBox.Show(result.ErrorMessage ?? "Failed to import item package.");
                        return;
                    }

                    mainWindowDirtyTrackingService.MarkRowDirty(
                        dirtyStateTracker,
                        listDisplayService,
                        ref viewModel.HasUnsavedChanges,
                        result.TargetListIndex,
                        result.NewItemIndex);
                    viewModel.HasUnsavedChanges = true;

                    if (result.TargetListIndex >= 0 && result.TargetListIndex < comboBox_lists.Items.Count)
                    {
                        comboBox_lists.SelectedIndex = result.TargetListIndex;
                        ListComboPopulationService.SetItemTextPreservingIcon(
                            comboBox_lists,
                            result.TargetListIndex,
                            BuildItemTransferListLabel(result.TargetListIndex));
                    }

                    change_list(null, null);
                    if (result.NewItemIndex >= 0 && result.NewItemIndex < dataGridView_elems.Rows.Count)
                    {
                        dataGridView_elems.ClearSelection();
                        dataGridView_elems.Rows[result.NewItemIndex].Selected = true;
                        dataGridView_elems.CurrentCell = dataGridView_elems.Rows[result.NewItemIndex].Cells[0];
                        dataGridView_elems.FirstDisplayedScrollingRowIndex = result.NewItemIndex;
                    }
                    change_item(null, null);

                    InvalidateItemReferenceOptionCaches();
                    ScheduleVisibleReferenceCountRefresh();

                    string message = "Item package imported.\nNew ID: " + result.NewId.ToString();
                    message += "\nAssets imported: " + result.ImportedAssetCount.ToString();
                    if (result.RemappedPathIdCount > 0)
                    {
                        message += "\nPathIDs remapped: " + result.RemappedPathIdCount.ToString();
                    }
                    if (result.MissingAssetCount > 0)
                    {
                        message += "\nWarnings: " + result.MissingAssetCount.ToString();
                    }
                    MessageBox.Show(message);
                }
                finally
                {
                    Cursor = previousCursor;
                }
            }
        }

        private string ResolveItemNameForTransferPackage(int listIndex, int elementIndex)
        {
            try
            {
                if (sessionService == null
                    || sessionService.ListCollection == null
                    || listIndex < 0
                    || listIndex >= sessionService.ListCollection.Lists.Length)
                {
                    return "Item";
                }

                eList list = sessionService.ListCollection.Lists[listIndex];
                int nameIndex = -1;
                for (int i = 0; i < list.elementFields.Length; i++)
                {
                    if (string.Equals(list.elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                    {
                        nameIndex = i;
                        break;
                    }
                }

                string name = nameIndex >= 0
                    ? listDisplayService.GetDisplayEntryName(sessionService, sessionService.ListCollection, listIndex, elementIndex, nameIndex)
                    : string.Empty;
                return string.IsNullOrWhiteSpace(name) ? "Item" : name.Trim();
            }
            catch
            {
                return "Item";
            }
        }

        private string BuildItemTransferListLabel(int listIndex)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return string.Empty;
            }

            eList list = sessionService.ListCollection.Lists[listIndex];
            return "[" + listIndex.ToString() + "] "
                + listDisplayService.GetFriendlyListName(list.listName)
                + " ("
                + list.elementValues.Length.ToString()
                + ")";
        }

        private static string BuildSafeItemTransferFileName(string value)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "item" : value.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            for (int i = 0; i < invalid.Length; i++)
            {
                safe = safe.Replace(invalid[i], '_');
            }

            while (safe.Contains("  "))
            {
                safe = safe.Replace("  ", " ");
            }

            if (safe.Length > 120)
            {
                safe = safe.Substring(0, 120).Trim();
            }

            return string.IsNullOrWhiteSpace(safe) ? "item" : safe;
        }
    }
}





using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void change_list(object sender, EventArgs ea)
		{
            bool previousSuppressSelectionHistory = suppressSelectionHistory;
            pendingAutoSelectionHistoryListIndex = -1;
            if (!previousSuppressSelectionHistory)
            {
                suppressSelectionHistory = true;
            }

            try
            {
                mainWindowSelectionCoordinatorService.HandleChangeList(
                    mainWindowSelectionUiService,
                    viewModel.EnableSelectionList,
                    comboBox_lists,
                    sessionService,
                    UpdateEquipmentTabsVisibility,
                    listSelectionCommandService,
                    listSelectionRequestBuilderService,
                    listSelectionWorkflowService,
                    listSelectionUiService,
                    listDisplayService,
                    listRowBuilderService,
                    (listIndex, entryIndex, nameFieldIndex) =>
                        listDisplayService.ComposeListDisplayName(
                            sessionService,
                            sessionService.ListCollection,
                            listIndex,
                            entryIndex,
                            nameFieldIndex,
                            IsElementMarkedDirty(listIndex, entryIndex)),
                    dataGridView_elems,
                    dataGridView_item,
                    textBox_offset,
                    xrefItemToolStripMenuItem,
                    (listIndex, entryIndex, row) => itemQualityRowStyleService.ApplyQualityStyle(
                        sessionService.ListCollection,
                        listIndex,
                        entryIndex,
                        row,
                        dataGridView_elems,
                        index => fieldIndexLookupService.GetItemQualityFieldIndex(sessionService.ListCollection, index),
                        quality => itemQualityColorService.GetQualityColor(quality),
                        (color, factor) => colorShadeService.Darken(color, factor)),
                    UpdateDescriptionTabForSelection,
                    UpdatePickIconButtonState,
                    PersistNavigationState);
            }
            finally
            {
                suppressSelectionHistory = previousSuppressSelectionHistory;
            }

            RefreshVisibleReferenceCounts();
            UpdateNpcSellServiceUiForSelection();
            if (!previousSuppressSelectionHistory)
            {
                pendingAutoSelectionHistoryListIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            }
		}


        private void change_item(object sender, EventArgs ea)
		{
            mainWindowSelectionCoordinatorService.HandleChangeItem(
                mainWindowSelectionUiService,
                viewModel.EnableSelectionItem,
                comboBox_lists,
                dataGridView_elems,
                dataGridView_item,
                gridActiveRowService,
                gridSelectionService,
                (listIndex, gridRowIndex) => elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    gridRowIndex,
                    dataGridView_elems),
                itemSelectionContextService,
                itemSelectionRequestBuilderService,
                sessionService,
                ShouldIncludeFieldInValuesTab,
                (listIndex, entryIndex, nameFieldIndex) =>
                    listDisplayService.GetDisplayEntryName(sessionService, sessionService.ListCollection, listIndex, entryIndex, nameFieldIndex),
                () => addonTypeHintService.LoadHints(Application.StartupPath, AssetManager.GameRootPath),
                itemFieldClassifierService.IsModelFieldName,
                (listIndex, rowIndex, fieldIndex) => mainWindowDirtyTrackingService.IsFieldInvalid(
                    dirtyStateTracker,
                    listIndex,
                    rowIndex,
                    fieldIndex),
                (listIndex, rowIndex, fieldIndex) => mainWindowDirtyTrackingService.IsFieldDirty(
                    dirtyStateTracker,
                    listIndex,
                    rowIndex,
                    fieldIndex),
                itemSelectionCommandService,
                itemSelectionWorkflowService,
                itemSelectionUiService,
                null,
                UpdateDescriptionTabForSelection,
                UpdatePickIconButtonState,
                PersistNavigationState,
                value => viewModel.SuppressValuesUiRefresh = value);

            UpdateRawValueEditorFromCurrentCell();
            RefreshLiveModelPreviewFromCurrentRow(true);
            bool skipAutoSelectionHistory =
                pendingAutoSelectionHistoryListIndex >= 0
                && comboBox_lists != null
                && comboBox_lists.SelectedIndex == pendingAutoSelectionHistoryListIndex;
            if (skipAutoSelectionHistory)
            {
                pendingAutoSelectionHistoryListIndex = -1;
            }
            else
            {
                RecordSelectionHistory();
            }
            UpdateNpcSellServiceUiForSelection();
		}


        private void change_offset(object sender, EventArgs e)
		{
            if (mainWindowSelectionCoordinatorService.HandleChangeOffset(
                mainWindowSelectionUiService,
                offsetChangeUiService,
                offsetUpdateService,
                sessionService,
                comboBox_lists,
                textBox_offset.Text))
            {
                viewModel.HasUnsavedChanges = true;
            }
		}


        private void change_value(object sender, DataGridViewCellEventArgs ea)
		{
            if (viewModel == null || !viewModel.EnableSelectionItem || sessionService.ListCollection == null || sessionService.Database == null)
            {
                return;
            }

            ValueEditUndoCandidate undoCandidate = CaptureValueEditUndoCandidate(ea);

            int currentListIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int currentElementIndex = ResolveCurrentElementIndex();
            string editedFieldName = (ea != null && ea.RowIndex >= 0 && ea.RowIndex < dataGridView_item.Rows.Count)
                ? ValueGridFieldNameService.GetFieldName(dataGridView_item, ea.RowIndex)
                : string.Empty;
            bool shouldInvalidateReferenceState = ShouldInvalidateReferenceState(currentListIndex, currentElementIndex, editedFieldName);

            mainWindowSelectionCoordinatorService.HandleChangeValue(
                mainWindowSelectionUiService,
                valueChangeCommandService,
                sessionService,
                comboBox_lists,
                dataGridView_item,
                dataGridView_elems,
                ea,
                gridActiveRowService,
                gridSelectionService,
                valueChangeSelectionService,
                valueChangeRequestBuilderService,
                valueChangeService,
                valueCompatibilityService,
                listDisplayService,
                (listIndex, entryIndex, nameFieldIndex) =>
                    listDisplayService.ComposeListDisplayName(
                        sessionService,
                        sessionService.ListCollection,
                        listIndex,
                        entryIndex,
                        nameFieldIndex,
                        IsElementMarkedDirty(listIndex, entryIndex)),
                (listIndex, gridRowIndex) => elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    gridRowIndex,
                    dataGridView_elems),
                rowIndex => valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, rowIndex),
                itemFieldClassifierService.IsModelFieldName,
                RemapDescriptionIdIfNeeded,
                listDisplayService.ResetList0DisplayCache,
                (listIndex, rowIndex) => mainWindowDirtyTrackingService.MarkRowDirty(
                    dirtyStateTracker,
                    listDisplayService,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex),
                (listIndex, rowIndex, fieldIndex) => mainWindowDirtyTrackingService.MarkFieldDirty(
                    dirtyStateTracker,
                    ref viewModel.HasUnsavedChanges,
                    listIndex,
                    rowIndex,
                    fieldIndex),
                (listIndex, rowIndex, fieldIndex) => mainWindowDirtyTrackingService.MarkFieldInvalid(
                    dirtyStateTracker,
                    listIndex,
                    rowIndex,
                    fieldIndex),
                (listIndex, rowIndex, fieldIndex) => mainWindowDirtyTrackingService.ClearFieldInvalid(
                    dirtyStateTracker,
                    listIndex,
                    rowIndex,
                    fieldIndex),
                valueChangeResultUiService,
                (listIndex, entryIndex, row) => itemQualityRowStyleService.ApplyQualityStyle(
                    sessionService.ListCollection,
                    listIndex,
                    entryIndex,
                    row,
                    dataGridView_elems,
                    index => fieldIndexLookupService.GetItemQualityFieldIndex(sessionService.ListCollection, index),
                    quality => itemQualityColorService.GetQualityColor(quality),
                    (color, factor) => colorShadeService.Darken(color, factor)),
                () => viewModel.HasUnsavedChanges = true,
                message => MessageBox.Show(message),
                LogError,
                viewModel);

            RefreshLiveModelPreviewFromCurrentRow(false);
            UpdateRawValueEditorFromCurrentCell();
            if (string.Equals(editedFieldName, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "ID", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "name", StringComparison.OrdinalIgnoreCase))
            {
                searchSuggestionService.ClearCache();
            }
            if (string.Equals(editedFieldName, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "ID", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(editedFieldName, "item_quality", StringComparison.OrdinalIgnoreCase))
            {
                InvalidateItemReferenceOptionCaches();
            }
            if (shouldInvalidateReferenceState)
            {
                if (string.Equals(editedFieldName, "id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(editedFieldName, "ID", StringComparison.OrdinalIgnoreCase))
                {
                    InvalidateReferenceIndexAndDisplays();
                }
                else
                {
                    UpdateReferenceIndexForEditedElement(currentListIndex, currentElementIndex);
                }
            }
            UpdateNpcSellServiceUiForSelection();
            CommitValueEditUndoCandidate(undoCandidate);
        }
    }
}





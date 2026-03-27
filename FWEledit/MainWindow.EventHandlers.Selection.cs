using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void change_list(object sender, EventArgs ea)
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
                        mainWindowDirtyTrackingService.IsRowDirty(dirtyStateTracker, listIndex, entryIndex)),
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
            if (viewModel == null || sessionService.ListCollection == null || sessionService.Database == null)
            {
                return;
            }

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
                        mainWindowDirtyTrackingService.IsRowDirty(dirtyStateTracker, listIndex, entryIndex)),
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
        }
    }
}





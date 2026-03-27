using System;
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
            mainWindowActionsCoordinatorService.HandleSetValue(
                setValueActionService,
                message => MessageBox.Show(message));
		}
    }
}





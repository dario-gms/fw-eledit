using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementBatchAddCommandService
    {
        public bool AddMultiple(
            eListCollection listCollection,
            int listIndex,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            ComboBox listComboBox,
            DataGridView elementGrid,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            ref bool hasUnsavedChanges,
            System.Action<int> markRowDirty,
            System.Action refreshListAction,
            System.Action refreshItemAction,
            System.Func<int, string> buildListLabel)
        {
            if (listCollection == null || importExportUiService == null || workflowService == null)
            {
                return false;
            }

            if (elementGrid == null || elementGrid.RowCount < 1)
            {
                return false;
            }

            if (!importExportUiService.ValidateNotConversationList(listCollection, listIndex))
            {
                return false;
            }

            enableSelectionList = false;
            enableSelectionItem = false;

            ElementBatchAddResult result = importExportUiService.ImportMultipleItems(workflowService, listCollection, listIndex, progressBar);
            if (!result.Success)
            {
                enableSelectionList = true;
                enableSelectionItem = true;
                return false;
            }

            if (result.NewIndices != null && markRowDirty != null)
            {
                for (int i = 0; i < result.NewIndices.Length; i++)
                {
                    markRowDirty(result.NewIndices[i]);
                }
            }
            hasUnsavedChanges = true;

            if (refreshListAction != null)
            {
                refreshListAction();
            }

            if (listComboBox != null && buildListLabel != null && listIndex >= 0 && listIndex < listComboBox.Items.Count)
            {
                listComboBox.Items[listIndex] = buildListLabel(listIndex);
            }

            if (progressBar != null)
            {
                progressBar.Value = 0;
            }

            if (elementGrid != null)
            {
                elementGrid.ClearSelection();
            }

            enableSelectionList = true;
            enableSelectionItem = true;

            if (elementGrid != null && elementGrid.RowCount > 0)
            {
                int lastRow = elementGrid.RowCount - 1;
                elementGrid.Rows[lastRow].Selected = true;
                elementGrid.FirstDisplayedScrollingRowIndex = lastRow;
            }

            if (refreshItemAction != null)
            {
                refreshItemAction();
            }

            return true;
        }

        public bool AddMultiple(
            eListCollection listCollection,
            int listIndex,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            ComboBox listComboBox,
            DataGridView elementGrid,
            MainWindowViewModel viewModel,
            System.Action<int> markRowDirty,
            System.Action refreshListAction,
            System.Action refreshItemAction,
            System.Func<int, string> buildListLabel)
        {
            if (listCollection == null || importExportUiService == null || workflowService == null)
            {
                return false;
            }

            if (elementGrid == null || elementGrid.RowCount < 1)
            {
                return false;
            }

            if (!importExportUiService.ValidateNotConversationList(listCollection, listIndex))
            {
                return false;
            }

            if (viewModel != null)
            {
                viewModel.EnableSelectionList = false;
                viewModel.EnableSelectionItem = false;
            }

            ElementBatchAddResult result = importExportUiService.ImportMultipleItems(workflowService, listCollection, listIndex, progressBar);
            if (!result.Success)
            {
                if (viewModel != null)
                {
                    viewModel.EnableSelectionList = true;
                    viewModel.EnableSelectionItem = true;
                }
                return false;
            }

            if (result.NewIndices != null && markRowDirty != null)
            {
                for (int i = 0; i < result.NewIndices.Length; i++)
                {
                    markRowDirty(result.NewIndices[i]);
                }
            }

            if (viewModel != null)
            {
                viewModel.HasUnsavedChanges = true;
            }

            if (refreshListAction != null)
            {
                refreshListAction();
            }

            if (listComboBox != null && buildListLabel != null && listIndex >= 0 && listIndex < listComboBox.Items.Count)
            {
                listComboBox.Items[listIndex] = buildListLabel(listIndex);
            }

            if (progressBar != null)
            {
                progressBar.Value = 0;
            }

            if (elementGrid != null)
            {
                elementGrid.ClearSelection();
            }

            if (viewModel != null)
            {
                viewModel.EnableSelectionList = true;
                viewModel.EnableSelectionItem = true;
            }

            if (elementGrid != null && elementGrid.RowCount > 0)
            {
                int lastRow = elementGrid.RowCount - 1;
                elementGrid.Rows[lastRow].Selected = true;
                elementGrid.FirstDisplayedScrollingRowIndex = lastRow;
            }

            if (refreshItemAction != null)
            {
                refreshItemAction();
            }

            return true;
        }
    }
}

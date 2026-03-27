using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementImportUiService
    {
        public bool ImportSingleAndSelect(
            ElementImportCommandService importCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            Action<int> markRowDirty,
            Action reloadList,
            Action<int> selectRow)
        {
            if (importCommandService == null || elementGrid == null)
            {
                return false;
            }

            int currentRow = elementGrid.CurrentRow != null ? elementGrid.CurrentRow.Index : -1;
            bool imported = importCommandService.ImportSingle(
                listCollection,
                listIndex,
                currentRow,
                importExportUiService,
                workflowService,
                markRowDirty,
                reloadList,
                rowIndex =>
                {
                    if (selectRow != null)
                    {
                        selectRow(rowIndex);
                    }
                });

            return imported;
        }

        public bool ImportSingleAndSelect(
            ElementImportCommandService importCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            Action<int> markRowDirty,
            Action reloadList,
            Action<int> selectRow,
            MainWindowViewModel viewModel)
        {
            bool imported = ImportSingleAndSelect(
                importCommandService,
                listCollection,
                listIndex,
                elementGrid,
                importExportUiService,
                workflowService,
                markRowDirty,
                reloadList,
                selectRow);

            if (imported && viewModel != null)
            {
                viewModel.HasUnsavedChanges = true;
            }

            return imported;
        }

        public void AddMultiple(
            ElementBatchAddCommandService batchAddCommandService,
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
            Action<int> markRowDirty,
            Action reloadList,
            Action reloadItem,
            Func<int, string> buildListDisplayName)
        {
            if (batchAddCommandService == null)
            {
                return;
            }

            batchAddCommandService.AddMultiple(
                listCollection,
                listIndex,
                importExportUiService,
                workflowService,
                progressBar,
                listComboBox,
                elementGrid,
                ref enableSelectionList,
                ref enableSelectionItem,
                ref hasUnsavedChanges,
                markRowDirty,
                reloadList,
                reloadItem,
                buildListDisplayName);
        }

        public void AddMultiple(
            ElementBatchAddCommandService batchAddCommandService,
            eListCollection listCollection,
            int listIndex,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            ComboBox listComboBox,
            DataGridView elementGrid,
            MainWindowViewModel viewModel,
            Action<int> markRowDirty,
            Action reloadList,
            Action reloadItem,
            Func<int, string> buildListDisplayName)
        {
            if (batchAddCommandService == null)
            {
                return;
            }

            batchAddCommandService.AddMultiple(
                listCollection,
                listIndex,
                importExportUiService,
                workflowService,
                progressBar,
                listComboBox,
                elementGrid,
                viewModel,
                markRowDirty,
                reloadList,
                reloadItem,
                buildListDisplayName);
        }
    }
}

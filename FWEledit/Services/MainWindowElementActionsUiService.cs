using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowElementActionsUiService
    {
        public void DeleteSelected(
            ElementDeleteCommandService commandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementDeletionUiService deletionUiService,
            ComboBox listCombo,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            ref bool hasUnsavedChanges,
            Action refreshItem)
        {
            if (commandService == null)
            {
                return;
            }

            commandService.DeleteSelected(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                deletionUiService,
                listCombo,
                ref enableSelectionList,
                ref enableSelectionItem,
                ref hasUnsavedChanges,
                refreshItem);
        }

        public void DeleteSelected(
            ElementDeleteCommandService commandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementDeletionUiService deletionUiService,
            ComboBox listCombo,
            MainWindowViewModel viewModel,
            Action refreshItem)
        {
            if (commandService == null)
            {
                return;
            }

            commandService.DeleteSelected(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                deletionUiService,
                listCombo,
                viewModel,
                refreshItem);
        }

        public void CloneSelected(
            ElementCloneCommandService commandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementCloneUiService cloneUiService,
            ComboBox listCombo,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            Action<int> markRowDirty,
            Action refreshList,
            Action refreshItem,
            Func<int, string> getFriendlyListName)
        {
            if (commandService == null)
            {
                return;
            }

            commandService.CloneSelected(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                cloneUiService,
                listCombo,
                ref enableSelectionList,
                ref enableSelectionItem,
                markRowDirty,
                refreshList,
                refreshItem,
                getFriendlyListName);
        }

        public void CloneSelected(
            ElementCloneCommandService commandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementCloneUiService cloneUiService,
            ComboBox listCombo,
            MainWindowViewModel viewModel,
            Action<int> markRowDirty,
            Action refreshList,
            Action refreshItem,
            Func<int, string> getFriendlyListName)
        {
            if (commandService == null)
            {
                return;
            }

            commandService.CloneSelected(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                cloneUiService,
                listCombo,
                viewModel,
                markRowDirty,
                refreshList,
                refreshItem,
                getFriendlyListName);
        }

        public void ExportSelected(
            ElementExportUiService exportUiService,
            ElementExportCommandService exportCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (exportUiService == null)
            {
                return;
            }

            exportUiService.ExportSelected(
                exportCommandService,
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                importExportUiService,
                workflowService,
                progressBar);
        }

        public void ImportSingleAndSelect(
            ElementImportUiService importUiService,
            ElementImportCommandService importCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            Action<int> markRowDirty,
            Action refreshList,
            Action<int> selectRow,
            ref bool hasUnsavedChanges)
        {
            if (importUiService == null)
            {
                return;
            }

            bool imported = importUiService.ImportSingleAndSelect(
                importCommandService,
                listCollection,
                listIndex,
                elementGrid,
                importExportUiService,
                workflowService,
                markRowDirty,
                refreshList,
                selectRow);

            if (imported)
            {
                hasUnsavedChanges = true;
            }
        }

        public void ImportSingleAndSelect(
            ElementImportUiService importUiService,
            ElementImportCommandService importCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            Action<int> markRowDirty,
            Action refreshList,
            Action<int> selectRow,
            MainWindowViewModel viewModel)
        {
            if (importUiService == null)
            {
                return;
            }

            importUiService.ImportSingleAndSelect(
                importCommandService,
                listCollection,
                listIndex,
                elementGrid,
                importExportUiService,
                workflowService,
                markRowDirty,
                refreshList,
                selectRow,
                viewModel);
        }

        public void AddMultiple(
            ElementImportUiService importUiService,
            ElementBatchAddCommandService batchAddCommandService,
            eListCollection listCollection,
            int listIndex,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            ComboBox listCombo,
            DataGridView elementGrid,
            ref bool enableSelectionList,
            ref bool enableSelectionItem,
            ref bool hasUnsavedChanges,
            Action<int> markRowDirty,
            Action refreshList,
            Action refreshItem,
            Func<int, string> getListDisplayName)
        {
            if (importUiService == null)
            {
                return;
            }

            importUiService.AddMultiple(
                batchAddCommandService,
                listCollection,
                listIndex,
                importExportUiService,
                workflowService,
                progressBar,
                listCombo,
                elementGrid,
                ref enableSelectionList,
                ref enableSelectionItem,
                ref hasUnsavedChanges,
                markRowDirty,
                refreshList,
                refreshItem,
                getListDisplayName);
        }

        public void AddMultiple(
            ElementImportUiService importUiService,
            ElementBatchAddCommandService batchAddCommandService,
            eListCollection listCollection,
            int listIndex,
            ElementImportExportUiService importExportUiService,
            ElementImportExportWorkflowService workflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            ComboBox listCombo,
            DataGridView elementGrid,
            MainWindowViewModel viewModel,
            Action<int> markRowDirty,
            Action refreshList,
            Action refreshItem,
            Func<int, string> getListDisplayName)
        {
            if (importUiService == null)
            {
                return;
            }

            importUiService.AddMultiple(
                batchAddCommandService,
                listCollection,
                listIndex,
                importExportUiService,
                workflowService,
                progressBar,
                listCombo,
                elementGrid,
                viewModel,
                markRowDirty,
                refreshList,
                refreshItem,
                getListDisplayName);
        }

        public void MoveToTop(
            ElementMoveCommandService moveCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            ref bool enableSelectionItem,
            Action refreshList)
        {
            if (moveCommandService == null)
            {
                return;
            }

            moveCommandService.MoveToTop(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                moveUiService,
                ref enableSelectionItem,
                refreshList);
        }

        public void MoveToTop(
            ElementMoveCommandService moveCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            MainWindowViewModel viewModel,
            Action refreshList)
        {
            if (moveCommandService == null)
            {
                return;
            }

            moveCommandService.MoveToTop(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                moveUiService,
                viewModel,
                refreshList);
        }

        public void MoveToEnd(
            ElementMoveCommandService moveCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            ref bool enableSelectionItem,
            Action refreshList)
        {
            if (moveCommandService == null)
            {
                return;
            }

            moveCommandService.MoveToEnd(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                moveUiService,
                ref enableSelectionItem,
                refreshList);
        }

        public void MoveToEnd(
            ElementMoveCommandService moveCommandService,
            eListCollection listCollection,
            int listIndex,
            DataGridView elementGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService mutationService,
            ElementMoveUiService moveUiService,
            MainWindowViewModel viewModel,
            Action refreshList)
        {
            if (moveCommandService == null)
            {
                return;
            }

            moveCommandService.MoveToEnd(
                listCollection,
                listIndex,
                elementGrid,
                gridSelectionService,
                mutationService,
                moveUiService,
                viewModel,
                refreshList);
        }
    }
}

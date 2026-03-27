using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowActionsCoordinatorService
    {
        public void HandleDeleteSelected(
            MainWindowElementActionsUiService actionsUiService,
            ElementDeleteCommandService deleteCommandService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService elementListMutationService,
            ElementDeletionUiService elementDeletionUiService,
            ComboBox listCombo,
            MainWindowViewModel viewModel,
            Action refreshItem)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.DeleteSelected(
                deleteCommandService,
                sessionService.ListCollection,
                listIndex,
                elementsGrid,
                gridSelectionService,
                elementListMutationService,
                elementDeletionUiService,
                listCombo,
                viewModel,
                refreshItem);
        }

        public void HandleCloneSelected(
            MainWindowElementActionsUiService actionsUiService,
            ElementCloneCommandService cloneCommandService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService elementListMutationService,
            ElementCloneUiService elementCloneUiService,
            ComboBox listCombo,
            MainWindowViewModel viewModel,
            Action<int> markRowDirty,
            Action refreshList,
            Action refreshItem,
            Func<int, string> getFriendlyListName)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.CloneSelected(
                cloneCommandService,
                sessionService.ListCollection,
                listIndex,
                elementsGrid,
                gridSelectionService,
                elementListMutationService,
                elementCloneUiService,
                listCombo,
                viewModel,
                markRowDirty,
                refreshList,
                refreshItem,
                getFriendlyListName);
        }

        public void HandleExportSelected(
            MainWindowElementActionsUiService actionsUiService,
            ElementExportUiService elementExportUiService,
            ElementExportCommandService exportCommandService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            GridSelectionService gridSelectionService,
            ElementImportExportUiService elementImportExportUiService,
            ElementImportExportWorkflowService elementImportExportWorkflowService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.ExportSelected(
                elementExportUiService,
                exportCommandService,
                sessionService.ListCollection,
                listIndex,
                elementsGrid,
                gridSelectionService,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                progressBar);
        }

        public void HandleImportSingle(
            MainWindowElementActionsUiService actionsUiService,
            ElementImportUiService elementImportUiService,
            ElementImportCommandService importCommandService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            ElementImportExportUiService elementImportExportUiService,
            ElementImportExportWorkflowService elementImportExportWorkflowService,
            Action<int> markRowDirty,
            Action refreshList,
            Action<int> selectRow,
            MainWindowViewModel viewModel)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.ImportSingleAndSelect(
                elementImportUiService,
                importCommandService,
                sessionService.ListCollection,
                listIndex,
                elementsGrid,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                markRowDirty,
                refreshList,
                selectRow,
                viewModel);
        }

        public void HandleAddMultiple(
            MainWindowElementActionsUiService actionsUiService,
            ElementImportUiService elementImportUiService,
            ElementBatchAddCommandService batchAddCommandService,
            ISessionService sessionService,
            int listIndex,
            ElementImportExportUiService elementImportExportUiService,
            ElementImportExportWorkflowService elementImportExportWorkflowService,
            ColorProgressBar.ColorProgressBar progressBar,
            ComboBox listCombo,
            DataGridView elementsGrid,
            MainWindowViewModel viewModel,
            Action<int> markRowDirty,
            Action refreshList,
            Action refreshItem,
            Func<int, string> describeList)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.AddMultiple(
                elementImportUiService,
                batchAddCommandService,
                sessionService.ListCollection,
                listIndex,
                elementImportExportUiService,
                elementImportExportWorkflowService,
                progressBar,
                listCombo,
                elementsGrid,
                viewModel,
                markRowDirty,
                refreshList,
                refreshItem,
                describeList);
        }

        public void HandleMoveToTop(
            MainWindowElementActionsUiService actionsUiService,
            ElementMoveCommandService moveCommandService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService elementListMutationService,
            ElementMoveUiService elementMoveUiService,
            MainWindowViewModel viewModel,
            Action refreshList)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.MoveToTop(
                moveCommandService,
                sessionService.ListCollection,
                listIndex,
                elementsGrid,
                gridSelectionService,
                elementListMutationService,
                elementMoveUiService,
                viewModel,
                refreshList);
        }

        public void HandleMoveToEnd(
            MainWindowElementActionsUiService actionsUiService,
            ElementMoveCommandService moveCommandService,
            ISessionService sessionService,
            int listIndex,
            DataGridView elementsGrid,
            GridSelectionService gridSelectionService,
            ElementListMutationService elementListMutationService,
            ElementMoveUiService elementMoveUiService,
            MainWindowViewModel viewModel,
            Action refreshList)
        {
            if (actionsUiService == null || sessionService == null)
            {
                return;
            }

            actionsUiService.MoveToEnd(
                moveCommandService,
                sessionService.ListCollection,
                listIndex,
                elementsGrid,
                gridSelectionService,
                elementListMutationService,
                elementMoveUiService,
                viewModel,
                refreshList);
        }

        public void HandleSetValue(SetValueActionService setValueActionService, Action<string> showMessage)
        {
            if (setValueActionService == null)
            {
                return;
            }

            setValueActionService.ShowDisabledMessage(showMessage);
        }
    }
}

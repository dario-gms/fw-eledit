using System;

namespace FWEledit
{
    public sealed class DescriptionUiService
    {
        public void UpdateSelection(
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            object idValue,
            MainWindowViewModel viewModel,
            DescriptionWorkflowService workflowService,
            Func<int, string> itemDescResolver,
            Action<string> setEditorText,
            Action<string> renderPreview)
        {
            if (listCollection == null || viewModel == null || workflowService == null || setEditorText == null)
            {
                return;
            }
            if (listIndex < 0 || listIndex >= listCollection.Lists.Length || rowIndex < 0)
            {
                return;
            }

            DescriptionSelectionResult selection = workflowService.BuildSelection(
                listCollection,
                listIndex,
                rowIndex,
                idValue,
                itemDescResolver,
                viewModel.DescriptionViewModel);

            viewModel.IsUpdatingDescriptionUi = true;
            try
            {
                setEditorText(selection.EditorText ?? string.Empty);
                if (renderPreview != null)
                {
                    renderPreview(selection.EditorText ?? string.Empty);
                }
            }
            finally
            {
                viewModel.IsUpdatingDescriptionUi = false;
            }
        }

        public void HandleEditorChanged(
            MainWindowViewModel viewModel,
            bool hasPreview,
            string editorText,
            Action<string> renderPreview,
            Action stageChange)
        {
            if (viewModel == null || viewModel.IsUpdatingDescriptionUi || !hasPreview)
            {
                return;
            }
            if (renderPreview != null)
            {
                renderPreview(editorText ?? string.Empty);
            }
            if (stageChange != null)
            {
                stageChange();
            }
        }

        public DescriptionChangeResult StageChange(
            MainWindowViewModel viewModel,
            DescriptionWorkflowService workflowService,
            DescriptionLoadService loadService,
            DescriptionRuntimeService runtimeService,
            string editorText,
            Action<string[]> applyRuntime,
            Action markUnsaved,
            Func<int> getSelectedListIndex,
            Func<int> getSelectedRowIndex,
            Action<int, int> markRowDirty)
        {
            DescriptionChangeResult result = new DescriptionChangeResult();
            if (viewModel == null || workflowService == null)
            {
                return result;
            }
            if (viewModel.IsUpdatingDescriptionUi || viewModel.DescriptionViewModel.CurrentItemId <= 0)
            {
                return result;
            }

            result = workflowService.StageEditorText(viewModel.DescriptionViewModel, editorText);
            if (result.Changed)
            {
                if (markUnsaved != null)
                {
                    markUnsaved();
                }
                if (loadService != null && runtimeService != null && applyRuntime != null)
                {
                    loadService.SyncRuntime(viewModel.DescriptionViewModel, runtimeService, applyRuntime);
                }

                if (getSelectedListIndex != null && getSelectedRowIndex != null && markRowDirty != null)
                {
                    int listIndex = getSelectedListIndex();
                    int rowIndex = getSelectedRowIndex();
                    if (listIndex > -1 && rowIndex > -1)
                    {
                        markRowDirty(listIndex, rowIndex);
                    }
                }
            }

            return result;
        }

        public bool TrySaveCurrentDescription(MainWindowViewModel viewModel, Action stageChange, Action<string> showMessage)
        {
            if (viewModel == null)
            {
                return false;
            }
            if (viewModel.DescriptionViewModel.CurrentItemId <= 0)
            {
                if (showMessage != null)
                {
                    showMessage("No item selected.");
                }
                return false;
            }

            if (stageChange != null)
            {
                stageChange();
            }
            return true;
        }
    }
}

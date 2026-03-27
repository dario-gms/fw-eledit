using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowDescriptionUiService
    {
        public void ApplyItemDescriptionRuntime(
            DescriptionRuntimeBridgeService runtimeBridgeService,
            ISessionService sessionService,
            string[] data,
            CacheSave database)
        {
            if (runtimeBridgeService == null)
            {
                return;
            }

            runtimeBridgeService.ApplyRuntime(sessionService, data, database);
        }

        public void LoadItemDescriptionsFromConfigs(
            DescriptionLoadUiService loadUiService,
            DescriptionLoadService loadService,
            MainWindowViewModel viewModel,
            ItemDescriptionFileService descriptionFileService,
            DescriptionRuntimeService runtimeService,
            string gameRootPath,
            string workspaceRootPath,
            Action<string> updateStatus,
            Action<string[]> applyRuntime)
        {
            if (loadUiService == null)
            {
                return;
            }

            loadUiService.LoadDescriptions(
                loadService,
                viewModel,
                descriptionFileService,
                runtimeService,
                gameRootPath,
                workspaceRootPath,
                updateStatus,
                applyRuntime);
        }

        public void UpdateDescriptionTabForSelection(
            eListCollection listCollection,
            ComboBox listCombo,
            DataGridView elementGrid,
            TextBox editor,
            MainWindowViewModel viewModel,
            DescriptionUiService descriptionUiService,
            DescriptionWorkflowService workflowService,
            Func<int, string> itemDescResolver,
            Action<string> renderPreview)
        {
            if (editor == null || listCollection == null)
            {
                return;
            }

            int listIndex = listCombo != null ? listCombo.SelectedIndex : -1;
            int rowIndex = elementGrid != null && elementGrid.CurrentCell != null
                ? elementGrid.CurrentCell.RowIndex
                : -1;
            object idValue = rowIndex >= 0 && elementGrid != null && rowIndex < elementGrid.Rows.Count
                ? elementGrid.Rows[rowIndex].Cells[0].Value
                : null;

            if (descriptionUiService == null)
            {
                return;
            }

            descriptionUiService.UpdateSelection(
                listCollection,
                listIndex,
                rowIndex,
                idValue,
                viewModel,
                workflowService,
                itemDescResolver,
                text => editor.Text = text,
                renderPreview);
        }

        public void HandleDescriptionChanged(
            DescriptionUiService descriptionUiService,
            MainWindowViewModel viewModel,
            TextBox editor,
            RichTextBox preview,
            Action<string> renderPreview,
            Action stageChange)
        {
            if (descriptionUiService == null)
            {
                return;
            }

            bool hasPreview = preview != null && editor != null;
            string editorText = editor != null ? editor.Text : string.Empty;

            descriptionUiService.HandleEditorChanged(
                viewModel,
                hasPreview,
                editorText,
                renderPreview,
                stageChange);
        }

        public DescriptionChangeResult StageCurrentDescriptionChange(
            DescriptionUiService descriptionUiService,
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
            if (descriptionUiService == null)
            {
                return new DescriptionChangeResult();
            }

            return descriptionUiService.StageChange(
                viewModel,
                workflowService,
                loadService,
                runtimeService,
                editorText,
                applyRuntime,
                markUnsaved,
                getSelectedListIndex,
                getSelectedRowIndex,
                markRowDirty);
        }

        public bool FlushPendingDescriptionsToDisk(
            DescriptionFlushUiService flushUiService,
            DescriptionWorkflowService workflowService,
            DescriptionLoadService loadService,
            DescriptionRuntimeService runtimeService,
            MainWindowViewModel viewModel,
            AssetManager assetManager,
            Action<string> showMessage,
            Action<string> updateStatus,
            Action<string[]> applyRuntime)
        {
            if (flushUiService == null)
            {
                return false;
            }

            return flushUiService.FlushPending(
                workflowService,
                loadService,
                runtimeService,
                viewModel,
                assetManager,
                showMessage,
                updateStatus,
                applyRuntime);
        }

        public void RenderDescriptionPreview(
            DescriptionPreviewUiService previewUiService,
            DescriptionPreviewService previewService,
            RichTextBox previewBox,
            string rawText)
        {
            if (previewUiService == null)
            {
                return;
            }

            previewUiService.RenderPreview(previewBox, previewService, rawText);
        }

        public void TrySaveCurrentDescription(
            DescriptionUiService descriptionUiService,
            MainWindowViewModel viewModel,
            Action stageChange,
            Action<string> showMessage)
        {
            if (descriptionUiService == null)
            {
                return;
            }

            descriptionUiService.TrySaveCurrentDescription(viewModel, stageChange, showMessage);
        }
    }
}

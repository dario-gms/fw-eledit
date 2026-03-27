using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowDescriptionCoordinatorService
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
            MainWindowDescriptionUiService descriptionUiService,
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
            if (descriptionUiService == null)
            {
                return;
            }

            descriptionUiService.LoadItemDescriptionsFromConfigs(
                loadUiService,
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
            MainWindowDescriptionUiService descriptionUiService,
            eListCollection listCollection,
            ComboBox listCombo,
            DataGridView elementGrid,
            TextBox editor,
            MainWindowViewModel viewModel,
            DescriptionUiService uiService,
            DescriptionWorkflowService workflowService,
            Func<int, string> itemDescResolver,
            Action<string> renderPreview)
        {
            if (descriptionUiService == null)
            {
                return;
            }

            descriptionUiService.UpdateDescriptionTabForSelection(
                listCollection,
                listCombo,
                elementGrid,
                editor,
                viewModel,
                uiService,
                workflowService,
                itemDescResolver,
                renderPreview);
        }

        public void HandleDescriptionChanged(
            MainWindowDescriptionUiService descriptionUiService,
            DescriptionUiService uiService,
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

            descriptionUiService.HandleDescriptionChanged(
                uiService,
                viewModel,
                editor,
                preview,
                renderPreview,
                stageChange);
        }

        public DescriptionChangeResult StageCurrentDescriptionChange(
            MainWindowDescriptionUiService descriptionUiService,
            DescriptionUiService uiService,
            MainWindowViewModel viewModel,
            DescriptionWorkflowService workflowService,
            DescriptionLoadService loadService,
            DescriptionRuntimeService runtimeService,
            string editorText,
            Action<string[]> applyRuntime,
            Action markUnsaved,
            Func<int> getSelectedListIndex,
            Func<int> getSelectedRowIndex,
            Action<int, int> markRowDirty,
            bool updateStatus,
            Action<string> updateStatusText)
        {
            if (descriptionUiService == null)
            {
                return new DescriptionChangeResult();
            }

            DescriptionChangeResult result = descriptionUiService.StageCurrentDescriptionChange(
                uiService,
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

            if (updateStatus && updateStatusText != null && !string.IsNullOrWhiteSpace(result.StatusText))
            {
                updateStatusText(result.StatusText);
            }

            return result;
        }

        public bool FlushPendingDescriptionsToDisk(
            MainWindowDescriptionUiService descriptionUiService,
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
            if (descriptionUiService == null)
            {
                return false;
            }

            return descriptionUiService.FlushPendingDescriptionsToDisk(
                flushUiService,
                workflowService,
                loadService,
                runtimeService,
                viewModel,
                assetManager,
                showMessage,
                updateStatus,
                applyRuntime);
        }

        public void RemapDescriptionIdIfNeeded(
            DescriptionIdRemapService remapService,
            DescriptionViewModel descriptionViewModel,
            DescriptionLoadService loadService,
            DescriptionRuntimeService runtimeService,
            int oldId,
            int newId,
            Action<string[]> applyRuntime,
            Action markUnsaved)
        {
            if (remapService == null)
            {
                return;
            }

            remapService.TryRemap(
                descriptionViewModel,
                loadService,
                runtimeService,
                oldId,
                newId,
                applyRuntime,
                markUnsaved);
        }

        public void RenderDescriptionPreview(
            MainWindowDescriptionUiService descriptionUiService,
            DescriptionPreviewUiService previewUiService,
            DescriptionPreviewService previewService,
            RichTextBox previewBox,
            string rawText)
        {
            if (descriptionUiService == null)
            {
                return;
            }

            descriptionUiService.RenderDescriptionPreview(
                previewUiService,
                previewService,
                previewBox,
                rawText);
        }

        public void TrySaveCurrentDescription(
            MainWindowDescriptionUiService descriptionUiService,
            DescriptionUiService uiService,
            MainWindowViewModel viewModel,
            Action stageChange,
            Action<string> showMessage)
        {
            if (descriptionUiService == null)
            {
                return;
            }

            descriptionUiService.TrySaveCurrentDescription(
                uiService,
                viewModel,
                stageChange,
                showMessage);
        }
    }
}

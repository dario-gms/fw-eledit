using System;

namespace FWEledit
{
    public sealed class DescriptionFlushUiService
    {
        public bool FlushPending(
            DescriptionWorkflowService workflowService,
            DescriptionLoadService loadService,
            DescriptionRuntimeService runtimeService,
            MainWindowViewModel viewModel,
            AssetManager assetManager,
            Action<string> showMessage,
            Action<string> updateStatus,
            Action<string[]> applyRuntime)
        {
            if (workflowService == null || viewModel == null)
            {
                return false;
            }

            DescriptionFlushResult result = workflowService.FlushPendingDescriptions(viewModel.DescriptionViewModel, assetManager);
            if (!result.Success)
            {
                if (showMessage != null)
                {
                    showMessage(string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "item_ext_desc.txt was not found in configs.pck.files."
                        : result.ErrorMessage);
                }
                return false;
            }

            if (result.HadPendingChanges)
            {
                if (loadService != null && runtimeService != null && applyRuntime != null)
                {
                    loadService.SyncRuntime(viewModel.DescriptionViewModel, runtimeService, applyRuntime);
                }
                if (updateStatus != null && !string.IsNullOrWhiteSpace(result.StatusText))
                {
                    updateStatus(result.StatusText);
                }
            }

            return true;
        }
    }
}

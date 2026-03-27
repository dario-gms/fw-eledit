using System;

namespace FWEledit
{
    public sealed class DescriptionLoadUiService
    {
        public void LoadDescriptions(
            DescriptionLoadService loadService,
            MainWindowViewModel viewModel,
            ItemDescriptionFileService descriptionFileService,
            DescriptionRuntimeService runtimeService,
            string gameRootPath,
            string workspaceRootPath,
            Action<string> updateStatus,
            Action<string[]> applyRuntime)
        {
            if (loadService == null || viewModel == null)
            {
                return;
            }

            loadService.LoadFromConfigs(
                viewModel.DescriptionViewModel,
                descriptionFileService,
                runtimeService,
                gameRootPath,
                workspaceRootPath,
                updateStatus,
                applyRuntime);
        }
    }
}

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
            LoadDescriptions(
                loadService,
                viewModel,
                descriptionFileService,
                runtimeService,
                gameRootPath,
                workspaceRootPath,
                string.Empty,
                updateStatus,
                applyRuntime);
        }

        public void LoadDescriptions(
            DescriptionLoadService loadService,
            MainWindowViewModel viewModel,
            ItemDescriptionFileService descriptionFileService,
            DescriptionRuntimeService runtimeService,
            string gameRootPath,
            string workspaceRootPath,
            string resolvedFilePath,
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
                resolvedFilePath,
                updateStatus,
                applyRuntime);
        }
    }
}

using System;

namespace FWEledit
{
    public sealed class DescriptionLoadService
    {
        public void LoadFromConfigs(
            DescriptionViewModel viewModel,
            ItemDescriptionFileService fileService,
            DescriptionRuntimeService runtimeService,
            string gameRootPath,
            string workspaceRootPath,
            Action<string> updateStatus,
            Action<string[]> applyRuntime)
        {
            if (viewModel == null || fileService == null || runtimeService == null)
            {
                return;
            }

            LoadFromConfigs(
                viewModel,
                fileService,
                runtimeService,
                gameRootPath,
                workspaceRootPath,
                string.Empty,
                updateStatus,
                applyRuntime);
        }

        public void LoadFromConfigs(
            DescriptionViewModel viewModel,
            ItemDescriptionFileService fileService,
            DescriptionRuntimeService runtimeService,
            string gameRootPath,
            string workspaceRootPath,
            string resolvedFilePath,
            Action<string> updateStatus,
            Action<string[]> applyRuntime)
        {
            if (viewModel == null || fileService == null || runtimeService == null)
            {
                return;
            }

            string filePath = resolvedFilePath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                filePath = fileService.ResolveItemExtDescFilePath(gameRootPath, workspaceRootPath);
            }
            viewModel.LoadFromFile(filePath);

            if (updateStatus != null && !string.IsNullOrWhiteSpace(viewModel.StatusText))
            {
                updateStatus(viewModel.StatusText);
            }

            SyncRuntime(viewModel, runtimeService, applyRuntime);
        }

        public void SyncRuntime(DescriptionViewModel viewModel, DescriptionRuntimeService runtimeService, Action<string[]> applyRuntime)
        {
            if (viewModel == null || runtimeService == null || applyRuntime == null)
            {
                return;
            }

            string[] data = runtimeService.BuildRuntimeArray(viewModel);
            applyRuntime(data);
        }
    }
}

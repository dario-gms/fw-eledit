using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigFileSaveUiService
    {
        public bool SaveConfig(
            IDialogService dialogService,
            ConfigWindowViewModel viewModel,
            ConfigData data,
            string initialDirectory,
            IWin32Window owner)
        {
            if (dialogService == null || viewModel == null || data == null)
            {
                return false;
            }

            string filePath = dialogService.ShowSaveFile(
                "Save Configuration File",
                "EL Configuration File (*.cfg)|*.cfg|All Files (*.*)|*.*",
                initialDirectory,
                data.LoadedFileName,
                owner);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return false;
            }

            viewModel.SaveConfig(filePath);
            return true;
        }
    }
}

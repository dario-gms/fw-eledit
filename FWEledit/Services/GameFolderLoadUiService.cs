using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GameFolderLoadUiService
    {
        public void LoadFromDialog(
            GameFolderDialogService dialogService,
            NavigationStateService navigationStateService,
            Action<string> loadFolder,
            Action<string> showMessage)
        {
            LoadFromDialog(dialogService, navigationStateService, loadFolder, showMessage, null);
        }

        public void LoadFromDialog(
            GameFolderDialogService dialogService,
            NavigationStateService navigationStateService,
            Action<string> loadFolder,
            Action<string> showMessage,
            IWin32Window owner)
        {
            if (dialogService == null || loadFolder == null)
            {
                return;
            }

            string savedFolder = navigationStateService != null ? navigationStateService.GetLastGameFolder() : string.Empty;
            string selectedFolder = dialogService.PromptForGameFolder(
                "Select the Forsaken World game folder",
                savedFolder,
                owner);
            if (!string.IsNullOrWhiteSpace(selectedFolder))
            {
                loadFolder(selectedFolder);
            }
        }

        public void LoadLastFolder(
            GameFolderDialogService dialogService,
            NavigationStateService navigationStateService,
            Action<string> loadFolder,
            Action<string> showMessage)
        {
            if (dialogService == null || loadFolder == null || navigationStateService == null)
            {
                return;
            }

            string savedFolder = dialogService.ResolveExistingFolder(navigationStateService.GetLastGameFolder());
            if (string.IsNullOrWhiteSpace(savedFolder))
            {
                if (showMessage != null)
                {
                    showMessage("No saved game folder found. Use File > Load... first.");
                }
                return;
            }
            loadFolder(savedFolder);
        }
    }
}

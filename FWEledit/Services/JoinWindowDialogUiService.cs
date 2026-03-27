using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class JoinWindowDialogUiService
    {
        public void BrowseElementFile(IDialogService dialogService, TextBox elementFile, string initialDirectory, IWin32Window owner)
        {
            if (dialogService == null || elementFile == null)
            {
                return;
            }

            string path = dialogService.ShowOpenFile(
                "Elements File",
                "Elements File (*.data)|*.data|All Files (*.*)|*.*",
                initialDirectory,
                owner);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            elementFile.Text = path;
        }

        public void BrowseLogDirectory(GameFolderDialogService folderDialogService, TextBox logDir)
        {
            if (folderDialogService == null || logDir == null)
            {
                return;
            }

            string path = folderDialogService.PromptForGameFolder("Select Log Directory", logDir.Text);
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return;
            }

            logDir.Text = path;
        }
    }
}

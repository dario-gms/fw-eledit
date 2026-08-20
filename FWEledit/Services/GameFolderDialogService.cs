using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GameFolderDialogService
    {
        public string PromptForGameFolder(string description, string initialPath)
        {
            return PromptForGameFolder(description, initialPath, null);
        }

        public string PromptForGameFolder(string description, string initialPath, IWin32Window owner)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = description ?? "Select folder";
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = false;
                dialog.FileName = "Select this folder";
                dialog.Filter = "Folders|*.folder";
                dialog.AutoUpgradeEnabled = true;

                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.InitialDirectory = initialPath;
                }

                DialogResult result = owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return string.Empty;
                }

                if (Directory.Exists(dialog.FileName))
                {
                    return dialog.FileName;
                }

                string selectedFolder = Path.GetDirectoryName(dialog.FileName);
                return !string.IsNullOrWhiteSpace(selectedFolder) && Directory.Exists(selectedFolder)
                    ? selectedFolder
                    : string.Empty;
            }
        }

        public string ResolveExistingFolder(string path)
        {
            return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)
                ? path
                : string.Empty;
        }
    }
}

using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GameFolderDialogService
    {
        public string PromptForGameFolder(string description, string initialPath)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = description ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.SelectedPath = initialPath;
                }

                return dialog.ShowDialog() == DialogResult.OK && Directory.Exists(dialog.SelectedPath)
                    ? dialog.SelectedPath
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

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void click_clearCache(object sender, EventArgs e)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string editorRoot = Path.Combine(localAppData, "FWEledit");
            string workspaceRoot = Path.Combine(editorRoot, "workspace");
            string pckIndexCacheRoot = Path.Combine(editorRoot, "pck-index-cache");
            string clientMapsRoot = Path.Combine(editorRoot, "client-maps");

            string message =
                "Clear FWEledit cache and restart the application?\n\n" +
                "This deletes only editor-generated cache folders:\n" +
                "- workspace copies and extracted/materialized PCK files\n" +
                "- cached path.data/resources used by the editor\n" +
                "- PCK index cache\n" +
                "- persistent client resource maps\n\n" +
                "It does not delete your game client, resources, backups, or project files.\n\n" +
                "Continue?";

            DialogResult confirm = MessageBox.Show(
                this,
                message,
                "Clear Cache",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            Cursor previousCursor = Cursor.Current;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                string error;
                if (!TryClearEditorCache(workspaceRoot, pckIndexCacheRoot, clientMapsRoot, out error))
                {
                    MessageBox.Show(
                        this,
                        "Some cache files could not be deleted.\n\n" + error,
                        "Clear Cache",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                RestartApplication();
            }
            finally
            {
                Cursor.Current = previousCursor;
            }
        }

        private static bool TryClearEditorCache(string workspaceRoot, string pckIndexCacheRoot, string clientMapsRoot, out string error)
        {
            error = string.Empty;
            try
            {
                TryDeleteDirectory(workspaceRoot);
                TryDeleteDirectory(pckIndexCacheRoot);
                TryDeleteDirectory(clientMapsRoot);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            {
                return;
            }

            DirectoryInfo directory = new DirectoryInfo(path);
            foreach (FileInfo file in directory.GetFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            foreach (DirectoryInfo child in directory.GetDirectories("*", SearchOption.AllDirectories))
            {
                child.Attributes = FileAttributes.Normal;
            }

            directory.Attributes = FileAttributes.Normal;
            directory.Delete(true);
        }

        private static void RestartApplication()
        {
            string executablePath = Application.ExecutablePath;
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath)
            };

            Process.Start(startInfo);
            Application.Exit();
        }
    }
}

using System.Windows.Forms;

namespace FWEledit
{
    public sealed class DialogService : IDialogService
    {
        public void ShowMessage(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, IWin32Window owner = null)
        {
            if (owner == null)
            {
                MessageBox.Show(message, title, buttons, icon);
            }
            else
            {
                MessageBox.Show(owner, message, title, buttons, icon);
            }
        }

        public string ShowOpenFile(string title, string filter, string initialDirectory, IWin32Window owner = null)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = title ?? string.Empty;
                dialog.Filter = filter ?? "All Files (*.*)|*.*";
                dialog.InitialDirectory = initialDirectory ?? string.Empty;
                if (owner == null)
                {
                    return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : string.Empty;
                }
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : string.Empty;
            }
        }

        public string ShowSaveFile(string title, string filter, string initialDirectory, string defaultFileName, IWin32Window owner = null)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = title ?? string.Empty;
                dialog.Filter = filter ?? "All Files (*.*)|*.*";
                dialog.InitialDirectory = initialDirectory ?? string.Empty;
                dialog.FileName = defaultFileName ?? string.Empty;
                if (owner == null)
                {
                    return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : string.Empty;
                }
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : string.Empty;
            }
        }
    }
}

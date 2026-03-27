using System.Windows.Forms;

namespace FWEledit
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon, IWin32Window owner = null);
        string ShowOpenFile(string title, string filter, string initialDirectory, IWin32Window owner = null);
        string ShowSaveFile(string title, string filter, string initialDirectory, string defaultFileName, IWin32Window owner = null);
    }
}

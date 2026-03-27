using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SavePathService
    {
        public string PromptElementsSavePath(string initialDirectory, IWin32Window owner = null)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.InitialDirectory = initialDirectory ?? string.Empty;
                dialog.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
                if (owner == null)
                {
                    return dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName)
                        ? dialog.FileName
                        : string.Empty;
                }
                return dialog.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName)
                    ? dialog.FileName
                    : string.Empty;
            }
        }
    }
}

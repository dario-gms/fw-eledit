using System.Windows.Forms;

namespace FWEledit
{
    public sealed class TextExportDialogService
    {
        public string PromptForTextExportPath(string initialDirectory, string title, string defaultFileName)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.InitialDirectory = initialDirectory ?? string.Empty;
                dialog.Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*";
                if (!string.IsNullOrWhiteSpace(title))
                {
                    dialog.Title = title;
                }
                if (!string.IsNullOrWhiteSpace(defaultFileName))
                {
                    dialog.FileName = defaultFileName;
                }

                return dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName)
                    ? dialog.FileName
                    : string.Empty;
            }
        }
    }
}

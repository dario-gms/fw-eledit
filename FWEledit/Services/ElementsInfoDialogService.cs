using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ElementsInfoDialogService
    {
        public void ShowElementsInfo(ElementsFileInfoService infoService, DialogService dialogService, IWin32Window owner)
        {
            if (infoService == null || dialogService == null)
            {
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Elements File (*.data)|*.data|All Files (*.*)|*.*";
                if (dialog.ShowDialog() != DialogResult.OK || !File.Exists(dialog.FileName))
                {
                    return;
                }

                ElementsFileInfo info = infoService.ReadFileInfo(dialog.FileName);
                if (!info.Success)
                {
                    dialogService.ShowMessage(
                        string.IsNullOrWhiteSpace(info.ErrorMessage) ? "No File!" : info.ErrorMessage,
                        "FWEledit",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning,
                        owner);
                    return;
                }

                string timestamp = string.IsNullOrWhiteSpace(info.TimestampText) ? string.Empty : "\nTimestamp: " + info.TimestampText;
                dialogService.ShowMessage(
                    "File: " + info.FilePath + "\n\nVersion: " + info.Version.ToString() + "\nSignature: " + info.Signature.ToString() + timestamp,
                    "FWEledit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    owner);
            }
        }
    }
}

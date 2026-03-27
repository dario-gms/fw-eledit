using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigFileLoadUiService
    {
        public bool LoadConfig(
            IDialogService dialogService,
            ConfigWindowViewModel viewModel,
            ComboBox listComboBox,
            TextBox conversationListIndexTextBox,
            string initialDirectory,
            IWin32Window owner)
        {
            if (dialogService == null || viewModel == null)
            {
                return false;
            }

            string filePath = dialogService.ShowOpenFile(
                "Open Configuration File",
                "EL Configuration File (*.cfg)|*.cfg|All Files (*.*)|*.*",
                initialDirectory,
                owner);
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            viewModel.LoadConfig(filePath);
            ConfigData data = viewModel.Data;
            if (data == null)
            {
                return false;
            }

            if (listComboBox != null)
            {
                listComboBox.Items.Clear();
                for (int i = 0; i < data.ListCount; i++)
                {
                    listComboBox.Items.Add("[" + i + "]: " + data.ListNames[i]);
                }
            }

            if (conversationListIndexTextBox != null)
            {
                conversationListIndexTextBox.Text = data.ConversationListIndex.ToString();
            }

            return true;
        }
    }
}

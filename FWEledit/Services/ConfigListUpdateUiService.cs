using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigListUpdateUiService
    {
        public void UpdateConversationListIndex(ConfigData data, string text)
        {
            if (data == null)
            {
                return;
            }

            int value;
            if (int.TryParse(text, out value))
            {
                data.ConversationListIndex = value;
            }
        }

        public void UpdateListName(ConfigData data, ComboBox listComboBox, string text)
        {
            if (data == null || listComboBox == null || listComboBox.SelectedIndex < 0)
            {
                return;
            }

            data.ListNames[listComboBox.SelectedIndex] = text ?? string.Empty;
            listComboBox.Items[listComboBox.SelectedIndex] = "[" + listComboBox.SelectedIndex + "]: " + (text ?? string.Empty);
        }

        public void UpdateListOffset(ConfigData data, int selectedIndex, string text)
        {
            if (data == null || selectedIndex < 0)
            {
                return;
            }

            data.ListOffsets[selectedIndex] = text ?? string.Empty;
        }
    }
}

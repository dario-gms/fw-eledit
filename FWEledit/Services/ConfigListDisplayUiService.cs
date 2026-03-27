using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigListDisplayUiService
    {
        public void ApplySelection(
            ConfigData data,
            int selectedIndex,
            TextBox listNameTextBox,
            TextBox offsetTextBox,
            DataGridView itemGrid)
        {
            if (data == null || selectedIndex < 0)
            {
                return;
            }

            if (listNameTextBox != null)
            {
                listNameTextBox.Text = data.ListNames[selectedIndex];
            }
            if (offsetTextBox != null)
            {
                offsetTextBox.Text = data.ListOffsets[selectedIndex];
            }

            if (itemGrid != null)
            {
                itemGrid.Rows.Clear();
                for (int i = 0; i < data.FieldNames[selectedIndex].Length; i++)
                {
                    itemGrid.Rows.Add(new string[] { data.FieldNames[selectedIndex][i], data.FieldTypes[selectedIndex][i] });
                    itemGrid.Rows[i].HeaderCell.Value = i.ToString();
                }
            }
        }
    }
}

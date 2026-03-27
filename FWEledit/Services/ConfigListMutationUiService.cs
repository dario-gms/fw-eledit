using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigListMutationUiService
    {
        public void AddList(ConfigData data, ComboBox listComboBox)
        {
            if (data == null || listComboBox == null || listComboBox.SelectedIndex < 0)
            {
                return;
            }

            string[] templistNames = new string[data.ListNames.Length + 1];
            System.Array.Copy(data.ListNames, 0, templistNames, 0, listComboBox.SelectedIndex + 1);
            templistNames[listComboBox.SelectedIndex + 1] = "< LIST >";
            System.Array.Copy(data.ListNames, listComboBox.SelectedIndex + 1, templistNames, listComboBox.SelectedIndex + 2, data.ListNames.Length - listComboBox.SelectedIndex - 1);
            data.ListNames = templistNames;

            string[] templistOffsets = new string[data.ListOffsets.Length + 1];
            System.Array.Copy(data.ListOffsets, 0, templistOffsets, 0, listComboBox.SelectedIndex + 1);
            templistOffsets[listComboBox.SelectedIndex + 1] = "0";
            System.Array.Copy(data.ListOffsets, listComboBox.SelectedIndex + 1, templistOffsets, listComboBox.SelectedIndex + 2, data.ListOffsets.Length - listComboBox.SelectedIndex - 1);
            data.ListOffsets = templistOffsets;

            string[][] tempfieldNames = new string[data.FieldNames.Length + 1][];
            System.Array.Copy(data.FieldNames, 0, tempfieldNames, 0, listComboBox.SelectedIndex + 1);
            tempfieldNames[listComboBox.SelectedIndex + 1] = new string[] { "< NAME >" };
            System.Array.Copy(data.FieldNames, listComboBox.SelectedIndex + 1, tempfieldNames, listComboBox.SelectedIndex + 2, data.FieldNames.Length - listComboBox.SelectedIndex - 1);
            data.FieldNames = tempfieldNames;

            string[][] tempfieldTypes = new string[data.FieldTypes.Length + 1][];
            System.Array.Copy(data.FieldTypes, 0, tempfieldTypes, 0, listComboBox.SelectedIndex + 1);
            tempfieldTypes[listComboBox.SelectedIndex + 1] = new string[] { "< TYPE >" };
            System.Array.Copy(data.FieldTypes, listComboBox.SelectedIndex + 1, tempfieldTypes, listComboBox.SelectedIndex + 2, data.FieldTypes.Length - listComboBox.SelectedIndex - 1);
            data.FieldTypes = tempfieldTypes;

            listComboBox.Items.Add("[" + (data.ListNames.Length - 1) + "]: " + data.ListNames[data.ListNames.Length - 1]);
            for (int i = 0; i < data.ListNames.Length; i++)
            {
                listComboBox.Items[i] = "[" + i + "]: " + data.ListNames[i];
            }
            listComboBox.SelectedIndex = listComboBox.SelectedIndex + 1;
        }

        public void DeleteList(ConfigData data, ComboBox listComboBox, DataGridView itemGrid)
        {
            if (data == null || listComboBox == null)
            {
                return;
            }
            if (listComboBox.SelectedIndex < 0)
            {
                return;
            }

            int index = listComboBox.SelectedIndex;

            string[] itemp = new string[data.ListOffsets.Length - 1];
            System.Array.Copy(data.ListOffsets, 0, itemp, 0, index);
            System.Array.Copy(data.ListOffsets, index + 1, itemp, index, data.ListOffsets.Length - 1 - index);
            data.ListOffsets = itemp;

            string[] stemp = new string[data.ListNames.Length - 1];
            System.Array.Copy(data.ListNames, 0, stemp, 0, index);
            System.Array.Copy(data.ListNames, index + 1, stemp, index, data.ListNames.Length - 1 - index);
            data.ListNames = stemp;

            string[][] astemp = new string[data.FieldNames.Length - 1][];
            System.Array.Copy(data.FieldNames, 0, astemp, 0, index);
            System.Array.Copy(data.FieldNames, index + 1, astemp, index, data.FieldNames.Length - 1 - index);
            data.FieldNames = astemp;

            astemp = new string[data.FieldTypes.Length - 1][];
            System.Array.Copy(data.FieldTypes, 0, astemp, 0, index);
            System.Array.Copy(data.FieldTypes, index + 1, astemp, index, data.FieldTypes.Length - 1 - index);
            data.FieldTypes = astemp;

            if (itemGrid != null)
            {
                itemGrid.Rows.Clear();
            }
            listComboBox.Items.Clear();

            for (int i = 0; i < data.ListNames.Length; i++)
            {
                listComboBox.Items.Add("[" + i + "]: " + data.ListNames[i]);
            }

            index--;
            if (index < 0 && listComboBox.Items.Count > 0)
            {
                index++;
            }
            listComboBox.SelectedIndex = index;
        }
    }
}

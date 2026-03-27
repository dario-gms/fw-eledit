using System.Collections;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigRowPasteUiService
    {
        public int PasteSelection(ConfigData data, int listIndex, DataGridView grid, string[] copyNames, string[] copyTypes)
        {
            if (data == null || grid == null || listIndex < 0)
            {
                return -1;
            }
            if (copyNames == null || copyTypes == null || copyNames.Length == 0 || copyTypes.Length == 0)
            {
                return -1;
            }
            if (data.FieldNames == null || data.FieldTypes == null)
            {
                return -1;
            }
            if (listIndex >= data.FieldNames.Length || listIndex >= data.FieldTypes.Length)
            {
                return -1;
            }
            if (grid.SelectedCells == null || grid.SelectedCells.Count == 0)
            {
                return -1;
            }

            ArrayList selectedCellsIndexes = new ArrayList();
            ArrayList locSelectedCellsIndexes = new ArrayList();
            for (int i = 0; i < grid.SelectedCells.Count; i++)
            {
                bool check = true;
                for (int k = 0; k < selectedCellsIndexes.Count; k++)
                {
                    if ((int)selectedCellsIndexes[k] == grid.SelectedCells[i].RowIndex)
                    {
                        check = false;
                        break;
                    }
                }
                if (check)
                {
                    selectedCellsIndexes.Add(grid.SelectedCells[i].RowIndex);
                }
            }
            selectedCellsIndexes.Sort();

            int currentRowIndex = -1;
            if (grid.CurrentCell != null)
            {
                currentRowIndex = grid.CurrentCell.RowIndex;
            }

            for (int i = 0; i < grid.SelectedCells.Count; i++)
            {
                for (int ic = 0; ic < copyNames.Length; ic++)
                {
                    for (int p = 0; p < selectedCellsIndexes.Count; p++)
                    {
                        locSelectedCellsIndexes.Add(selectedCellsIndexes[p]);
                    }
                    if (data.FieldNames[listIndex].Length > 0)
                    {
                        string[] temp = new string[data.FieldNames[listIndex].Length + 1];
                        System.Array.Copy(data.FieldNames[listIndex], 0, temp, 0, (int)locSelectedCellsIndexes[i] + 1);
                        temp[(int)locSelectedCellsIndexes[i] + 1] = copyNames[ic];
                        System.Array.Copy(data.FieldNames[listIndex], (int)locSelectedCellsIndexes[i] + 1, temp, (int)locSelectedCellsIndexes[i] + 2, data.FieldNames[listIndex].Length - (int)locSelectedCellsIndexes[i] - 1);
                        data.FieldNames[listIndex] = temp;

                        temp = new string[data.FieldTypes[listIndex].Length + 1];
                        System.Array.Copy(data.FieldTypes[listIndex], 0, temp, 0, (int)locSelectedCellsIndexes[i] + 1);
                        temp[(int)locSelectedCellsIndexes[i] + 1] = copyTypes[ic];
                        System.Array.Copy(data.FieldTypes[listIndex], (int)locSelectedCellsIndexes[i] + 1, temp, (int)locSelectedCellsIndexes[i] + 2, data.FieldTypes[listIndex].Length - (int)locSelectedCellsIndexes[i] - 1);
                        data.FieldTypes[listIndex] = temp;
                    }
                    else
                    {
                        data.FieldNames[listIndex] = new string[1] { copyNames[ic] };
                        data.FieldTypes[listIndex] = new string[1] { copyTypes[ic] };
                    }

                    for (int ii = 0; ii < grid.SelectedCells.Count; ii++)
                    {
                        locSelectedCellsIndexes[ii] = (int)locSelectedCellsIndexes[ii] + 1;
                    }
                    for (int ii = i; ii < grid.SelectedCells.Count; ii++)
                    {
                        if (ic < copyNames.Length - 1)
                        {
                            selectedCellsIndexes[ii] = (int)selectedCellsIndexes[ii] + 1 + ic;
                        }
                        else
                        {
                            selectedCellsIndexes[ii] = (int)selectedCellsIndexes[ii] + ic;
                        }
                    }
                    currentRowIndex = (int)locSelectedCellsIndexes[0];
                }
            }

            return currentRowIndex;
        }
    }
}

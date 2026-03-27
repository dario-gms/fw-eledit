using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NavigationSelectionService
    {
        public void RestoreSelection(
            NavigationSettingsSnapshot navSettings,
            ComboBox listComboBox,
            DataGridView elementGrid,
            MainWindowViewModel viewModel,
            Action persistAction)
        {
            if (navSettings == null || listComboBox == null || listComboBox.Items.Count == 0 || viewModel == null)
            {
                return;
            }

            int savedList = navSettings.LastListIndex;
            int savedItemId = navSettings.LastItemId;

            viewModel.IsRestoringSessionState = true;
            try
            {
                if (savedList < 0 || savedList >= listComboBox.Items.Count)
                {
                    savedList = 0;
                }
                listComboBox.SelectedIndex = savedList;

                if (savedItemId > 0 && listComboBox.SelectedIndex > -1 && elementGrid != null)
                {
                    for (int row = 0; row < elementGrid.Rows.Count; row++)
                    {
                        int rowId;
                        if (int.TryParse(Convert.ToString(elementGrid.Rows[row].Cells[0].Value), out rowId) && rowId == savedItemId)
                        {
                            elementGrid.ClearSelection();
                            elementGrid.CurrentCell = elementGrid[0, row];
                            elementGrid.Rows[row].Selected = true;
                            elementGrid.FirstDisplayedScrollingRowIndex = row;
                            break;
                        }
                    }
                }
            }
            finally
            {
                viewModel.IsRestoringSessionState = false;
            }

            if (persistAction != null)
            {
                persistAction();
            }
        }
    }
}

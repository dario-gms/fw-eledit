using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NavigationSnapshotService
    {
        public NavigationSnapshot CaptureSnapshot(int listIndex, DataGridView grid)
        {
            NavigationSnapshot snapshot = new NavigationSnapshot
            {
                ListIndex = listIndex,
                ItemId = -1,
                GridRowIndex = -1,
                FirstDisplayedRow = -1
            };

            if (grid != null)
            {
                if (grid.CurrentCell != null)
                {
                    snapshot.GridRowIndex = grid.CurrentCell.RowIndex;
                    if (snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < grid.Rows.Count)
                    {
                        int id;
                        if (int.TryParse(Convert.ToString(grid.Rows[snapshot.GridRowIndex].Cells[0].Value), out id))
                        {
                            snapshot.ItemId = id;
                        }
                    }
                }
                if (grid.Rows.Count > 0)
                {
                    snapshot.FirstDisplayedRow = grid.FirstDisplayedScrollingRowIndex;
                }
            }

            return snapshot;
        }

        public void RestoreSnapshot(NavigationSnapshot snapshot, ComboBox listCombo, DataGridView grid, Func<bool> getIsRestoring, Action<bool> setIsRestoring)
        {
            if (listCombo == null || grid == null)
            {
                return;
            }
            if (snapshot.ListIndex < 0 || snapshot.ListIndex >= listCombo.Items.Count)
            {
                return;
            }

            bool previousRestore = getIsRestoring != null && getIsRestoring();
            if (setIsRestoring != null)
            {
                setIsRestoring(true);
            }
            try
            {
                listCombo.SelectedIndex = snapshot.ListIndex;

                int targetRow = -1;
                if (snapshot.ItemId > 0)
                {
                    for (int row = 0; row < grid.Rows.Count; row++)
                    {
                        int rowId;
                        if (int.TryParse(Convert.ToString(grid.Rows[row].Cells[0].Value), out rowId) && rowId == snapshot.ItemId)
                        {
                            targetRow = row;
                            break;
                        }
                    }
                }

                if (targetRow < 0 && snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < grid.Rows.Count)
                {
                    targetRow = snapshot.GridRowIndex;
                }

                if (targetRow >= 0 && targetRow < grid.Rows.Count)
                {
                    grid.ClearSelection();
                    grid.CurrentCell = grid[0, targetRow];
                    grid.Rows[targetRow].Selected = true;
                    try
                    {
                        if (snapshot.FirstDisplayedRow >= 0 && snapshot.FirstDisplayedRow < grid.Rows.Count)
                        {
                            grid.FirstDisplayedScrollingRowIndex = snapshot.FirstDisplayedRow;
                        }
                        else
                        {
                            grid.FirstDisplayedScrollingRowIndex = targetRow;
                        }
                    }
                    catch
                    { }
                }
            }
            finally
            {
                if (setIsRestoring != null)
                {
                    setIsRestoring(previousRestore);
                }
            }
        }
    }
}

using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NavigationSnapshotService
    {
        public NavigationSnapshot CaptureSnapshot(int listIndex, DataGridView grid)
        {
            return CaptureSnapshot(listIndex, grid, null);
        }

        public NavigationSnapshot CaptureSnapshot(int listIndex, DataGridView elementGrid, DataGridView itemGrid)
        {
            NavigationSnapshot snapshot = new NavigationSnapshot
            {
                ListIndex = listIndex,
                ItemId = -1,
                GridRowIndex = -1,
                FirstDisplayedRow = -1,
                ItemGridRowIndex = -1,
                ItemGridColumnIndex = -1,
                ItemGridFirstDisplayedRow = -1
            };

            if (elementGrid != null)
            {
                if (elementGrid.CurrentCell != null)
                {
                    snapshot.GridRowIndex = elementGrid.CurrentCell.RowIndex;
                    if (snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < elementGrid.Rows.Count)
                    {
                        int id;
                        if (int.TryParse(Convert.ToString(elementGrid.Rows[snapshot.GridRowIndex].Cells[0].Value), out id))
                        {
                            snapshot.ItemId = id;
                        }
                    }
                }
                if (elementGrid.Rows.Count > 0)
                {
                    snapshot.FirstDisplayedRow = elementGrid.FirstDisplayedScrollingRowIndex;
                }
            }

            if (itemGrid != null)
            {
                if (itemGrid.CurrentCell != null)
                {
                    snapshot.ItemGridRowIndex = itemGrid.CurrentCell.RowIndex;
                    snapshot.ItemGridColumnIndex = itemGrid.CurrentCell.ColumnIndex;
                }
                else if (itemGrid.CurrentRow != null)
                {
                    snapshot.ItemGridRowIndex = itemGrid.CurrentRow.Index;
                    snapshot.ItemGridColumnIndex = itemGrid.Columns.Count > 2 ? 2 : 0;
                }

                if (itemGrid.Rows.Count > 0)
                {
                    snapshot.ItemGridFirstDisplayedRow = itemGrid.FirstDisplayedScrollingRowIndex;
                }
            }

            return snapshot;
        }

        public void RestoreSnapshot(NavigationSnapshot snapshot, ComboBox listCombo, DataGridView grid, Func<bool> getIsRestoring, Action<bool> setIsRestoring)
        {
            RestoreSnapshot(snapshot, listCombo, grid, null, getIsRestoring, setIsRestoring);
        }

        public void RestoreSnapshot(
            NavigationSnapshot snapshot,
            ComboBox listCombo,
            DataGridView elementGrid,
            DataGridView itemGrid,
            Func<bool> getIsRestoring,
            Action<bool> setIsRestoring)
        {
            if (snapshot == null || listCombo == null || elementGrid == null)
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
                    for (int row = 0; row < elementGrid.Rows.Count; row++)
                    {
                        int rowId;
                        if (int.TryParse(Convert.ToString(elementGrid.Rows[row].Cells[0].Value), out rowId) && rowId == snapshot.ItemId)
                        {
                            targetRow = row;
                            break;
                        }
                    }
                }

                if (targetRow < 0 && snapshot.GridRowIndex >= 0 && snapshot.GridRowIndex < elementGrid.Rows.Count)
                {
                    targetRow = snapshot.GridRowIndex;
                }

                if (targetRow >= 0 && targetRow < elementGrid.Rows.Count)
                {
                    elementGrid.ClearSelection();
                    elementGrid.CurrentCell = elementGrid[0, targetRow];
                    elementGrid.Rows[targetRow].Selected = true;
                    try
                    {
                        if (snapshot.FirstDisplayedRow >= 0 && snapshot.FirstDisplayedRow < elementGrid.Rows.Count)
                        {
                            elementGrid.FirstDisplayedScrollingRowIndex = snapshot.FirstDisplayedRow;
                        }
                        else
                        {
                            elementGrid.FirstDisplayedScrollingRowIndex = targetRow;
                        }
                    }
                    catch
                    { }
                }

                RestoreItemGridPosition(snapshot, itemGrid);
            }
            finally
            {
                if (setIsRestoring != null)
                {
                    setIsRestoring(previousRestore);
                }
            }
        }

        private static void RestoreItemGridPosition(NavigationSnapshot snapshot, DataGridView itemGrid)
        {
            if (snapshot == null || itemGrid == null || itemGrid.Rows.Count == 0)
            {
                return;
            }

            int targetRow = snapshot.ItemGridRowIndex;
            if (targetRow < 0 || targetRow >= itemGrid.Rows.Count)
            {
                return;
            }

            int targetColumn = snapshot.ItemGridColumnIndex;
            if (targetColumn < 0 || targetColumn >= itemGrid.Columns.Count)
            {
                targetColumn = itemGrid.Columns.Count > 2 ? 2 : 0;
            }

            itemGrid.ClearSelection();
            itemGrid.CurrentCell = itemGrid[targetColumn, targetRow];
            itemGrid.Rows[targetRow].Cells[targetColumn].Selected = true;

            try
            {
                if (snapshot.ItemGridFirstDisplayedRow >= 0 && snapshot.ItemGridFirstDisplayedRow < itemGrid.Rows.Count)
                {
                    itemGrid.FirstDisplayedScrollingRowIndex = snapshot.ItemGridFirstDisplayedRow;
                }
                else
                {
                    itemGrid.FirstDisplayedScrollingRowIndex = targetRow;
                }
            }
            catch
            { }
        }
    }
}

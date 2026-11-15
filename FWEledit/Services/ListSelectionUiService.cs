using System.Collections.Generic;
using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ListSelectionUiService
    {
        private readonly Dictionary<DataGridView, List<object[]>> virtualRowsByGrid =
            new Dictionary<DataGridView, List<object[]>>();
        private readonly HashSet<DataGridView> configuredVirtualGrids =
            new HashSet<DataGridView>();

        public void ApplySelection(
            ListSelectionWorkflowService workflowService,
            ListSelectionRequest request,
            int listIndex,
            int conversationListIndex,
            DataGridView elementGrid,
            DataGridView itemGrid,
            TextBox offsetBox,
            ToolStripMenuItem xrefMenuItem,
            System.Action<int, int, DataGridViewRow> applyQualityColor,
            System.Action updateDescription,
            System.Action updatePickIcon,
            System.Action persistNavigation)
        {
            if (workflowService == null || elementGrid == null || itemGrid == null)
            {
                return;
            }

            ListSelectionResult selection = workflowService.BuildSelection(request);
            int currentItemId = GetCurrentItemId(elementGrid);

            elementGrid.SuspendLayout();
            ClearElementGrid(elementGrid);
            itemGrid.Rows.Clear();

            if (!selection.Success)
            {
                elementGrid.ResumeLayout();
                return;
            }

            if (offsetBox != null)
            {
                offsetBox.Text = selection.OffsetText ?? string.Empty;
            }
            if (xrefMenuItem != null)
            {
                xrefMenuItem.Enabled = selection.HasXref;
            }

            List<object[]> rows = selection.Rows ?? new List<object[]>();
            if (request.UseLightweightRows)
            {
                SetVirtualRows(elementGrid, rows);
            }
            else if (rows.Count > 0)
            {
                DataGridViewRow[] dgRows = new DataGridViewRow[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    DataGridViewRow row = (DataGridViewRow)elementGrid.RowTemplate.Clone();
                    row.CreateCells(elementGrid, rows[i]);
                    row.Tag = i;
                    if (!request.UseLightweightRows
                        && listIndex != conversationListIndex
                        && applyQualityColor != null)
                    {
                        applyQualityColor(listIndex, i, row);
                    }
                    dgRows[i] = row;
                }
                elementGrid.Rows.AddRange(dgRows);
            }

            elementGrid.ResumeLayout();
            if (elementGrid.Rows.Count > 0)
            {
                int restoredRow;
                if (currentItemId > 0 && TryRestoreCurrentItem(elementGrid, currentItemId, out restoredRow))
                {
                    try
                    {
                        elementGrid.FirstDisplayedScrollingRowIndex = restoredRow;
                    }
                    catch
                    {
                    }
                }
                else if (elementGrid.CurrentCell == null)
                {
                    elementGrid.CurrentCell = elementGrid.Rows[0].Cells[0];
                }
            }

            if (updateDescription != null)
            {
                updateDescription();
            }
            if (updatePickIcon != null)
            {
                updatePickIcon();
            }
            if (persistNavigation != null)
            {
                persistNavigation();
            }
        }

        private void ClearElementGrid(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            virtualRowsByGrid.Remove(grid);
            if (grid.VirtualMode)
            {
                grid.RowCount = 0;
                grid.VirtualMode = false;
            }
            grid.Rows.Clear();
        }

        private void SetVirtualRows(DataGridView grid, List<object[]> rows)
        {
            if (grid == null)
            {
                return;
            }

            EnsureVirtualGridHandlers(grid);
            virtualRowsByGrid[grid] = rows ?? new List<object[]>();
            grid.VirtualMode = true;
            grid.RowCount = rows != null ? rows.Count : 0;
        }

        private void EnsureVirtualGridHandlers(DataGridView grid)
        {
            if (grid == null || configuredVirtualGrids.Contains(grid))
            {
                return;
            }

            grid.CellValueNeeded += VirtualGridCellValueNeeded;
            grid.CellValuePushed += VirtualGridCellValuePushed;
            grid.Disposed += delegate
            {
                virtualRowsByGrid.Remove(grid);
                configuredVirtualGrids.Remove(grid);
            };
            configuredVirtualGrids.Add(grid);
        }

        private void VirtualGridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            List<object[]> rows;
            if (grid == null
                || !virtualRowsByGrid.TryGetValue(grid, out rows)
                || e.RowIndex < 0
                || e.RowIndex >= rows.Count
                || rows[e.RowIndex] == null
                || e.ColumnIndex < 0
                || e.ColumnIndex >= rows[e.RowIndex].Length)
            {
                return;
            }

            e.Value = rows[e.RowIndex][e.ColumnIndex];
        }

        private void VirtualGridCellValuePushed(object sender, DataGridViewCellValueEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            List<object[]> rows;
            if (grid == null
                || !virtualRowsByGrid.TryGetValue(grid, out rows)
                || e.RowIndex < 0
                || e.RowIndex >= rows.Count
                || rows[e.RowIndex] == null
                || e.ColumnIndex < 0
                || e.ColumnIndex >= rows[e.RowIndex].Length)
            {
                return;
            }

            rows[e.RowIndex][e.ColumnIndex] = e.Value;
        }

        private static int GetCurrentItemId(DataGridView elementGrid)
        {
            if (elementGrid == null || elementGrid.CurrentCell == null)
            {
                return 0;
            }

            int rowIndex = elementGrid.CurrentCell.RowIndex;
            if (rowIndex < 0 || rowIndex >= elementGrid.Rows.Count)
            {
                return 0;
            }

            int itemId;
            return int.TryParse(Convert.ToString(elementGrid.Rows[rowIndex].Cells[0].Value), out itemId)
                ? itemId
                : 0;
        }

        private static bool TryRestoreCurrentItem(DataGridView elementGrid, int itemId, out int restoredRow)
        {
            restoredRow = -1;
            if (elementGrid == null || itemId <= 0)
            {
                return false;
            }

            for (int rowIndex = 0; rowIndex < elementGrid.Rows.Count; rowIndex++)
            {
                int rowId;
                if (!int.TryParse(Convert.ToString(elementGrid.Rows[rowIndex].Cells[0].Value), out rowId) || rowId != itemId)
                {
                    continue;
                }

                elementGrid.ClearSelection();
                elementGrid.CurrentCell = elementGrid.Rows[rowIndex].Cells[0];
                elementGrid.Rows[rowIndex].Selected = true;
                restoredRow = rowIndex;
                return true;
            }

            return false;
        }
    }
}

using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemListThemeService
    {
        public void ApplyTheme(DataGridView grid, bool darkMode)
        {
            ApplyDarkTheme(grid);
        }

        public void ApplyDarkTheme(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.BackgroundColor = Color.FromArgb(24, 26, 30);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(24, 26, 30);
            grid.RowsDefaultCellStyle.BackColor = Color.FromArgb(24, 26, 30);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(28, 31, 36);
            grid.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(24, 26, 30);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 39, 45);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(226, 231, 238);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(35, 39, 45);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(226, 231, 238);
            grid.GridColor = Color.FromArgb(42, 48, 58);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(222, 226, 232);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(49, 93, 130);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            grid.RowTemplate.Height = 38;
        }

        public void ApplyLightTheme(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }

            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.BackgroundColor = Color.White;
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.RowsDefaultCellStyle.BackColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);
            grid.RowHeadersDefaultCellStyle.BackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(225, 231, 238);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(29, 36, 45);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(225, 231, 238);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(29, 36, 45);
            grid.GridColor = Color.FromArgb(211, 218, 226);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(29, 36, 45);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(47, 111, 159);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            grid.RowTemplate.Height = 38;
            ResetRowBackgrounds(grid);
        }

        private static void ResetRowBackgrounds(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                row.DefaultCellStyle.BackColor = Color.Empty;
                row.DefaultCellStyle.SelectionBackColor = Color.Empty;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Style.BackColor = Color.Empty;
                    if (cell.ColumnIndex != 2)
                    {
                        cell.Style.ForeColor = Color.Empty;
                    }
                }
            }
        }
    }
}

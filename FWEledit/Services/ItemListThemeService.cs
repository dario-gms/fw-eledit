using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemListThemeService
    {
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
            grid.GridColor = Color.FromArgb(49, 54, 63);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(222, 226, 232);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(49, 93, 130);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            grid.RowTemplate.Height = 38;
        }
    }
}

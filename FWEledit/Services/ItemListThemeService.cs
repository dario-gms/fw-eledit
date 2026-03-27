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

            grid.BackgroundColor = Color.Black;
            grid.DefaultCellStyle.BackColor = Color.Black;
            grid.RowsDefaultCellStyle.BackColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.Black;
            grid.RowHeadersDefaultCellStyle.BackColor = Color.Black;
            grid.GridColor = Color.FromArgb(40, 40, 40);
            grid.DefaultCellStyle.ForeColor = Color.Gainsboro;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
        }
    }
}

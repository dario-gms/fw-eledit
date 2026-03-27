using System.Reflection;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GridDoubleBufferService
    {
        public void EnableDoubleBuffer(DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }
            try
            {
                typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(grid, true, null);
            }
            catch
            {
            }
        }
    }
}

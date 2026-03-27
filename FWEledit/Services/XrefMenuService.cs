using System.Windows.Forms;

namespace FWEledit
{
    public sealed class XrefMenuService
    {
        public void SetVisibility(ToolStripSeparator separator, ToolStripMenuItem menuItem, bool visible)
        {
            if (separator != null)
            {
                separator.Visible = visible;
            }
            if (menuItem != null)
            {
                menuItem.Visible = visible;
            }
        }
    }
}

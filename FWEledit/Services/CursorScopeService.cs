using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class CursorScopeService
    {
        public void RunWithCursor(Action<Cursor> setCursor, Cursor busyCursor, Action work)
        {
            if (setCursor == null || work == null)
            {
                return;
            }

            try
            {
                setCursor(busyCursor ?? Cursors.WaitCursor);
                work();
            }
            finally
            {
                setCursor(Cursors.Default);
            }
        }
    }
}

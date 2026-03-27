using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowEventWiringService
    {
        public void WireBasicEvents(
            Form owner,
            EventHandler shownHandler,
            EventHandler resizeHandler,
            FormClosingEventHandler closingHandler,
            TextBox searchBox,
            EventHandler searchChangedHandler,
            KeyEventHandler searchKeyDownHandler)
        {
            if (owner != null)
            {
                if (shownHandler != null)
                {
                    owner.Shown += shownHandler;
                }
                if (resizeHandler != null)
                {
                    owner.Resize += resizeHandler;
                }
                if (closingHandler != null)
                {
                    owner.FormClosing += closingHandler;
                }
            }

            if (searchBox != null)
            {
                if (searchChangedHandler != null)
                {
                    searchBox.TextChanged += searchChangedHandler;
                }
                if (searchKeyDownHandler != null)
                {
                    searchBox.KeyDown += searchKeyDownHandler;
                }
            }
        }
    }
}

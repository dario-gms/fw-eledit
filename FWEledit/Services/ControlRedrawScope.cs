using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ControlRedrawScope : IDisposable
    {
        private const int WM_SETREDRAW = 0x000B;

        private readonly Control control;
        private readonly bool redrawSuspended;
        private bool disposed;

        public ControlRedrawScope(Control control)
        {
            this.control = control;
            if (control == null || control.IsDisposed)
            {
                return;
            }

            control.SuspendLayout();
            if (control.IsHandleCreated)
            {
                SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                redrawSuspended = true;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;

            if (control == null || control.IsDisposed)
            {
                return;
            }

            if (redrawSuspended && control.IsHandleCreated)
            {
                SendMessage(control.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            }

            control.ResumeLayout(false);
            control.Invalidate(true);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}

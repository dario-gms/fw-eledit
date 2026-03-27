using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SaveProgressService
    {
        public void Begin(ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        public void Begin(ProgressBar progressBar)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = 0;
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        public void Set(ColorProgressBar.ColorProgressBar progressBar, int value)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                int bounded = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, value));
                progressBar.Value = bounded;
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        public void Set(ProgressBar progressBar, int value)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                int bounded = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, value));
                progressBar.Value = bounded;
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        public void End(ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                progressBar.Value = 0;
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }

        public void End(ProgressBar progressBar)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                progressBar.Value = 0;
                progressBar.Refresh();
                Application.DoEvents();
            }
            catch
            {
            }
        }
    }
}

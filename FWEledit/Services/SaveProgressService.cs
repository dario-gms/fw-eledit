using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SaveProgressService
    {
        private sealed class ProgressScopeState
        {
            public int StartValue { get; set; }
            public int EndValue { get; set; }
            public object PreviousTag { get; set; }
        }

        public void Begin(ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                SetVisible(progressBar, true);
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
                SetVisible(progressBar, false);
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

        public static void SetVisible(Control progressBar, bool visible)
        {
            if (progressBar == null)
            {
                return;
            }

            progressBar.Visible = visible;
            TableLayoutPanel table = progressBar.Parent as TableLayoutPanel;
            if (table != null)
            {
                int row = table.GetRow(progressBar);
                if (row >= 0 && row < table.RowStyles.Count)
                {
                    table.RowStyles[row].SizeType = SizeType.Absolute;
                    table.RowStyles[row].Height = visible ? 10F : 0F;
                }
                table.PerformLayout();
            }
            progressBar.Refresh();
            Application.DoEvents();
        }

        public static void BeginScope(Control progressBar, int startValue, int endValue)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                int start = Math.Max(0, Math.Min(100, startValue));
                int end = Math.Max(start, Math.Min(100, endValue));
                progressBar.Tag = new ProgressScopeState
                {
                    StartValue = start,
                    EndValue = end,
                    PreviousTag = progressBar.Tag
                };

                ProgressBar windowsProgressBar = progressBar as ProgressBar;
                if (windowsProgressBar != null)
                {
                    windowsProgressBar.Minimum = 0;
                    windowsProgressBar.Maximum = 100;
                    windowsProgressBar.Value = start;
                    windowsProgressBar.Refresh();
                    Application.DoEvents();
                    return;
                }

                ColorProgressBar.ColorProgressBar colorProgressBar = progressBar as ColorProgressBar.ColorProgressBar;
                if (colorProgressBar != null)
                {
                    colorProgressBar.Minimum = 0;
                    colorProgressBar.Maximum = 100;
                    colorProgressBar.Value = start;
                    colorProgressBar.Refresh();
                    Application.DoEvents();
                }
            }
            catch
            {
            }
        }

        public static void EndScope(Control progressBar)
        {
            if (progressBar == null)
            {
                return;
            }

            try
            {
                ProgressScopeState scope = progressBar.Tag as ProgressScopeState;
                progressBar.Tag = scope != null ? scope.PreviousTag : null;
            }
            catch
            {
            }
        }

        public static bool TrySetScopedValue(ColorProgressBar.ColorProgressBar progressBar, int completedUnits, int totalUnits)
        {
            if (progressBar == null)
            {
                return false;
            }

            try
            {
                ProgressScopeState scope = progressBar.Tag as ProgressScopeState;
                if (scope == null)
                {
                    return false;
                }

                int total = Math.Max(1, totalUnits);
                int completed = Math.Max(0, Math.Min(total, completedUnits));
                int range = Math.Max(0, scope.EndValue - scope.StartValue);
                int mappedValue = scope.StartValue;
                if (range > 0)
                {
                    mappedValue = scope.StartValue + (int)Math.Round((double)range * completed / total);
                }

                mappedValue = Math.Max(0, Math.Min(100, mappedValue));
                progressBar.Minimum = 0;
                progressBar.Maximum = 100;
                progressBar.Value = mappedValue;
                progressBar.Refresh();
                Application.DoEvents();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

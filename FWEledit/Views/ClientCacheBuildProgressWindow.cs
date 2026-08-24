using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ClientCacheBuildProgressWindow : Form
    {
        private readonly Label titleLabel;
        private readonly Label detailLabel;
        private readonly Label noteLabel;
        private readonly ProgressBar progressBar;
        private bool allowClose;

        public ClientCacheBuildProgressWindow()
        {
            Text = "FWEledit - Building client cache";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 165);
            BackColor = Color.FromArgb(18, 22, 27);
            ForeColor = Color.White;

            titleLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 18,
                Width = 524,
                Height = 24,
                Font = new Font(Font.FontFamily, 10.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "Building client cache"
            };

            detailLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 48,
                Width = 524,
                Height = 46,
                ForeColor = Color.FromArgb(211, 222, 235),
                Text = "Please wait while FWEledit indexes this client."
            };

            progressBar = new ProgressBar
            {
                Left = 18,
                Top = 102,
                Width = 524,
                Height = 18,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 25
            };

            noteLabel = new Label
            {
                AutoSize = false,
                Left = 18,
                Top = 132,
                Width = 524,
                Height = 20,
                ForeColor = Color.FromArgb(150, 166, 184),
                Text = "The first opening can take a while. Future openings should be faster."
            };

            Controls.Add(titleLabel);
            Controls.Add(detailLabel);
            Controls.Add(progressBar);
            Controls.Add(noteLabel);
        }

        public void UpdateProgress(string stage, string detail, int percent, bool indeterminate)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, int, bool>(UpdateProgress), stage, detail, percent, indeterminate);
                return;
            }

            titleLabel.Text = string.IsNullOrWhiteSpace(stage)
                ? "Building client cache"
                : stage;
            detailLabel.Text = string.IsNullOrWhiteSpace(detail)
                ? "Please wait while FWEledit indexes this client."
                : detail;

            if (indeterminate)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 25;
                return;
            }

            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, percent));
        }

        public void AllowCloseAndClose()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(AllowCloseAndClose));
                return;
            }

            allowClose = true;
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowClose)
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }
    }
}

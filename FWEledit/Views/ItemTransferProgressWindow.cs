using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemTransferProgressWindow : Form
    {
        private readonly Label stageLabel;
        private readonly Label detailLabel;
        private readonly ProgressBar progressBar;
        private readonly Button cancelButton;
        private bool allowClose;

        public ItemTransferProgressWindow(string title)
        {
            Cancellation = new CancellationTokenSource();

            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 154);
            BackColor = Color.FromArgb(17, 21, 26);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            stageLabel = new Label
            {
                AutoSize = false,
                Left = 16,
                Top = 16,
                Width = 488,
                Height = 22,
                ForeColor = Color.White,
                Text = "Preparing export..."
            };

            detailLabel = new Label
            {
                AutoSize = false,
                Left = 16,
                Top = 42,
                Width = 488,
                Height = 40,
                ForeColor = Color.FromArgb(190, 210, 230),
                Text = string.Empty
            };

            progressBar = new ProgressBar
            {
                Left = 16,
                Top = 88,
                Width = 488,
                Height = 18,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 24
            };

            cancelButton = new Button
            {
                Left = 390,
                Top = 118,
                Width = 114,
                Height = 28,
                Text = "Cancel"
            };
            cancelButton.Click += (sender, args) =>
            {
                Cancellation.Cancel();
                cancelButton.Enabled = false;
                cancelButton.Text = "Cancelling...";
            };

            Controls.Add(stageLabel);
            Controls.Add(detailLabel);
            Controls.Add(progressBar);
            Controls.Add(cancelButton);
        }

        public CancellationTokenSource Cancellation { get; private set; }

        public void UpdateProgress(ItemTransferProgressInfo info)
        {
            if (IsDisposed || info == null)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action<ItemTransferProgressInfo>(UpdateProgress), info);
                return;
            }

            stageLabel.Text = info.Stage ?? string.Empty;
            detailLabel.Text = info.Detail ?? string.Empty;

            if (info.IsIndeterminate || info.Total <= 0)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                progressBar.MarqueeAnimationSpeed = 24;
                return;
            }

            progressBar.MarqueeAnimationSpeed = 0;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(1, info.Total);
            progressBar.Value = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, info.Current));
        }

        public void AllowCloseAndClose()
        {
            if (IsDisposed)
            {
                return;
            }

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
            if (!allowClose && !Cancellation.IsCancellationRequested)
            {
                Cancellation.Cancel();
                cancelButton.Enabled = false;
                cancelButton.Text = "Cancelling...";
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Cancellation != null)
            {
                Cancellation.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}

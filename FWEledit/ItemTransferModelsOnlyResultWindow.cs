using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemTransferModelsOnlyResultWindow : Form
    {
        private readonly TextBox summaryTextBox;
        private readonly Button copyButton;
        private readonly Button closeButton;

        public ItemTransferModelsOnlyResultWindow(string summary, string importDetails)
        {
            Text = "Import Models Only";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            ShowInTaskbar = false;
            MinimumSize = new Size(620, 360);
            ClientSize = new Size(720, 430);
            Font = SystemFonts.MessageBoxFont;

            Label titleLabel = new Label
            {
                Text = "Model assets imported. No Equipment Essence item was created or changed.",
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(680, 22),
                Font = new Font(Font, FontStyle.Bold)
            };
            Controls.Add(titleLabel);

            Label hintLabel = new Label
            {
                Text = "Use the path IDs below in the target equipment fields, or import the full structure to create a ready-to-edit item.",
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(680, 34)
            };
            Controls.Add(hintLabel);

            Label summaryLabel = new Label
            {
                Text = summary ?? string.Empty,
                AutoSize = false,
                Location = new Point(16, 76),
                Size = new Size(680, 72)
            };
            Controls.Add(summaryLabel);

            Label idsLabel = new Label
            {
                Text = "Model path IDs and package dependencies",
                AutoSize = false,
                Location = new Point(16, 154),
                Size = new Size(680, 20),
                Font = new Font(Font, FontStyle.Bold)
            };
            Controls.Add(idsLabel);

            summaryTextBox = new TextBox
            {
                Text = importDetails ?? string.Empty,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 178),
                Size = new Size(688, 196)
            };
            Controls.Add(summaryTextBox);

            copyButton = new Button
            {
                Text = "Copy",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(524, 390),
                Size = new Size(86, 26)
            };
            copyButton.Click += copyButton_Click;
            Controls.Add(copyButton);

            closeButton = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(618, 390),
                Size = new Size(86, 26)
            };
            Controls.Add(closeButton);

            AcceptButton = closeButton;
            CancelButton = closeButton;
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(summaryTextBox.Text ?? string.Empty);
                copyButton.Text = "Copied";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not copy to clipboard.\n" + ex.Message, "Clipboard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

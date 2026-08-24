using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemTransferImportModeWindow : Form
    {
        private readonly RadioButton fullStructureRadio;
        private readonly RadioButton modelsOnlyRadio;
        private readonly Button importButton;
        private readonly Button cancelButton;

        public ItemTransferImportMode SelectedMode { get; private set; }

        public ItemTransferImportModeWindow()
        {
            Text = "Import Item Package";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 210);
            Font = SystemFonts.MessageBoxFont;
            SelectedMode = ItemTransferImportMode.FullStructure;

            Label titleLabel = new Label
            {
                Text = "Choose how this item package should be imported.",
                AutoSize = false,
                Location = new Point(18, 16),
                Size = new Size(480, 24),
                Font = new Font(Font, FontStyle.Bold)
            };
            Controls.Add(titleLabel);

            fullStructureRadio = new RadioButton
            {
                Text = "Import full structure",
                Checked = true,
                Location = new Point(22, 54),
                Size = new Size(450, 22)
            };
            Controls.Add(fullStructureRadio);

            Label fullStructureDescription = new Label
            {
                Text = "Creates the item entry and imports all required assets.",
                AutoSize = false,
                Location = new Point(42, 78),
                Size = new Size(455, 22)
            };
            Controls.Add(fullStructureDescription);

            modelsOnlyRadio = new RadioButton
            {
                Text = "Import models only",
                Location = new Point(22, 112),
                Size = new Size(450, 22)
            };
            Controls.Add(modelsOnlyRadio);

            Label modelsOnlyDescription = new Label
            {
                Text = "Imports model assets and path IDs without creating a new item.",
                AutoSize = false,
                Location = new Point(42, 136),
                Size = new Size(455, 22)
            };
            Controls.Add(modelsOnlyDescription);

            importButton = new Button
            {
                Text = "Import",
                DialogResult = DialogResult.OK,
                Location = new Point(326, 172),
                Size = new Size(86, 26)
            };
            importButton.Click += importButton_Click;
            Controls.Add(importButton);

            cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(422, 172),
                Size = new Size(86, 26)
            };
            Controls.Add(cancelButton);

            AcceptButton = importButton;
            CancelButton = cancelButton;
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            SelectedMode = modelsOnlyRadio.Checked
                ? ItemTransferImportMode.ModelsOnly
                : ItemTransferImportMode.FullStructure;
        }
    }
}

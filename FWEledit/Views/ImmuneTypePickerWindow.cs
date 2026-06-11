using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ImmuneTypePickerWindow : Form
    {
        private readonly CheckedListBox checkedListBox;
        private readonly Label summaryLabel;
        private readonly TextBox descriptionTextBox;

        public uint SelectedValue { get; private set; }

        public ImmuneTypePickerWindow(uint currentValue)
        {
            SelectedValue = currentValue;

            Text = "Choose immunity flags...";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(420, 430);
            Size = new Size(520, 560);
            BackColor = Color.FromArgb(24, 24, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += HandleWindowKeyDown;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.BackColor = BackColor;

            summaryLabel = new Label();
            summaryLabel.Dock = DockStyle.Fill;
            summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            summaryLabel.ForeColor = Color.FromArgb(180, 205, 235);

            checkedListBox = new CheckedListBox();
            checkedListBox.Dock = DockStyle.Fill;
            checkedListBox.BorderStyle = BorderStyle.FixedSingle;
            checkedListBox.BackColor = Color.FromArgb(18, 20, 24);
            checkedListBox.ForeColor = Color.White;
            checkedListBox.CheckOnClick = true;
            checkedListBox.IntegralHeight = false;
            checkedListBox.HorizontalScrollbar = true;
            checkedListBox.SelectedIndexChanged += HandleSelectionChanged;
            checkedListBox.ItemCheck += HandleItemCheck;
            checkedListBox.DoubleClick += (s, e) => ConfirmSelection();

            descriptionTextBox = new TextBox();
            descriptionTextBox.Dock = DockStyle.Fill;
            descriptionTextBox.Multiline = true;
            descriptionTextBox.ReadOnly = true;
            descriptionTextBox.WordWrap = true;
            descriptionTextBox.ScrollBars = ScrollBars.Vertical;
            descriptionTextBox.BorderStyle = BorderStyle.FixedSingle;
            descriptionTextBox.BackColor = Color.FromArgb(18, 20, 24);
            descriptionTextBox.ForeColor = Color.FromArgb(215, 223, 232);

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.WrapContents = false;
            buttonPanel.BackColor = BackColor;

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Width = 96;
            okButton.Height = 30;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.BackColor = Color.FromArgb(48, 61, 82);
            okButton.ForeColor = Color.White;
            okButton.Click += (s, e) => ConfirmSelection();

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel";
            cancelButton.Width = 96;
            cancelButton.Height = 30;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.BackColor = Color.FromArgb(38, 42, 49);
            cancelButton.ForeColor = Color.White;
            cancelButton.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);

            root.Controls.Add(summaryLabel, 0, 0);
            root.Controls.Add(checkedListBox, 0, 1);
            root.Controls.Add(descriptionTextBox, 0, 2);
            root.Controls.Add(buttonPanel, 0, 3);
            Controls.Add(root);

            LoadOptions(currentValue);
            UpdateSelectionDetails();
            Shown += (s, e) => checkedListBox.Focus();
        }

        private void HandleWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
                e.SuppressKeyPress = true;
            }
        }

        private void LoadOptions(uint currentValue)
        {
            checkedListBox.Items.Clear();
            for (int i = 0; i < ImmuneTypeCatalog.Options.Count; i++)
            {
                ImmuneTypeOption option = ImmuneTypeCatalog.Options[i];
                if (option == null)
                {
                    continue;
                }

                int index = checkedListBox.Items.Add(option);
                checkedListBox.SetItemChecked(index, (currentValue & option.Value) == option.Value);
            }

            if (checkedListBox.Items.Count > 0)
            {
                checkedListBox.SelectedIndex = 0;
            }
        }

        private void HandleSelectionChanged(object sender, EventArgs e)
        {
            UpdateSelectionDetails();
        }

        private void HandleItemCheck(object sender, ItemCheckEventArgs e)
        {
            UpdateSelectionDetails(e.Index, e.NewValue);
        }

        private void UpdateSelectionDetails()
        {
            UpdateSelectionDetails(-1, CheckState.Indeterminate);
        }

        private void UpdateSelectionDetails(int pendingIndex, CheckState pendingState)
        {
            uint value = 0;
            for (int i = 0; i < checkedListBox.Items.Count; i++)
            {
                bool isChecked = checkedListBox.GetItemChecked(i);
                if (i == pendingIndex)
                {
                    isChecked = pendingState == CheckState.Checked;
                }

                if (!isChecked)
                {
                    continue;
                }

                ImmuneTypeOption option = checkedListBox.Items[i] as ImmuneTypeOption;
                if (option != null)
                {
                    value |= option.Value;
                }
            }

            SelectedValue = value;
            summaryLabel.Text = "Raw value: " + value.ToString(CultureInfo.InvariantCulture) + " (0x" + value.ToString("X4", CultureInfo.InvariantCulture) + ")";

            ImmuneTypeOption selected = checkedListBox.SelectedItem as ImmuneTypeOption;
            descriptionTextBox.Text = selected != null
                ? selected.Label + Environment.NewLine + Environment.NewLine + (selected.Description ?? string.Empty)
                : "Select one or more immunity flags.";
        }

        private void ConfirmSelection()
        {
            UpdateSelectionDetails();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

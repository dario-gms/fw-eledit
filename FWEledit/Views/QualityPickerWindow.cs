using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public class QualityPickerWindow : Form
    {
        private readonly List<QualityOption> options;
        private readonly ListBox listBox;
        public int SelectedValue { get; private set; }

        public QualityPickerWindow(List<QualityOption> options, int currentValue)
        {
            this.options = options ?? new List<QualityOption>();
            SelectedValue = currentValue;

            Text = "Choose value...";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(220, 260);
            Size = new Size(260, 340);
            BackColor = Color.FromArgb(24, 24, 24);
            Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                }
            };

            listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.BorderStyle = BorderStyle.FixedSingle;
            listBox.BackColor = Color.FromArgb(24, 24, 24);
            listBox.ForeColor = Color.White;
            listBox.DoubleClick += (s, e) => ConfirmSelection();
            listBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                    e.SuppressKeyPress = true;
                }
            };

            Controls.Add(listBox);

            LoadOptions();
            SelectValue(currentValue);
            Shown += (s, e) =>
            {
                listBox.Focus();
            };
        }

        private void LoadOptions()
        {
            listBox.BeginUpdate();
            listBox.Items.Clear();
            for (int i = 0; i < options.Count; i++)
            {
                listBox.Items.Add(options[i]);
            }
            listBox.EndUpdate();
        }

        private void SelectValue(int value)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                QualityOption option = listBox.Items[i] as QualityOption;
                if (option != null && option.Value == value)
                {
                    listBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void ConfirmSelection()
        {
            QualityOption option = listBox.SelectedItem as QualityOption;
            if (option == null)
            {
                return;
            }
            SelectedValue = option.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

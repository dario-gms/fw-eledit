using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public class AddonTypePickerWindow : Form
    {
        private readonly List<AddonTypeOption> allOptions;
        private readonly ListView listView;
        private readonly TextBox searchBox;
        public int SelectedType { get; private set; }

        public AddonTypePickerWindow(List<AddonTypeOption> options, int currentType)
        {
            allOptions = options ?? new List<AddonTypeOption>();
            SelectedType = currentType;

            Text = "Choose value...";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(520, 420);
            Size = new Size(760, 560);
            BackColor = Color.FromArgb(24, 24, 24);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
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

            Panel top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 36;
            top.Padding = new Padding(8, 6, 8, 6);

            searchBox = new TextBox();
            searchBox.Dock = DockStyle.Fill;
            searchBox.TextChanged += (s, e) => ApplyFilter();
            top.Controls.Add(searchBox);

            listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.HideSelection = false;
            listView.MultiSelect = false;
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView.BackColor = Color.FromArgb(24, 24, 24);
            listView.ForeColor = Color.White;
            listView.DoubleClick += (s, e) => ConfirmSelection();
            listView.Columns.Add("Type", 80);
            listView.Columns.Add("Description", 560);

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 44;
            bottom.Padding = new Padding(8, 6, 8, 6);

            Button cancelButton = new Button();
            cancelButton.Text = "Cancel [ESC]";
            cancelButton.Dock = DockStyle.Right;
            cancelButton.Width = 140;
            cancelButton.FlatStyle = FlatStyle.Flat;
            cancelButton.UseVisualStyleBackColor = false;
            cancelButton.BackColor = Color.FromArgb(56, 56, 56);
            cancelButton.ForeColor = Color.Gainsboro;
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(84, 84, 84);
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(cancelButton);

            Button okButton = new Button();
            okButton.Text = "OK [Enter]";
            okButton.Dock = DockStyle.Right;
            okButton.Width = 140;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.UseVisualStyleBackColor = false;
            okButton.BackColor = Color.FromArgb(56, 56, 56);
            okButton.ForeColor = Color.Gainsboro;
            okButton.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
            okButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 70, 70);
            okButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(84, 84, 84);
            okButton.Click += (s, e) => ConfirmSelection();
            bottom.Controls.Add(okButton);

            Controls.Add(listView);
            Controls.Add(bottom);
            Controls.Add(top);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            ApplyFilter();
            SelectType(currentType);
            Shown += (s, e) =>
            {
                BeginInvoke(new Action(() => SelectType(SelectedType)));
            };
        }

        private void ApplyFilter()
        {
            string term = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            listView.BeginUpdate();
            listView.Items.Clear();

            for (int i = 0; i < allOptions.Count; i++)
            {
                AddonTypeOption option = allOptions[i];
                if (!string.IsNullOrWhiteSpace(term))
                {
                    if (!option.TypeId.ToString().Contains(term)
                        && (option.Display == null || !option.Display.ToLowerInvariant().Contains(term)))
                    {
                        continue;
                    }
                }

                ListViewItem item = new ListViewItem(option.TypeId.ToString());
                item.SubItems.Add(option.Display ?? string.Empty);
                item.Tag = option;
                listView.Items.Add(item);
            }

            listView.EndUpdate();
        }

        private void SelectType(int typeId)
        {
            if (typeId < 0)
            {
                return;
            }
            for (int i = 0; i < listView.Items.Count; i++)
            {
                AddonTypeOption option = listView.Items[i].Tag as AddonTypeOption;
                if (option != null && option.TypeId == typeId)
                {
                    listView.Items[i].Selected = true;
                    listView.Items[i].Focused = true;
                    listView.EnsureVisible(i);
                    return;
                }
            }
        }

        private void ConfirmSelection()
        {
            if (listView.SelectedItems.Count == 0)
            {
                return;
            }
            AddonTypeOption option = listView.SelectedItems[0].Tag as AddonTypeOption;
            if (option == null)
            {
                return;
            }
            SelectedType = option.TypeId;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

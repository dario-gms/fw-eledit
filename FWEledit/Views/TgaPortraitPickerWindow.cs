using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class TgaPortraitPickerWindow : Form
    {
        private readonly List<TgaPortraitEntry> allEntries;
        private readonly TextBox searchBox;
        private readonly ListView listView;
        private readonly ImageList imageList;
        private readonly PictureBox previewBox;
        private readonly Label detailLabel;

        public int SelectedPathId { get; private set; }

        public TgaPortraitPickerWindow(List<TgaPortraitEntry> entries, int currentPathId)
        {
            allEntries = entries ?? new List<TgaPortraitEntry>();
            SelectedPathId = currentPathId;

            Text = "Choose TGA portrait...";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 520);
            Size = new Size(980, 680);
            BackColor = Color.FromArgb(18, 21, 26);
            ForeColor = Color.FromArgb(229, 234, 242);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += Window_KeyDown;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            root.Padding = new Padding(10);
            root.BackColor = BackColor;

            Panel left = new Panel();
            left.Dock = DockStyle.Fill;
            left.BackColor = BackColor;

            searchBox = new TextBox();
            searchBox.Dock = DockStyle.Top;
            searchBox.Height = 28;
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.BackColor = Color.FromArgb(24, 28, 34);
            searchBox.ForeColor = ForeColor;
            searchBox.TextChanged += (s, e) => ApplyFilter();

            imageList = new ImageList();
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.ImageSize = new Size(32, 32);

            listView = new ListView();
            listView.Dock = DockStyle.Fill;
            listView.View = View.Details;
            listView.FullRowSelect = true;
            listView.HideSelection = false;
            listView.MultiSelect = false;
            listView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listView.BackColor = Color.FromArgb(18, 21, 26);
            listView.ForeColor = ForeColor;
            listView.SmallImageList = imageList;
            listView.Columns.Add("ID", 72);
            listView.Columns.Add("Name", 220);
            listView.Columns.Add("Path", 360);
            listView.SelectedIndexChanged += (s, e) => UpdatePreview();
            listView.DoubleClick += (s, e) => ConfirmSelection();
            listView.KeyDown += ListView_KeyDown;

            left.Controls.Add(listView);
            left.Controls.Add(searchBox);

            Panel right = new Panel();
            right.Dock = DockStyle.Fill;
            right.Padding = new Padding(12, 0, 0, 0);
            right.BackColor = BackColor;

            previewBox = new PictureBox();
            previewBox.Dock = DockStyle.Fill;
            previewBox.BackColor = Color.Black;
            previewBox.BorderStyle = BorderStyle.FixedSingle;
            previewBox.SizeMode = PictureBoxSizeMode.Zoom;

            detailLabel = new Label();
            detailLabel.Dock = DockStyle.Top;
            detailLabel.Height = 72;
            detailLabel.ForeColor = Color.FromArgb(190, 202, 220);
            detailLabel.BackColor = Color.FromArgb(31, 35, 42);
            detailLabel.Padding = new Padding(10);
            detailLabel.TextAlign = ContentAlignment.MiddleLeft;

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 44;
            bottom.BackColor = BackColor;
            bottom.Padding = new Padding(0, 8, 0, 0);

            Button cancelButton = BuildButton("Cancel [ESC]");
            cancelButton.Dock = DockStyle.Right;
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Button okButton = BuildButton("OK [Enter]");
            okButton.Dock = DockStyle.Right;
            okButton.Click += (s, e) => ConfirmSelection();

            bottom.Controls.Add(cancelButton);
            bottom.Controls.Add(okButton);

            right.Controls.Add(previewBox);
            right.Controls.Add(bottom);
            right.Controls.Add(detailLabel);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);
            Controls.Add(root);

            AcceptButton = okButton;
            CancelButton = cancelButton;

            ApplyFilter();
            Shown += (s, e) =>
            {
                SelectPathId(currentPathId);
                searchBox.Focus();
            };
        }

        private static Button BuildButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 130;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.BackColor = Color.FromArgb(41, 48, 59);
            button.ForeColor = Color.FromArgb(229, 234, 242);
            button.FlatAppearance.BorderColor = Color.FromArgb(50, 58, 70);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 64, 78);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(68, 132, 184);
            return button;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
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

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                ConfirmSelection();
                e.SuppressKeyPress = true;
            }
        }

        private void ApplyFilter()
        {
            string term = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            listView.BeginUpdate();
            listView.Items.Clear();
            imageList.Images.Clear();

            for (int i = 0; i < allEntries.Count; i++)
            {
                TgaPortraitEntry entry = allEntries[i];
                if (!Matches(entry, term))
                {
                    continue;
                }

                int imageIndex = imageList.Images.Count;
                imageList.Images.Add(entry.Thumbnail ?? Properties.Resources.NoIcon);

                ListViewItem item = new ListViewItem(entry.PathId.ToString(), imageIndex);
                item.SubItems.Add(entry.Name ?? string.Empty);
                item.SubItems.Add(entry.Path ?? string.Empty);
                item.Tag = entry;
                listView.Items.Add(item);
            }

            listView.EndUpdate();

            if (listView.Items.Count > 0 && listView.SelectedItems.Count == 0)
            {
                listView.Items[0].Selected = true;
                listView.Items[0].Focused = true;
            }

            UpdatePreview();
        }

        private static bool Matches(TgaPortraitEntry entry, string term)
        {
            if (entry == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(term))
            {
                return true;
            }

            return entry.PathId.ToString().Contains(term)
                || ((entry.Name ?? string.Empty).ToLowerInvariant().Contains(term))
                || ((entry.Path ?? string.Empty).ToLowerInvariant().Contains(term));
        }

        private void SelectPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return;
            }

            for (int i = 0; i < listView.Items.Count; i++)
            {
                TgaPortraitEntry entry = listView.Items[i].Tag as TgaPortraitEntry;
                if (entry != null && entry.PathId == pathId)
                {
                    listView.Items[i].Selected = true;
                    listView.Items[i].Focused = true;
                    listView.EnsureVisible(i);
                    UpdatePreview();
                    return;
                }
            }
        }

        private void UpdatePreview()
        {
            TgaPortraitEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                previewBox.Image = null;
                detailLabel.Text = string.Empty;
                return;
            }

            previewBox.Image = entry.Thumbnail;
            detailLabel.Text = "ID: " + entry.PathId + Environment.NewLine + (entry.Path ?? string.Empty);
        }

        private TgaPortraitEntry GetSelectedEntry()
        {
            if (listView.SelectedItems.Count == 0)
            {
                return null;
            }

            return listView.SelectedItems[0].Tag as TgaPortraitEntry;
        }

        private void ConfirmSelection()
        {
            TgaPortraitEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                return;
            }

            SelectedPathId = entry.PathId;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

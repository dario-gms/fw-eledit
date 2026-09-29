using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class CustomIconPickerWindow : Form
    {
        private readonly List<CustomIconEntry> allEntries;
        private readonly TextBox searchBox;
        private readonly ListView listView;
        private readonly ImageList imageList;
        private readonly PictureBox previewBox;
        private readonly Label detailLabel;
        private readonly Label capacityLabel;
        private readonly Button expandButton;
        private readonly Button removeButton;
        private readonly Image itemFrame;
        private Image previewComposedImage;
        private readonly Func<IWin32Window, CustomIconEntry> importIcon;
        private readonly Func<CustomIconCapacityInfo> getCapacity;
        private readonly Func<IWin32Window, CustomIconCapacityInfo> expandCapacity;
        private readonly Func<IWin32Window, CustomIconEntry, bool> removeIcon;

        public int SelectedPathId { get; private set; }

        public CustomIconPickerWindow(
            List<CustomIconEntry> entries,
            int currentPathId,
            Func<IWin32Window, CustomIconEntry> importIcon,
            Func<CustomIconCapacityInfo> getCapacity,
            Func<IWin32Window, CustomIconCapacityInfo> expandCapacity,
            Func<IWin32Window, CustomIconEntry, bool> removeIcon,
            Image itemFrame)
        {
            allEntries = entries ?? new List<CustomIconEntry>();
            SelectedPathId = currentPathId;
            this.importIcon = importIcon;
            this.getCapacity = getCapacity;
            this.expandCapacity = expandCapacity;
            this.removeIcon = removeIcon;
            this.itemFrame = itemFrame;

            Text = "Custom Icons...";
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

            capacityLabel = new Label();
            capacityLabel.Dock = DockStyle.Top;
            capacityLabel.Height = 48;
            capacityLabel.ForeColor = Color.FromArgb(205, 217, 236);
            capacityLabel.BackColor = Color.FromArgb(24, 28, 34);
            capacityLabel.Padding = new Padding(10, 6, 10, 6);
            capacityLabel.TextAlign = ContentAlignment.MiddleLeft;

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 44;
            bottom.BackColor = BackColor;
            bottom.Padding = new Padding(0, 8, 0, 0);

            Button okButton = BuildButton("OK [Enter]");
            okButton.Width = 95;
            okButton.Dock = DockStyle.Right;
            okButton.Click += (s, e) => ConfirmSelection();

            Button importButton = BuildButton("Import");
            importButton.Width = 85;
            importButton.Dock = DockStyle.Left;
            importButton.Enabled = importIcon != null;
            importButton.Click += (s, e) => ImportIcon();

            expandButton = BuildButton("Expand");
            expandButton.Width = 90;
            expandButton.Dock = DockStyle.Left;
            expandButton.Enabled = expandCapacity != null;
            expandButton.Click += (s, e) => ExpandCapacity();

            removeButton = BuildButton("Remove");
            removeButton.Width = 85;
            removeButton.Dock = DockStyle.Left;
            removeButton.Enabled = false;
            removeButton.Click += (s, e) => RemoveIcon();

            bottom.Controls.Add(importButton);
            bottom.Controls.Add(expandButton);
            bottom.Controls.Add(removeButton);
            bottom.Controls.Add(okButton);

            right.Controls.Add(previewBox);
            right.Controls.Add(bottom);
            right.Controls.Add(capacityLabel);
            right.Controls.Add(detailLabel);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);
            Controls.Add(root);

            AcceptButton = okButton;

            ApplyFilter();
            RefreshCapacity();
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
            if (e.KeyCode == Keys.Enter)
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
                CustomIconEntry entry = allEntries[i];
                if (!Matches(entry, term))
                {
                    continue;
                }

                int imageIndex = imageList.Images.Count;
                imageList.Images.Add(CreateItemSlotPreview(entry.Thumbnail ?? Properties.Resources.NoIcon, imageList.ImageSize));

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

        private static bool Matches(CustomIconEntry entry, string term)
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

        private void ImportIcon()
        {
            if (importIcon == null)
            {
                return;
            }

            CustomIconEntry imported = importIcon(this);
            if (imported == null || imported.PathId <= 0)
            {
                return;
            }

            bool replaced = false;
            for (int i = 0; i < allEntries.Count; i++)
            {
                if (allEntries[i] != null && allEntries[i].PathId == imported.PathId)
                {
                    allEntries[i] = imported;
                    replaced = true;
                    break;
                }
            }

            if (!replaced)
            {
                allEntries.Add(imported);
            }

            SelectedPathId = imported.PathId;
            string currentTerm = (searchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            if (!Matches(imported, currentTerm))
            {
                searchBox.Text = string.Empty;
            }
            else
            {
                ApplyFilter();
            }
            SelectPathId(imported.PathId);
            RefreshCapacity();
        }

        private void ExpandCapacity()
        {
            if (expandCapacity == null)
            {
                return;
            }

            CustomIconCapacityInfo info = expandCapacity(this);
            RefreshCapacity(info);
        }

        private void RefreshCapacity()
        {
            CustomIconCapacityInfo info = null;
            if (getCapacity != null)
            {
                info = getCapacity();
            }
            RefreshCapacity(info);
        }

        private void RefreshCapacity(CustomIconCapacityInfo info)
        {
            if (info == null)
            {
                capacityLabel.Text = "Iconlist capacity unavailable.";
                expandButton.Enabled = false;
                return;
            }

            capacityLabel.Text =
                "Iconlist: " + info.UsedSlots + "/" + info.Capacity + " used, "
                + Math.Max(0, info.FreeSlots) + " free"
                + Environment.NewLine
                + "Maximum: " + info.MaxCapacity + " slots";
            expandButton.Enabled = expandCapacity != null && info.CanExpand;
        }

        private void SelectPathId(int pathId)
        {
            if (pathId <= 0)
            {
                return;
            }

            for (int i = 0; i < listView.Items.Count; i++)
            {
                CustomIconEntry entry = listView.Items[i].Tag as CustomIconEntry;
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
            CustomIconEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                SetPreviewImage(null);
                detailLabel.Text = string.Empty;
                removeButton.Enabled = false;
                return;
            }

            SetPreviewImage(CreateItemSlotPreview(entry.Thumbnail ?? Properties.Resources.NoIcon, new Size(128, 128)));
            detailLabel.Text = "ID: " + entry.PathId + Environment.NewLine + (entry.Path ?? string.Empty);
            removeButton.Enabled = removeIcon != null;
        }

        private Image CreateItemSlotPreview(Image icon, Size size)
        {
            int width = Math.Max(1, size.Width);
            int height = Math.Max(1, size.Height);
            Bitmap composed = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(composed))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                if (itemFrame != null)
                {
                    graphics.DrawImage(itemFrame, new Rectangle(0, 0, width, height));
                }
                else
                {
                    using (Brush brush = new SolidBrush(Color.FromArgb(13, 16, 20)))
                    {
                        graphics.FillRectangle(brush, 0, 0, width, height);
                    }
                }

                if (icon != null)
                {
                    int inset = Math.Max(2, width / 10);
                    Rectangle iconBounds = new Rectangle(inset, inset, Math.Max(1, width - (inset * 2)), Math.Max(1, height - (inset * 2)));
                    graphics.DrawImage(icon, iconBounds);
                }
            }

            return composed;
        }

        private void SetPreviewImage(Image image)
        {
            if (previewComposedImage != null)
            {
                previewComposedImage.Dispose();
                previewComposedImage = null;
            }

            previewComposedImage = image;
            previewBox.Image = previewComposedImage;
        }

        private void RemoveIcon()
        {
            if (removeIcon == null)
            {
                return;
            }

            CustomIconEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                return;
            }

            int selectedIndex = listView.SelectedIndices.Count > 0 ? listView.SelectedIndices[0] : -1;
            if (!removeIcon(this, entry))
            {
                return;
            }

            for (int i = allEntries.Count - 1; i >= 0; i--)
            {
                if (allEntries[i] != null && allEntries[i].PathId == entry.PathId)
                {
                    allEntries.RemoveAt(i);
                }
            }

            if (SelectedPathId == entry.PathId)
            {
                SelectedPathId = 0;
            }

            ApplyFilter();
            if (listView.Items.Count > 0)
            {
                int nextIndex = Math.Max(0, Math.Min(selectedIndex, listView.Items.Count - 1));
                listView.Items[nextIndex].Selected = true;
                listView.Items[nextIndex].Focused = true;
                listView.EnsureVisible(nextIndex);
            }
            RefreshCapacity();
        }

        private CustomIconEntry GetSelectedEntry()
        {
            if (listView.SelectedItems.Count == 0)
            {
                return null;
            }

            return listView.SelectedItems[0].Tag as CustomIconEntry;
        }

        private void ConfirmSelection()
        {
            CustomIconEntry entry = GetSelectedEntry();
            if (entry == null)
            {
                return;
            }

            SelectedPathId = entry.PathId;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (previewComposedImage != null)
                {
                    previewComposedImage.Dispose();
                    previewComposedImage = null;
                }
                if (itemFrame != null)
                {
                    itemFrame.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}

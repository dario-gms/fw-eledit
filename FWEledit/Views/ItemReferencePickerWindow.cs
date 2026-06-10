using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemReferencePickerWindow : Form
    {
        private const int AllListsIndex = -1;

        private readonly List<ItemReferenceOption> allOptions;
        private readonly CacheSave database;
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();
        private readonly ComboBox listComboBox;
        private readonly TextBox searchBox;
        private readonly ListBox listBox;
        private string searchText = string.Empty;

        public int SelectedId { get; private set; }
        public ItemReferenceOption SelectedOption { get; private set; }

        public ItemReferencePickerWindow(List<ItemReferenceOption> options, int currentId, int targetListIndex, CacheSave database, string title)
        {
            allOptions = options ?? new List<ItemReferenceOption>();
            this.database = database;
            SelectedId = currentId;

            Text = string.IsNullOrWhiteSpace(title) ? "Choose item..." : title;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(500, 460);
            Size = new Size(620, 660);
            BackColor = Color.FromArgb(17, 20, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += HandleWindowKeyDown;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(10);
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.BackColor = BackColor;

            TableLayoutPanel filterPanel = new TableLayoutPanel();
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.Margin = new Padding(0, 0, 0, 8);
            filterPanel.ColumnCount = 2;
            filterPanel.RowCount = 1;
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filterPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            filterPanel.BackColor = BackColor;

            listComboBox = new ComboBox();
            listComboBox.Dock = DockStyle.Fill;
            listComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            listComboBox.Margin = new Padding(0, 0, 8, 0);
            listComboBox.BackColor = Color.FromArgb(28, 32, 37);
            listComboBox.ForeColor = Color.White;
            listComboBox.FlatStyle = FlatStyle.Flat;
            listComboBox.SelectedIndexChanged += (s, e) => LoadOptions();

            searchBox = new TextBox();
            searchBox.Dock = DockStyle.Fill;
            searchBox.Margin = new Padding(0);
            searchBox.BackColor = Color.FromArgb(28, 32, 37);
            searchBox.ForeColor = Color.White;
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.TextChanged += (s, e) =>
            {
                searchText = searchBox.Text ?? string.Empty;
                LoadOptions();
            };

            filterPanel.Controls.Add(listComboBox, 0, 0);
            filterPanel.Controls.Add(searchBox, 1, 0);

            listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.BorderStyle = BorderStyle.FixedSingle;
            listBox.DrawMode = DrawMode.OwnerDrawFixed;
            listBox.ItemHeight = 42;
            listBox.IntegralHeight = false;
            listBox.BackColor = Color.FromArgb(15, 18, 22);
            listBox.ForeColor = Color.White;
            listBox.DrawItem += DrawItem;
            listBox.DoubleClick += (s, e) => ConfirmSelection();
            listBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                    e.SuppressKeyPress = true;
                }
            };

            layout.Controls.Add(filterPanel, 0, 0);
            layout.Controls.Add(listBox, 0, 1);
            Controls.Add(layout);

            PopulateListFilters(targetListIndex);
            LoadOptions();
            SelectValue(currentId);
            Shown += (s, e) =>
            {
                PositionNearOwnerLeft();
                searchBox.Focus();
            };
        }

        private void PopulateListFilters(int targetListIndex)
        {
            listComboBox.Items.Clear();
            listComboBox.Items.Add(new ListFilter(AllListsIndex, "All lists"));

            foreach (IGrouping<int, ItemReferenceOption> group in allOptions.GroupBy(option => option.ListIndex).OrderBy(group => group.Key))
            {
                ItemReferenceOption sample = group.FirstOrDefault();
                string label = sample != null && !string.IsNullOrWhiteSpace(sample.ListName) ? sample.ListName : "List " + group.Key.ToString();
                listComboBox.Items.Add(new ListFilter(group.Key, label));
            }

            for (int i = 0; i < listComboBox.Items.Count; i++)
            {
                ListFilter filter = listComboBox.Items[i] as ListFilter;
                if (filter != null && filter.ListIndex == targetListIndex)
                {
                    listComboBox.SelectedIndex = i;
                    return;
                }
            }

            listComboBox.SelectedIndex = listComboBox.Items.Count > 0 ? 0 : -1;
        }

        private void PositionNearOwnerLeft()
        {
            Rectangle bounds = Owner != null ? Owner.Bounds : Screen.FromControl(this).WorkingArea;
            Rectangle workingArea = Screen.FromRectangle(bounds).WorkingArea;
            int left = Math.Max(workingArea.Left, Math.Min(bounds.Left + 18, workingArea.Right - Width));
            int top = Math.Max(workingArea.Top, Math.Min(bounds.Top + 78, workingArea.Bottom - Height));
            Location = new Point(left, top);
        }

        private void HandleWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && !searchBox.Focused)
            {
                ConfirmSelection();
            }
        }

        private void LoadOptions()
        {
            if (listBox == null)
            {
                return;
            }

            ItemReferenceOption selected = listBox.SelectedItem as ItemReferenceOption;
            int selectedId = selected != null ? selected.Id : SelectedId;
            int selectedListIndex = GetSelectedListIndex();
            bool globalSearch = !string.IsNullOrWhiteSpace(searchText);

            listBox.BeginUpdate();
            listBox.Items.Clear();

            for (int i = 0; i < allOptions.Count; i++)
            {
                ItemReferenceOption option = allOptions[i];
                if ((!globalSearch && selectedListIndex != AllListsIndex && option.ListIndex != selectedListIndex)
                    || !Matches(option, searchText))
                {
                    continue;
                }

                listBox.Items.Add(option);
            }

            listBox.EndUpdate();
            SelectValue(selectedId);
            if (listBox.SelectedIndex < 0 && listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }
        }

        private int GetSelectedListIndex()
        {
            ListFilter filter = listComboBox != null ? listComboBox.SelectedItem as ListFilter : null;
            return filter != null ? filter.ListIndex : AllListsIndex;
        }

        private static bool Matches(ItemReferenceOption option, string query)
        {
            if (option == null)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            string q = query.Trim();
            return option.Id.ToString().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Name ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.ListName ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectValue(int id)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                ItemReferenceOption option = listBox.Items[i] as ItemReferenceOption;
                if (option != null && option.Id == id)
                {
                    listBox.SelectedIndex = i;
                    listBox.TopIndex = Math.Max(0, i - 6);
                    return;
                }
            }
        }

        private void ConfirmSelection()
        {
            ItemReferenceOption option = listBox.SelectedItem as ItemReferenceOption;
            if (option == null)
            {
                return;
            }

            SelectedOption = option;
            SelectedId = option.Id;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= listBox.Items.Count)
            {
                return;
            }

            ItemReferenceOption option = listBox.Items[e.Index] as ItemReferenceOption;
            if (option == null)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color backColor = selected ? Color.FromArgb(54, 48, 88) : listBox.BackColor;
            Color idColor = selected ? Color.FromArgb(215, 225, 238) : Color.FromArgb(130, 144, 163);
            Color textColor = ResolveQualityColor(option.Quality, selected);
            Color listColor = selected ? Color.FromArgb(190, 199, 211) : Color.FromArgb(108, 120, 136);

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            Rectangle iconBounds = new Rectangle(e.Bounds.Left + 7, e.Bounds.Top + 5, 32, 32);
            DrawIcon(e.Graphics, option, iconBounds);

            Rectangle idBounds = new Rectangle(e.Bounds.Left + 48, e.Bounds.Top + 4, 66, 16);
            Rectangle nameBounds = new Rectangle(e.Bounds.Left + 118, e.Bounds.Top + 4, e.Bounds.Width - 126, 18);
            Rectangle listBounds = new Rectangle(e.Bounds.Left + 118, e.Bounds.Top + 22, e.Bounds.Width - 126, 16);

            DrawHighlightedText(e.Graphics, option.Id.ToString(), idBounds, idColor, searchText, selected);
            DrawHighlightedText(e.Graphics, option.Name ?? string.Empty, nameBounds, textColor, searchText, selected);
            DrawHighlightedText(e.Graphics, option.ListName ?? string.Empty, listBounds, listColor, searchText, selected);
            e.DrawFocusRectangle();
        }

        private void DrawIcon(Graphics graphics, ItemReferenceOption option, Rectangle bounds)
        {
            Bitmap icon = Properties.Resources.NoIcon;
            if (database != null && !string.IsNullOrWhiteSpace(option.IconKey))
            {
                if (database.sourceBitmap != null && database.ContainsKey(option.IconKey))
                {
                    icon = database.images(option.IconKey);
                }
                else
                {
                    Bitmap portrait = portraitIconService.TryLoadPortraitThumbnail(option.IconKey, 32);
                    if (portrait != null)
                    {
                        icon = portrait;
                    }
                }
            }

            graphics.DrawImage(icon, bounds);
            using (Pen pen = new Pen(Color.FromArgb(62, 70, 80)))
            {
                graphics.DrawRectangle(pen, bounds);
            }
        }

        private static Color ResolveQualityColor(int quality, bool selected)
        {
            Color color;
            if (ItemQualityCatalog.TryGetColor(quality, out color))
            {
                if (quality == 1 && !selected)
                {
                    return Color.FromArgb(235, 238, 244);
                }
                return color;
            }
            return selected ? Color.White : Color.FromArgb(226, 231, 239);
        }

        private void DrawHighlightedText(Graphics graphics, string text, Rectangle bounds, Color color, string query, bool selected)
        {
            TextRenderer.DrawText(graphics, text, Font, bounds, color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
            {
                return;
            }

            int index = text.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return;
            }

            string before = text.Substring(0, index);
            string match = text.Substring(index, Math.Min(query.Trim().Length, text.Length - index));
            Size beforeSize = TextRenderer.MeasureText(graphics, before, Font, new Size(bounds.Width, bounds.Height), TextFormatFlags.NoPadding);
            Size matchSize = TextRenderer.MeasureText(graphics, match, Font, new Size(bounds.Width, bounds.Height), TextFormatFlags.NoPadding);
            Rectangle highlightBounds = new Rectangle(bounds.Left + beforeSize.Width, bounds.Top + 2, Math.Min(matchSize.Width + 2, bounds.Right - bounds.Left), bounds.Height - 4);

            using (SolidBrush brush = new SolidBrush(selected ? Color.FromArgb(255, 229, 128) : Color.FromArgb(120, 92, 16)))
            {
                graphics.FillRectangle(brush, highlightBounds);
            }

            TextRenderer.DrawText(graphics, text, Font, bounds, color, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private sealed class ListFilter
        {
            public ListFilter(int listIndex, string label)
            {
                ListIndex = listIndex;
                Label = label ?? string.Empty;
            }

            public int ListIndex { get; private set; }
            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}

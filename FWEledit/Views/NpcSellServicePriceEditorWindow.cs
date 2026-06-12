using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NpcSellServicePriceEditorWindow : Form
    {
        private readonly IList<NpcSellPageEditorPageModel> pages;
        private readonly CacheSave database;
        private readonly List<ItemReferenceOption> itemOptions;
        private readonly TabControl tabs;
        private readonly Dictionary<TabPage, DataGridView> gridsByPage = new Dictionary<TabPage, DataGridView>();

        public IList<NpcSellPageEditorPageModel> Pages
        {
            get { return pages; }
        }

        public NpcSellServicePriceEditorWindow(string serviceName, IList<NpcSellPageEditorPageModel> pages, int selectedPageIndex, CacheSave database, List<ItemReferenceOption> itemOptions)
        {
            this.pages = pages ?? new List<NpcSellPageEditorPageModel>();
            this.database = database;
            this.itemOptions = itemOptions ?? new List<ItemReferenceOption>();

            Text = string.IsNullOrWhiteSpace(serviceName)
                ? "Edit shop prices"
                : "Edit shop prices - " + serviceName;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 560);
            Size = new Size(980, 700);
            BackColor = Color.FromArgb(23, 26, 31);
            ForeColor = Color.FromArgb(229, 234, 242);
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += HandleWindowKeyDown;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            Label header = new Label();
            header.Dock = DockStyle.Fill;
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.ForeColor = Color.FromArgb(178, 192, 210);
            header.Text = "Adjust the prices for the items sold on each shop page.";

            tabs = new ThemedTabControl();
            tabs.Dock = DockStyle.Fill;

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel();
            buttonPanel.Dock = DockStyle.Fill;
            buttonPanel.FlowDirection = FlowDirection.RightToLeft;
            buttonPanel.WrapContents = false;

            Button okButton = CreateButton("OK", Color.FromArgb(48, 61, 82));
            okButton.Click += (s, e) =>
            {
                CommitActiveGrid();
                DialogResult = DialogResult.OK;
                Close();
            };

            Button cancelButton = CreateButton("Cancel", Color.FromArgb(38, 42, 49));
            cancelButton.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            buttonPanel.Controls.Add(okButton);
            buttonPanel.Controls.Add(cancelButton);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(tabs, 0, 1);
            root.Controls.Add(buttonPanel, 0, 2);
            Controls.Add(root);

            BuildTabs();
            if (tabs.TabPages.Count > 0)
            {
                tabs.SelectedIndex = Math.Max(0, Math.Min(selectedPageIndex, tabs.TabPages.Count - 1));
            }
        }

        private void HandleWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && !(ActiveControl is DataGridViewTextBoxEditingControl))
            {
                CommitActiveGrid();
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void BuildTabs()
        {
            tabs.TabPages.Clear();
            gridsByPage.Clear();

            for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                NpcSellPageEditorPageModel page = pages[pageIndex];
                TabPage tabPage = new TabPage(BuildTabTitle(page, pageIndex));

                TableLayoutPanel pageLayout = new TableLayoutPanel();
                pageLayout.Dock = DockStyle.Fill;
                pageLayout.RowCount = 2;
                pageLayout.ColumnCount = 1;
                pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
                pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                Label pageInfo = new Label();
                pageInfo.Dock = DockStyle.Fill;
                pageInfo.TextAlign = ContentAlignment.MiddleLeft;
                pageInfo.ForeColor = Color.FromArgb(178, 192, 210);
                pageInfo.Text = (page.Title ?? ("Page " + (pageIndex + 1).ToString(CultureInfo.InvariantCulture)))
                    + "    |    Currency: "
                    + NpcSellMoneyTypeCatalog.FormatDisplay(page.MoneyType.ToString(CultureInfo.InvariantCulture));

                DataGridView grid = CreateGrid();
                LoadPageRows(grid, page);

                pageLayout.Controls.Add(pageInfo, 0, 0);
                pageLayout.Controls.Add(grid, 0, 1);
                tabPage.Controls.Add(pageLayout);
                tabs.TabPages.Add(tabPage);
                gridsByPage[tabPage] = grid;
            }
        }

        private static string BuildTabTitle(NpcSellPageEditorPageModel page, int pageIndex)
        {
            string title = page != null ? page.Title : string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "Page " + (pageIndex + 1).ToString(CultureInfo.InvariantCulture);
            }

            return title;
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 38;
            grid.ColumnHeadersHeight = 30;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.EnableHeadersVisualStyles = false;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.BackgroundColor = Color.FromArgb(18, 21, 26);
            grid.GridColor = Color.FromArgb(48, 56, 68);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(38, 44, 52);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(229, 234, 242);
            grid.DefaultCellStyle.BackColor = Color.FromArgb(22, 26, 31);
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(226, 231, 239);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(62, 93, 126);
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.RowTemplate.DefaultCellStyle.BackColor = Color.FromArgb(22, 26, 31);
            grid.RowTemplate.DefaultCellStyle.ForeColor = Color.FromArgb(226, 231, 239);

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "slot",
                HeaderText = "#",
                Width = 42,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewImageColumn
            {
                Name = "icon",
                HeaderText = string.Empty,
                Width = 38,
                ReadOnly = true,
                ImageLayout = DataGridViewImageCellLayout.Zoom
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "item",
                HeaderText = "Item",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 240,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "itemId",
                HeaderText = "ID",
                Width = 72,
                ReadOnly = true
            });
            grid.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "currency",
                HeaderText = "Currency",
                Width = 210,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "price",
                HeaderText = "Price",
                Width = 110
            });

            grid.CellValidating += Grid_CellValidating;
            grid.CellEndEdit += Grid_CellEndEdit;
            grid.CellDoubleClick += Grid_CellDoubleClick;
            grid.CurrentCellDirtyStateChanged += Grid_CurrentCellDirtyStateChanged;
            return grid;
        }

        private void LoadPageRows(DataGridView grid, NpcSellPageEditorPageModel page)
        {
            grid.Rows.Clear();
            if (page == null || page.Entries == null)
            {
                return;
            }

            for (int i = 0; i < page.Entries.Count; i++)
            {
                NpcSellPageEditorEntryModel entry = page.Entries[i];
                int rowIndex = grid.Rows.Add(
                    (entry.SlotIndex + 1).ToString("00", CultureInfo.InvariantCulture),
                    ResolveIcon(entry.IconKey),
                    entry.ItemName,
                    entry.ItemId.ToString(CultureInfo.InvariantCulture),
                    entry.CurrencyLabel,
                    entry.Price.ToString(CultureInfo.InvariantCulture));
                DataGridViewRow row = grid.Rows[rowIndex];
                row.Tag = entry;
                row.Cells["item"].Style.ForeColor = ResolveQualityColor(entry.Quality);
                DataGridViewComboBoxCell currencyCell = row.Cells["currency"] as DataGridViewComboBoxCell;
                if (currencyCell != null)
                {
                    currencyCell.Items.Clear();
                    List<QualityOption> options = NpcSellPriceCatalog.BuildTierOptions(entry.MoneyType);
                    for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
                    {
                        currencyCell.Items.Add(options[optionIndex].Label);
                    }

                    currencyCell.Value = entry.CurrencyLabel;
                }
                if (entry.PriceFieldIndex < 0)
                {
                    row.Cells["price"].ReadOnly = true;
                    row.Cells["price"].Style.ForeColor = Color.FromArgb(198, 104, 104);
                    row.Cells["price"].Value = "Unavailable";
                }
            }
        }

        private Image ResolveIcon(string iconKey)
        {
            if (database != null
                && database.sourceBitmap != null
                && !string.IsNullOrWhiteSpace(iconKey)
                && database.ContainsKey(iconKey))
            {
                return database.images(iconKey);
            }

            return Properties.Resources.NoIcon;
        }

        private static Color ResolveQualityColor(int quality)
        {
            if (ItemQualityCatalog.TryGetColor(quality, out Color qualityColor))
            {
                return quality == 1 ? Color.FromArgb(226, 231, 239) : qualityColor;
            }

            return Color.FromArgb(226, 231, 239);
        }

        private void Grid_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (!string.Equals(grid.Columns[e.ColumnIndex].Name, "price", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            DataGridViewRow row = grid.Rows[e.RowIndex];
            NpcSellPageEditorEntryModel entry = row.Tag as NpcSellPageEditorEntryModel;
            if (entry == null || entry.PriceFieldIndex < 0)
            {
                return;
            }

            string text = Convert.ToString(e.FormattedValue)?.Trim() ?? string.Empty;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue) || parsedValue < 0)
            {
                e.Cancel = true;
                MessageBox.Show(this, "Use only whole numbers greater than or equal to zero for the shop price.", "Invalid shop price", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            DataGridViewRow row = grid.Rows[e.RowIndex];
            NpcSellPageEditorEntryModel entry = row.Tag as NpcSellPageEditorEntryModel;
            if (entry == null)
            {
                return;
            }

            string columnName = grid.Columns[e.ColumnIndex].Name;
            if (string.Equals(columnName, "price", StringComparison.OrdinalIgnoreCase))
            {
                if (entry.PriceFieldIndex < 0)
                {
                    return;
                }

                if (int.TryParse(Convert.ToString(row.Cells["price"].Value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedValue))
                {
                    entry.Price = parsedValue;
                }
                return;
            }

            if (string.Equals(columnName, "currency", StringComparison.OrdinalIgnoreCase))
            {
                string selectedLabel = Convert.ToString(row.Cells["currency"].Value) ?? string.Empty;
                int scale = NpcSellPriceCatalog.ParseTierScale(entry.MoneyType, selectedLabel, NpcSellPriceCatalog.DetectScale(entry.RawPrice));
                entry.RawPrice = NpcSellPriceCatalog.EncodeRawAmountWithScale(entry.Price, scale);
                entry.CurrencyLabel = NpcSellPriceCatalog.BuildCurrencyDisplay(entry.MoneyType, entry.RawPrice);
                row.Cells["currency"].Value = entry.CurrencyLabel;
            }
        }

        private void Grid_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || !grid.IsCurrentCellDirty)
            {
                return;
            }

            if (grid.CurrentCell is DataGridViewComboBoxCell)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            if (!string.Equals(grid.Columns[e.ColumnIndex].Name, "item", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(grid.Columns[e.ColumnIndex].Name, "itemId", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(grid.Columns[e.ColumnIndex].Name, "icon", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            DataGridViewRow row = grid.Rows[e.RowIndex];
            NpcSellPageEditorEntryModel entry = row.Tag as NpcSellPageEditorEntryModel;
            if (entry == null || itemOptions == null || itemOptions.Count == 0)
            {
                return;
            }

            using (ItemReferencePickerWindow picker = new ItemReferencePickerWindow(itemOptions, entry.ItemId, entry.ItemListIndex, database, "Choose shop item...", null))
            {
                if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedOption == null)
                {
                    return;
                }

                ItemReferenceOption option = picker.SelectedOption;
                entry.ItemId = option.Id;
                entry.ItemListIndex = option.ListIndex;
                entry.ItemElementIndex = option.ElementIndex;
                entry.ItemName = option.Name ?? string.Empty;
                entry.IconKey = option.IconKey ?? string.Empty;
                entry.Quality = option.Quality;
                entry.PriceFieldIndex = -1;
                row.Cells["icon"].Value = ResolveIcon(entry.IconKey);
                row.Cells["item"].Value = entry.ItemName;
                row.Cells["item"].Style.ForeColor = ResolveQualityColor(entry.Quality);
                row.Cells["itemId"].Value = entry.ItemId.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void CommitActiveGrid()
        {
            if (tabs.SelectedTab == null || !gridsByPage.TryGetValue(tabs.SelectedTab, out DataGridView grid))
            {
                return;
            }

            if (grid.IsCurrentCellInEditMode)
            {
                grid.EndEdit();
            }
        }

        private static Button CreateButton(string text, Color backColor)
        {
            return new Button
            {
                Text = text,
                Width = 96,
                Height = 30,
                Margin = new Padding(8, 4, 0, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = Color.White
            };
        }
    }
}

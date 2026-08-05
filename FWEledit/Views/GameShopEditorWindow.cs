using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GameShopEditorWindow : Form
    {
        private readonly GameShopDataService gameShopDataService;
        private readonly eListCollection listCollection;
        private readonly CacheSave database;
        private readonly ItemReferenceService itemReferenceService;
        private readonly IconResolutionService iconResolutionService;
        private readonly CreaturePortraitIconService portraitIconService;
        private readonly GameShopIconService gameShopIconService = new GameShopIconService();
        private readonly bool darkMode;

        private readonly Dictionary<int, ItemReferenceOption> itemOptionsById = new Dictionary<int, ItemReferenceOption>();
        private readonly Dictionary<int, Image> shopIconCache = new Dictionary<int, Image>();
        private readonly List<GameShopIconOption> shopIconOptions = new List<GameShopIconOption>();
        private readonly List<GameShopEntry> allEntries = new List<GameShopEntry>();
        private readonly List<GameShopEntry> visibleEntries = new List<GameShopEntry>();

        private TextBox pathTextBox;
        private TextBox searchTextBox;
        private DataGridView entriesGrid;
        private PictureBox itemPictureBox;
        private PictureBox shopIconPictureBox;
        private Label statusLabel;
        private NumericUpDown idInput;
        private TextBox nameInput;
        private NumericUpDown fileIconInput;
        private TextBox fileIconPathTextBox;
        private NumericUpDown itemIdInput;
        private TextBox itemNameTextBox;
        private NumericUpDown quantityInput;
        private NumericUpDown priceInput;
        private NumericUpDown internalPriceInput;
        private NumericUpDown giftIdInput;
        private NumericUpDown giftRateInput;
        private ComboBox currencyComboBox;
        private CheckBox hiddenCheckBox;
        private CheckBox consumeScoreCheckBox;
        private NumericUpDown expireDateInput;
        private NumericUpDown discountPriceInput;
        private NumericUpDown sellBeginInput;
        private NumericUpDown sellEndInput;
        private Button saveButton;
        private ToolTip editorToolTip;
        private bool suppressEditorEvents;
        private string elementsPath;

        public event EventHandler GameShopSaved;

        public GameShopEditorWindow(
            GameShopDataService gameShopDataService,
            string elementsPath,
            eListCollection listCollection,
            CacheSave database,
            ItemReferenceService itemReferenceService,
            IconResolutionService iconResolutionService,
            CreaturePortraitIconService portraitIconService,
            bool darkMode)
        {
            this.gameShopDataService = gameShopDataService ?? new GameShopDataService();
            this.elementsPath = elementsPath ?? string.Empty;
            this.listCollection = listCollection;
            this.database = database;
            this.itemReferenceService = itemReferenceService;
            this.iconResolutionService = iconResolutionService;
            this.portraitIconService = portraitIconService;
            this.darkMode = darkMode;

            Text = "FWEledit - GShop Editor";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1260, 720);
            Size = new Size(1280, 760);
            ShowInTaskbar = false;

            BuildLayout();
            ApplyTheme();
            LoadItemOptions();
            LoadGameShop();
        }

        public void SelectGameShopEntry(int gameShopId, int itemId)
        {
            if (entriesGrid == null || entriesGrid.IsDisposed)
            {
                return;
            }

            for (int i = 0; i < entriesGrid.Rows.Count; i++)
            {
                GameShopEntry entry = entriesGrid.Rows[i].Tag as GameShopEntry;
                if (entry == null)
                {
                    continue;
                }

                if ((gameShopId > 0 && entry.Id == gameShopId)
                    || (gameShopId <= 0 && itemId > 0 && entry.ItemId == itemId))
                {
                    entriesGrid.ClearSelection();
                    entriesGrid.Rows[i].Selected = true;
                    entriesGrid.CurrentCell = entriesGrid.Rows[i].Cells[0];
                    PopulateEditor(entry);
                    return;
                }
            }
        }

        private void BuildLayout()
        {
            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

            Panel top = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 7, 8, 5) };
            Label pathLabel = new Label { Text = "gshop.data:", AutoSize = true, Location = new Point(0, 10) };
            pathTextBox = new TextBox { Left = 78, Top = 4, Width = 760, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, ReadOnly = true };
            Button openButton = new Button { Text = "Open", Width = 78, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            openButton.Left = 850;
            openButton.Top = 3;
            openButton.Click += openButton_Click;
            saveButton = new Button { Text = "Save", Width = 78, Height = 24, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            saveButton.Left = 934;
            saveButton.Top = 3;
            saveButton.Click += saveButton_Click;
            top.Resize += (sender, e) =>
            {
                saveButton.Left = top.ClientSize.Width - saveButton.Width - 8;
                openButton.Left = saveButton.Left - openButton.Width - 6;
                pathTextBox.Width = Math.Max(160, openButton.Left - pathTextBox.Left - 8);
            };
            top.Controls.Add(pathLabel);
            top.Controls.Add(pathTextBox);
            top.Controls.Add(openButton);
            top.Controls.Add(saveButton);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.SplitterWidth = 5;
            split.HandleCreated += (sender, e) => SetGameShopSplitter(split);
            split.Resize += (sender, e) => SetGameShopSplitter(split);

            split.Panel1.Controls.Add(BuildListPanel());
            split.Panel2.Controls.Add(BuildEditorPanel());

            statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(8, 0, 8, 0) };

            root.Controls.Add(top, 0, 0);
            root.Controls.Add(split, 0, 1);
            root.Controls.Add(statusLabel, 0, 2);
            Controls.Add(root);
        }

        private Control BuildListPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.RowCount = 2;
            panel.ColumnCount = 1;
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Padding = new Padding(8, 8, 4, 8);

            searchTextBox = new TextBox { Dock = DockStyle.Fill };
            searchTextBox.TextChanged += (sender, e) => ApplySearch();
            panel.Controls.Add(searchTextBox, 0, 0);

            entriesGrid = new DataGridView();
            entriesGrid.Dock = DockStyle.Fill;
            entriesGrid.AllowUserToAddRows = false;
            entriesGrid.AllowUserToDeleteRows = false;
            entriesGrid.AllowUserToResizeRows = false;
            entriesGrid.ReadOnly = true;
            entriesGrid.RowHeadersVisible = false;
            entriesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            entriesGrid.MultiSelect = false;
            entriesGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            entriesGrid.RowTemplate.Height = 28;
            entriesGrid.ColumnHeadersHeight = 26;
            entriesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            entriesGrid.EnableHeadersVisualStyles = false;
            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "id", HeaderText = "ID", Width = 56 });
            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "itemId", HeaderText = "ItemID", Width = 72 });
            entriesGrid.Columns.Add(new DataGridViewImageColumn { Name = "icon", HeaderText = string.Empty, Width = 34, ImageLayout = DataGridViewImageCellLayout.Zoom });
            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "name", HeaderText = "Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 140 });
            entriesGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "price", HeaderText = "Price", Width = 70 });
            entriesGrid.SelectionChanged += entriesGrid_SelectionChanged;
            panel.Controls.Add(entriesGrid, 0, 1);

            return panel;
        }

        private Control BuildEditorPanel()
        {
            Panel scrollHost = new Panel();
            scrollHost.Dock = DockStyle.Fill;
            scrollHost.AutoScroll = true;
            scrollHost.Padding = new Padding(10, 8, 8, 8);

            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Top;
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.ColumnCount = 4;
            panel.RowCount = 11;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
            for (int i = 0; i < panel.RowCount; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 9 ? 88 : i == 10 ? 164 : 32));
            }

            idInput = AddNumber(panel, "ID", 0, 0, 0, int.MaxValue);
            nameInput = AddText(panel, "Name", 2, 0);
            fileIconInput = AddNumber(panel, "Icon", 0, 1, 0, int.MaxValue);
            fileIconInput.DoubleClick += (sender, e) => OpenShopIconPicker();
            fileIconInput.ValueChanged += (sender, e) => RefreshShopIconPreviewFromInput();
            fileIconPathTextBox = AddText(panel, "Icon path", 2, 1);
            fileIconPathTextBox.ReadOnly = true;
            fileIconPathTextBox.DoubleClick += (sender, e) => OpenShopIconPicker();
            itemIdInput = AddNumber(panel, "Item", 0, 2, 0, int.MaxValue);
            itemIdInput.DoubleClick += (sender, e) => OpenItemPicker(itemIdInput, "Choose GShop item...");
            itemIdInput.ValueChanged += (sender, e) => RefreshItemPreviewFromInput();
            itemNameTextBox = AddText(panel, "Item name", 2, 2);
            itemNameTextBox.ReadOnly = true;
            quantityInput = AddNumber(panel, "Amount", 0, 3, 0, int.MaxValue);
            priceInput = AddNumber(panel, "Price", 2, 3, 0, int.MaxValue);
            internalPriceInput = AddNumber(panel, "Raw price", 0, 4, 0, int.MaxValue);
            priceInput.ValueChanged += priceInput_ValueChanged;
            internalPriceInput.ValueChanged += internalPriceInput_ValueChanged;
            giftIdInput = AddNumber(panel, "Gift", 2, 4, 0, int.MaxValue);
            giftIdInput.DoubleClick += (sender, e) => OpenItemPicker(giftIdInput, "Choose gift item...");
            giftRateInput = AddNumber(panel, "Gift rate", 0, 5, -1000000, 1000000);
            giftRateInput.DecimalPlaces = 6;
            giftRateInput.Increment = 0.01M;
            currencyComboBox = AddCombo(panel, "Currency", 2, 5);
            PopulateCurrencySelector();
            expireDateInput = AddNumber(panel, "Duration", 0, 6, 0, int.MaxValue);
            discountPriceInput = AddNumber(panel, "Disc. price", 2, 6, -1, int.MaxValue);
            sellBeginInput = AddNumber(panel, "Sale from", 0, 7, 0, int.MaxValue);
            sellEndInput = AddNumber(panel, "Sale to", 2, 7, 0, int.MaxValue);

            hiddenCheckBox = new CheckBox { Text = "Disabled / hidden", Dock = DockStyle.Fill };
            consumeScoreCheckBox = new CheckBox { Text = "Consume score", Dock = DockStyle.Fill };
            panel.Controls.Add(hiddenCheckBox, 1, 8);
            panel.Controls.Add(consumeScoreCheckBox, 3, 8);

            itemPictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage, BorderStyle = BorderStyle.FixedSingle };
            shopIconPictureBox = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.CenterImage, BorderStyle = BorderStyle.FixedSingle };
            itemPictureBox.Cursor = Cursors.Hand;
            shopIconPictureBox.Cursor = Cursors.Hand;
            itemPictureBox.DoubleClick += (sender, e) => OpenItemPicker(itemIdInput, "Choose GShop item...");
            shopIconPictureBox.DoubleClick += (sender, e) => OpenShopIconPicker();
            panel.Controls.Add(new Label { Text = "Item", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 9);
            panel.Controls.Add(itemPictureBox, 1, 9);
            panel.Controls.Add(new Label { Text = "Shop icon", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 9);
            panel.Controls.Add(shopIconPictureBox, 3, 9);

            TextBox note = new TextBox();
            note.Multiline = true;
            note.ReadOnly = true;
            note.Dock = DockStyle.Fill;
            note.Text = "This editor updates the known FW gshop.data fields and preserves the rest of each binary record.";
            panel.Controls.Add(note, 0, 10);
            panel.SetColumnSpan(note, 4);

            scrollHost.Controls.Add(panel);

            editorToolTip = new ToolTip();
            editorToolTip.SetToolTip(fileIconInput, "Double-click to choose the shop icon.");
            editorToolTip.SetToolTip(fileIconPathTextBox, "Double-click to choose the shop icon.");
            editorToolTip.SetToolTip(shopIconPictureBox, "Double-click to choose the shop icon.");
            editorToolTip.SetToolTip(itemIdInput, "Double-click to choose an item.");
            editorToolTip.SetToolTip(itemPictureBox, "Double-click to choose the sold item.");
            editorToolTip.SetToolTip(giftIdInput, "Double-click to choose a gift item.");
            return scrollHost;
        }

        private static void SetGameShopSplitter(SplitContainer split)
        {
            if (split == null || split.ClientSize.Width <= 0)
            {
                return;
            }

            const int target = 560;
            const int minimum = 360;
            const int editorMinimum = 360;
            int maximum = split.ClientSize.Width - editorMinimum - split.SplitterWidth;
            if (maximum < minimum)
            {
                return;
            }

            int distance = Math.Max(minimum, Math.Min(target, maximum));
            if (distance <= 0 || split.SplitterDistance == distance || distance < minimum || distance > maximum)
            {
                return;
            }

            try
            {
                split.SplitterDistance = distance;
            }
            catch (InvalidOperationException)
            {
                // The splitter can briefly report stale bounds while WinForms is creating the window.
            }
        }

        private TextBox AddText(TableLayoutPanel panel, string label, int column, int row)
        {
            panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, column, row);
            TextBox input = new TextBox { Dock = DockStyle.Fill };
            panel.Controls.Add(input, column + 1, row);
            return input;
        }

        private NumericUpDown AddNumber(TableLayoutPanel panel, string label, int column, int row, int minimum, int maximum)
        {
            panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, column, row);
            NumericUpDown input = new NumericUpDown { Dock = DockStyle.Fill, Minimum = minimum, Maximum = maximum, ThousandsSeparator = true };
            panel.Controls.Add(input, column + 1, row);
            return input;
        }

        private ComboBox AddCombo(TableLayoutPanel panel, string label, int column, int row)
        {
            panel.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, column, row);
            ComboBox input = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat
            };
            panel.Controls.Add(input, column + 1, row);
            return input;
        }

        private void PopulateCurrencySelector()
        {
            if (currencyComboBox == null)
            {
                return;
            }

            currencyComboBox.Items.Clear();
            currencyComboBox.Items.Add(new CurrencyOption(1u, "Eyrda Leaf"));
            currencyComboBox.Items.Add(new CurrencyOption(8u, "Soul Leaf"));
            currencyComboBox.Items.Add(new CurrencyOption(9u, "All Leaves"));
            currencyComboBox.SelectedIndex = 0;
        }

        private void SelectCurrencyOption(uint value)
        {
            if (currencyComboBox == null)
            {
                return;
            }

            for (int i = 0; i < currencyComboBox.Items.Count; i++)
            {
                CurrencyOption option = currencyComboBox.Items[i] as CurrencyOption;
                if (option != null && option.Value == value)
                {
                    currencyComboBox.SelectedIndex = i;
                    return;
                }
            }

            CurrencyOption custom = new CurrencyOption(value, "Custom (" + value.ToString() + ")");
            currencyComboBox.Items.Add(custom);
            currencyComboBox.SelectedItem = custom;
        }

        private uint GetSelectedCurrencyValue()
        {
            CurrencyOption option = currencyComboBox != null ? currencyComboBox.SelectedItem as CurrencyOption : null;
            return option != null ? option.Value : 1u;
        }

        private void ApplyTheme()
        {
            Color surface = darkMode ? Color.FromArgb(18, 21, 26) : Color.FromArgb(238, 241, 245);
            Color raised = darkMode ? Color.FromArgb(31, 35, 42) : Color.White;
            Color text = darkMode ? Color.FromArgb(229, 234, 242) : Color.FromArgb(29, 36, 45);
            Color grid = darkMode ? Color.FromArgb(38, 44, 54) : Color.FromArgb(224, 230, 237);

            BackColor = surface;
            ForeColor = text;
            ApplyThemeRecursive(this, surface, raised, text);
            entriesGrid.BackgroundColor = surface;
            entriesGrid.GridColor = grid;
            entriesGrid.DefaultCellStyle.BackColor = darkMode ? Color.FromArgb(18, 21, 26) : Color.White;
            entriesGrid.DefaultCellStyle.ForeColor = text;
            entriesGrid.DefaultCellStyle.SelectionBackColor = darkMode ? Color.FromArgb(47, 76, 112) : Color.FromArgb(84, 137, 196);
            entriesGrid.DefaultCellStyle.SelectionForeColor = Color.White;
            entriesGrid.AlternatingRowsDefaultCellStyle.BackColor = darkMode ? Color.FromArgb(22, 26, 32) : Color.FromArgb(250, 251, 253);
            entriesGrid.ColumnHeadersDefaultCellStyle.BackColor = darkMode ? Color.FromArgb(38, 44, 53) : Color.FromArgb(225, 231, 238);
            entriesGrid.ColumnHeadersDefaultCellStyle.ForeColor = text;
        }

        private static void ApplyThemeRecursive(Control control, Color surface, Color raised, Color text)
        {
            if (control is TextBox || control is NumericUpDown || control is ComboBox)
            {
                control.BackColor = raised;
                control.ForeColor = text;
            }
            else if (!(control is Button))
            {
                control.BackColor = surface;
                control.ForeColor = text;
            }

            for (int i = 0; i < control.Controls.Count; i++)
            {
                ApplyThemeRecursive(control.Controls[i], surface, raised, text);
            }
        }

        private void LoadItemOptions()
        {
            itemOptionsById.Clear();
            if (itemReferenceService == null || listCollection == null)
            {
                return;
            }

            List<ItemReferenceOption> options = itemReferenceService.BuildSearchableItemOptions(listCollection, database, iconResolutionService);
            for (int i = 0; i < options.Count; i++)
            {
                ItemReferenceOption option = options[i];
                if (option != null && !itemOptionsById.ContainsKey(option.Id))
                {
                    itemOptionsById[option.Id] = option;
                }
            }
        }

        private void LoadGameShop()
        {
            allEntries.Clear();
            allEntries.AddRange(gameShopDataService.LoadEntries(elementsPath));
            shopIconOptions.Clear();
            shopIconOptions.AddRange(gameShopIconService.LoadOptions(database, elementsPath));
            pathTextBox.Text = GameShopDataService.ResolvePath(elementsPath);
            ApplySearch();
            statusLabel.Text = "Total items count: " + allEntries.Count.ToString();
        }

        private void ApplySearch()
        {
            string query = searchTextBox != null ? (searchTextBox.Text ?? string.Empty).Trim() : string.Empty;
            visibleEntries.Clear();
            for (int i = 0; i < allEntries.Count; i++)
            {
                GameShopEntry entry = allEntries[i];
                if (entry == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(query)
                    || entry.Id.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || entry.ItemId.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || (entry.Name ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || ResolveItemName(entry.ItemId).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    visibleEntries.Add(entry);
                }
            }

            PopulateGrid();
        }

        private void PopulateGrid()
        {
            if (entriesGrid == null)
            {
                return;
            }

            entriesGrid.SuspendLayout();
            try
            {
                entriesGrid.Rows.Clear();
                for (int i = 0; i < visibleEntries.Count; i++)
                {
                    GameShopEntry entry = visibleEntries[i];
                    int rowIndex = entriesGrid.Rows.Add(
                        entry.Id.ToString(),
                        entry.ItemId.ToString(),
                        ResolveItemIcon(entry.ItemId),
                        entry.Name ?? string.Empty,
                        FormatNumber(entry.Price));
                    DataGridViewRow row = entriesGrid.Rows[rowIndex];
                    row.Tag = entry;
                    ItemReferenceOption option;
                    if (itemOptionsById.TryGetValue(entry.ItemId, out option) && option != null)
                    {
                        Color? color = ResolveQualityColor(option.Quality);
                        if (color.HasValue)
                        {
                            row.Cells[3].Style.ForeColor = color.Value;
                            row.Cells[3].Style.SelectionForeColor = color.Value;
                        }
                    }
                    row.Cells[4].Style.ForeColor = Color.LimeGreen;
                    row.Cells[4].Style.SelectionForeColor = Color.LimeGreen;
                }
            }
            finally
            {
                entriesGrid.ResumeLayout();
            }
        }

        private void entriesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (entriesGrid.CurrentRow == null)
            {
                return;
            }

            PopulateEditor(entriesGrid.CurrentRow.Tag as GameShopEntry);
        }

        private void PopulateEditor(GameShopEntry entry)
        {
            suppressEditorEvents = true;
            try
            {
                if (entry == null)
                {
                    return;
                }

                idInput.Value = Clamp(entry.Id, idInput);
                nameInput.Text = entry.Name ?? string.Empty;
                fileIconInput.Value = Clamp(entry.FileIcon, fileIconInput);
                fileIconPathTextBox.Text = ResolveIconPathDisplay(entry.FileIcon);
                itemIdInput.Value = Clamp(entry.ItemId, itemIdInput);
                itemNameTextBox.Text = ResolveItemName(entry.ItemId);
                quantityInput.Value = Clamp(entry.Quantity, quantityInput);
                priceInput.Value = Clamp(entry.Price, priceInput);
                internalPriceInput.Value = Clamp(entry.InternalPrice, internalPriceInput);
                giftIdInput.Value = Clamp(entry.GiftId, giftIdInput);
                giftRateInput.Value = Clamp((decimal)entry.GiftRate, giftRateInput);
                SelectCurrencyOption(entry.BuyTypeMask);
                expireDateInput.Value = Clamp((decimal)entry.ExpireDate, expireDateInput);
                discountPriceInput.Value = Clamp(entry.DiscountPrice, discountPriceInput);
                sellBeginInput.Value = Clamp((decimal)entry.SellBeginTime, sellBeginInput);
                sellEndInput.Value = Clamp((decimal)entry.SellEndTime, sellEndInput);
                hiddenCheckBox.Checked = entry.IsHidden != 0;
                consumeScoreCheckBox.Checked = entry.CanGetConsumeScore != 0;
                itemPictureBox.Image = ResolveItemIcon(entry.ItemId);
                shopIconPictureBox.Image = ResolveShopIcon(entry.FileIcon);
            }
            finally
            {
                suppressEditorEvents = false;
            }
        }

        private void ApplyEditorToEntry(GameShopEntry entry)
        {
            if (entry == null || suppressEditorEvents)
            {
                return;
            }

            entry.Id = (int)idInput.Value;
            entry.Name = nameInput.Text ?? string.Empty;
            entry.FileIcon = (int)fileIconInput.Value;
            entry.ItemId = (int)itemIdInput.Value;
            entry.Quantity = (int)quantityInput.Value;
            entry.Price = (int)priceInput.Value;
            entry.InternalPrice = (int)internalPriceInput.Value;
            entry.GiftId = (int)giftIdInput.Value;
            entry.GiftRate = (float)giftRateInput.Value;
            entry.BuyTypeMask = GetSelectedCurrencyValue();
            entry.ExpireDate = (uint)expireDateInput.Value;
            entry.DiscountPrice = (int)discountPriceInput.Value;
            entry.SellBeginTime = (uint)sellBeginInput.Value;
            entry.SellEndTime = (uint)sellEndInput.Value;
            entry.IsHidden = hiddenCheckBox.Checked ? 1u : 0u;
            entry.CanGetConsumeScore = consumeScoreCheckBox.Checked ? 1u : 0u;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            GameShopEntry entry = entriesGrid.CurrentRow != null ? entriesGrid.CurrentRow.Tag as GameShopEntry : null;
            if (entry == null)
            {
                return;
            }

            ApplyEditorToEntry(entry);
            string error;
            if (!gameShopDataService.SaveEntry(elementsPath, entry, out error))
            {
                MessageBox.Show(this, error, "GShop Editor", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoadGameShop();
            SelectGameShopEntry(entry.Id, entry.ItemId);
            statusLabel.Text = "Saved " + entry.Name + " [" + entry.Id.ToString() + "]";
            EventHandler handler = GameShopSaved;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void priceInput_ValueChanged(object sender, EventArgs e)
        {
            if (suppressEditorEvents || internalPriceInput == null)
            {
                return;
            }

            suppressEditorEvents = true;
            try
            {
                internalPriceInput.Value = Clamp((int)Math.Ceiling((double)priceInput.Value * 2.5d), internalPriceInput);
            }
            finally
            {
                suppressEditorEvents = false;
            }
        }

        private void internalPriceInput_ValueChanged(object sender, EventArgs e)
        {
            if (suppressEditorEvents || priceInput == null)
            {
                return;
            }

            suppressEditorEvents = true;
            try
            {
                priceInput.Value = Clamp((int)Math.Floor((double)internalPriceInput.Value * 0.4d), priceInput);
            }
            finally
            {
                suppressEditorEvents = false;
            }
        }

        private void RefreshShopIconPreviewFromInput()
        {
            if (suppressEditorEvents)
            {
                return;
            }

            int fileIcon = (int)fileIconInput.Value;
            fileIconPathTextBox.Text = ResolveIconPathDisplay(fileIcon);
            shopIconPictureBox.Image = ResolveShopIcon(fileIcon);
        }

        private void RefreshItemPreviewFromInput()
        {
            if (suppressEditorEvents)
            {
                return;
            }

            int itemId = (int)itemIdInput.Value;
            itemNameTextBox.Text = ResolveItemName(itemId);
            itemPictureBox.Image = ResolveItemIcon(itemId);
        }

        private void OpenShopIconPicker()
        {
            if (database == null)
            {
                return;
            }

            if (shopIconOptions.Count == 0)
            {
                shopIconOptions.AddRange(gameShopIconService.LoadOptions(database, elementsPath));
            }

            using (GameShopIconPickerWindow picker = new GameShopIconPickerWindow(shopIconOptions, (int)fileIconInput.Value, darkMode))
            {
                if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedPathId <= 0)
                {
                    return;
                }

                fileIconInput.Value = Clamp(picker.SelectedPathId, fileIconInput);
                RefreshShopIconPreviewFromInput();
            }
        }

        private void OpenItemPicker(NumericUpDown target, string title)
        {
            if (target == null || itemOptionsById.Count == 0)
            {
                return;
            }

            List<ItemReferenceOption> options = new List<ItemReferenceOption>(itemOptionsById.Values);
            using (ItemReferencePickerWindow picker = new ItemReferencePickerWindow(options, (int)target.Value, -1, database, title, null))
            {
                if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedId <= 0)
                {
                    return;
                }

                target.Value = Clamp(picker.SelectedId, target);
                if (target == itemIdInput)
                {
                    RefreshItemPreviewFromInput();
                }
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "gshop.data|gshop.data|Data files (*.data)|*.data|All files (*.*)|*.*";
                dialog.FileName = "gshop.data";
                string current = GameShopDataService.ResolvePath(elementsPath);
                if (!string.IsNullOrWhiteSpace(current))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(current);
                }

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                elementsPath = Path.Combine(Path.GetDirectoryName(dialog.FileName), "elements.data");
                if (!File.Exists(elementsPath))
                {
                    elementsPath = dialog.FileName;
                }

                gameShopDataService.ClearCache();
                LoadGameShop();
            }
        }

        private string ResolveItemName(int itemId)
        {
            ItemReferenceOption option;
            return itemOptionsById.TryGetValue(itemId, out option) && option != null
                ? option.Name ?? string.Empty
                : string.Empty;
        }

        private Image ResolveItemIcon(int itemId)
        {
            ItemReferenceOption option;
            if (itemOptionsById.TryGetValue(itemId, out option) && option != null)
            {
                Image icon = LoadOptionIcon(option);
                if (icon != null)
                {
                    return icon;
                }
            }

            return Properties.Resources.NoIcon;
        }

        private Image ResolveShopIcon(int fileIcon)
        {
            if (database == null)
            {
                return Properties.Resources.NoIcon;
            }

            Image cached;
            if (shopIconCache.TryGetValue(fileIcon, out cached) && cached != null)
            {
                return cached;
            }

            string mappedPath = gameShopIconService.ResolveMappedPath(database, fileIcon);
            Image direct = gameShopIconService.TryLoadIcon(database, elementsPath, fileIcon, mappedPath);
            if (direct != null)
            {
                shopIconCache[fileIcon] = direct;
                return direct;
            }

            return Properties.Resources.NoIcon;
        }

        private string ResolveIconPathDisplay(int fileIcon)
        {
            return iconResolutionService != null
                ? iconResolutionService.FormatIconPathIdDisplay(database, fileIcon.ToString())
                : fileIcon.ToString();
        }

        private Image LoadOptionIcon(ItemReferenceOption option)
        {
            if (option == null || database == null || string.IsNullOrWhiteSpace(option.IconKey))
            {
                return null;
            }

            if (database.ContainsKey(option.IconKey))
            {
                return database.images(option.IconKey);
            }

            return portraitIconService != null ? portraitIconService.TryLoadPortraitThumbnail(option.IconKey, 32) : null;
        }

        private Color? ResolveQualityColor(int quality)
        {
            switch (quality)
            {
                case 1: return Color.White;
                case 2: return Color.LimeGreen;
                case 3: return Color.FromArgb(72, 145, 255);
                case 4: return Color.FromArgb(185, 64, 255);
                case 5: return Color.Gold;
                default: return null;
            }
        }

        private static decimal Clamp(decimal value, NumericUpDown input)
        {
            if (value < input.Minimum)
            {
                return input.Minimum;
            }
            if (value > input.Maximum)
            {
                return input.Maximum;
            }
            return value;
        }

        private static string FormatNumber(int value)
        {
            return value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", ".");
        }

        private sealed class CurrencyOption
        {
            public CurrencyOption(uint value, string label)
            {
                Value = value;
                Label = label ?? string.Empty;
            }

            public uint Value { get; private set; }

            public string Label { get; private set; }

            public override string ToString()
            {
                return Label;
            }
        }
    }
}

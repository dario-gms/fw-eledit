using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ItemReferencePickerWindow : Form
    {
        private const int AllListsIndex = -1;

        private readonly List<ItemReferenceOption> allOptions;
        private readonly CacheSave database;
        private readonly AssetManager assetManager;
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();
        private readonly ComboBox listComboBox;
        private readonly TextBox searchBox;
        private readonly ListBox listBox;
        private readonly TextBox detailsBox;
        private readonly bool isTitlePicker;
        private readonly TabControl bottomTabs;
        private readonly Button applyTitleButton;
        private TextBox titleNameTextBox;
        private TextBox titleColorTextBox;
        private TextBox titleIconPathTextBox;
        private TextBox titleDescriptionTextBox;
        private readonly TextBox[] titleAddonTextBoxes;
        private Button saveTitleButton;
        private Label titlePreviewLabel;
        private bool suppressTitleEditorUpdates;
        private bool suppressTitleSelectionGuard;
        private bool titleSaveInProgress;
        private bool titleEditorInitialized;
        private int lastSelectedTitleId;
        private string searchText = string.Empty;

        public int SelectedId { get; private set; }
        public ItemReferenceOption SelectedOption { get; private set; }

        public ItemReferencePickerWindow(List<ItemReferenceOption> options, int currentId, int targetListIndex, CacheSave database, string title, AssetManager assetManager)
        {
            allOptions = options ?? new List<ItemReferenceOption>();
            this.database = database;
            this.assetManager = assetManager;
            isTitlePicker = targetListIndex == TitleDefinitionCatalog.TargetListIndex;
            SelectedId = currentId;

            Text = string.IsNullOrWhiteSpace(title) ? "Choose item..." : title;
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(500, 460);
            Size = isTitlePicker ? new Size(760, 760) : new Size(620, 660);
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
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, isTitlePicker ? 290F : 132F));
            layout.BackColor = BackColor;

            TableLayoutPanel filterPanel = new TableLayoutPanel();
            filterPanel.Dock = DockStyle.Fill;
            filterPanel.Margin = new Padding(0, 0, 0, 8);
            filterPanel.ColumnCount = isTitlePicker ? 3 : 2;
            filterPanel.RowCount = 1;
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            if (isTitlePicker)
            {
                filterPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            }
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

            applyTitleButton = new Button();
            applyTitleButton.Dock = DockStyle.Fill;
            applyTitleButton.Margin = new Padding(8, 0, 0, 0);
            applyTitleButton.Text = "Apply";
            applyTitleButton.Visible = isTitlePicker;
            applyTitleButton.FlatStyle = FlatStyle.Flat;
            applyTitleButton.BackColor = Color.FromArgb(44, 54, 72);
            applyTitleButton.ForeColor = Color.White;
            applyTitleButton.Click += HandleSaveTitleClick;

            filterPanel.Controls.Add(listComboBox, 0, 0);
            filterPanel.Controls.Add(searchBox, 1, 0);
            if (isTitlePicker)
            {
                filterPanel.Controls.Add(applyTitleButton, 2, 0);
            }

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
            listBox.SelectedIndexChanged += HandleListBoxSelectedIndexChanged;
            listBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                    e.SuppressKeyPress = true;
                }
            };

            detailsBox = new TextBox();
            detailsBox.Dock = DockStyle.Fill;
            detailsBox.Margin = new Padding(0, 8, 0, 0);
            detailsBox.Multiline = true;
            detailsBox.ReadOnly = true;
            detailsBox.ScrollBars = ScrollBars.Vertical;
            detailsBox.BorderStyle = BorderStyle.FixedSingle;
            detailsBox.BackColor = Color.FromArgb(15, 18, 22);
            detailsBox.ForeColor = Color.FromArgb(222, 229, 238);
            bottomTabs = new TabControl();
            bottomTabs.Dock = DockStyle.Fill;
            bottomTabs.Margin = new Padding(0, 8, 0, 0);

            TabPage detailsPage = new TabPage("Details");
            detailsPage.BackColor = BackColor;
            detailsPage.ForeColor = ForeColor;
            detailsPage.Controls.Add(detailsBox);
            bottomTabs.TabPages.Add(detailsPage);

            titleAddonTextBoxes = new TextBox[5];
            if (isTitlePicker)
            {
                TabPage editorPage = new TabPage("Editor");
                editorPage.BackColor = BackColor;
                editorPage.ForeColor = ForeColor;

                TableLayoutPanel editorLayout = new TableLayoutPanel();
                editorLayout.Dock = DockStyle.Fill;
                editorLayout.Margin = new Padding(0);
                editorLayout.Padding = new Padding(8);
                editorLayout.ColumnCount = 2;
                editorLayout.RowCount = 10;
                editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
                editorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                for (int rowIndex = 0; rowIndex < 9; rowIndex++)
                {
                    editorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, rowIndex == 3 ? 72F : 28F));
                }
                editorLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

                titleNameTextBox = CreateEditorTextBox();
                titleColorTextBox = CreateEditorTextBox();
                titleIconPathTextBox = CreateEditorTextBox();
                titleIconPathTextBox.ReadOnly = true;
                titleDescriptionTextBox = CreateEditorTextBox(true);
                titleDescriptionTextBox.Height = 68;
                for (int i = 0; i < titleAddonTextBoxes.Length; i++)
                {
                    titleAddonTextBoxes[i] = CreateEditorTextBox();
                }

                titlePreviewLabel = new Label();
                titlePreviewLabel.Dock = DockStyle.Fill;
                titlePreviewLabel.TextAlign = ContentAlignment.MiddleLeft;
                titlePreviewLabel.ForeColor = Color.FromArgb(226, 231, 239);
                titlePreviewLabel.Text = "Title preview";

                saveTitleButton = new Button();
                saveTitleButton.Dock = DockStyle.Right;
                saveTitleButton.Width = 110;
                saveTitleButton.Text = "Save title";
                saveTitleButton.FlatStyle = FlatStyle.Flat;
                saveTitleButton.BackColor = Color.FromArgb(44, 54, 72);
                saveTitleButton.ForeColor = Color.White;
                saveTitleButton.Click += HandleSaveTitleClick;

                editorLayout.Controls.Add(CreateEditorLabel("Name"), 0, 0);
                editorLayout.Controls.Add(titleNameTextBox, 1, 0);
                editorLayout.Controls.Add(CreateEditorLabel("Color"), 0, 1);
                editorLayout.Controls.Add(titleColorTextBox, 1, 1);
                editorLayout.Controls.Add(CreateEditorLabel("Graphic"), 0, 2);
                editorLayout.Controls.Add(titleIconPathTextBox, 1, 2);
                editorLayout.Controls.Add(CreateEditorLabel("Description"), 0, 3);
                editorLayout.Controls.Add(titleDescriptionTextBox, 1, 3);
                for (int i = 0; i < titleAddonTextBoxes.Length; i++)
                {
                    editorLayout.Controls.Add(CreateEditorLabel("Bonus " + (i + 1).ToString()), 0, 4 + i);
                    editorLayout.Controls.Add(titleAddonTextBoxes[i], 1, 4 + i);
                }

                Panel footerPanel = new Panel();
                footerPanel.Dock = DockStyle.Fill;
                footerPanel.BackColor = BackColor;
                titlePreviewLabel.Dock = DockStyle.Fill;
                footerPanel.Controls.Add(titlePreviewLabel);
                footerPanel.Controls.Add(saveTitleButton);
                editorLayout.Controls.Add(footerPanel, 0, 9);
                editorLayout.SetColumnSpan(footerPanel, 2);

                editorPage.Controls.Add(editorLayout);
                bottomTabs.TabPages.Add(editorPage);
            }

            layout.Controls.Add(filterPanel, 0, 0);
            layout.Controls.Add(listBox, 0, 1);
            layout.Controls.Add(bottomTabs, 0, 2);
            Controls.Add(layout);

            suppressTitleSelectionGuard = true;
            try
            {
                PopulateListFilters(targetListIndex);
                LoadOptions();
                SelectValue(currentId);
                lastSelectedTitleId = currentId;
                titleEditorInitialized = true;
            }
            finally
            {
                suppressTitleSelectionGuard = false;
            }
            Shown += (s, e) =>
            {
                PositionNearOwnerLeft();
                searchBox.Focus();
            };
            FormClosing += HandleFormClosing;
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

        private void HandleFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!PromptSaveTitleChanges())
            {
                e.Cancel = true;
            }
        }

        private void HandleListBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (isTitlePicker && !suppressTitleSelectionGuard && !titleSaveInProgress)
            {
                ItemReferenceOption selectedOption = listBox.SelectedItem as ItemReferenceOption;
                int newId = selectedOption != null ? selectedOption.Id : 0;
                if (newId != lastSelectedTitleId && !PromptSaveTitleChanges())
                {
                    suppressTitleSelectionGuard = true;
                    try
                    {
                        SelectValue(lastSelectedTitleId);
                    }
                    finally
                    {
                        suppressTitleSelectionGuard = false;
                    }
                    return;
                }
            }

            UpdateDetailsPanel();
            if (isTitlePicker)
            {
                ItemReferenceOption currentSelection = listBox.SelectedItem as ItemReferenceOption;
                lastSelectedTitleId = currentSelection != null ? currentSelection.Id : 0;
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
                || (option.ListName ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Description ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.SecondaryText ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Kind ?? string.Empty).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
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
            Color textColor = ResolveTextColor(option, selected);
            Color listColor = selected ? Color.FromArgb(190, 199, 211) : Color.FromArgb(108, 120, 136);
            string secondaryText = !string.IsNullOrWhiteSpace(option.SecondaryText)
                ? option.SecondaryText
                : option.ListName ?? string.Empty;

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
            DrawHighlightedText(e.Graphics, secondaryText, listBounds, listColor, searchText, selected);
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

        private static Color ResolveTextColor(ItemReferenceOption option, bool selected)
        {
            Color accentColor;
            if (TryParseAccentColor(option != null ? option.AccentHex : string.Empty, out accentColor))
            {
                return accentColor;
            }

            int quality = option != null ? option.Quality : -1;
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

        private static bool TryParseAccentColor(string accentHex, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(accentHex))
            {
                return false;
            }

            string normalized = accentHex.Trim().TrimStart('#');
            if (normalized.Length != 6)
            {
                return false;
            }

            int rgb;
            if (!int.TryParse(normalized, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out rgb))
            {
                return false;
            }

            color = Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            return true;
        }

        private void UpdateDetailsPanel()
        {
            if (detailsBox == null)
            {
                return;
            }

            ItemReferenceOption option = listBox != null ? listBox.SelectedItem as ItemReferenceOption : null;
            if (option == null)
            {
                detailsBox.Text = string.Empty;
                return;
            }

            List<string> lines = new List<string>();
            lines.Add((option.Name ?? "Unknown") + " [" + option.Id.ToString() + "]");

            if (!string.IsNullOrWhiteSpace(option.Kind))
            {
                lines.Add("Type: " + option.Kind);
            }

            if (!string.IsNullOrWhiteSpace(option.ListName))
            {
                lines.Add("Source: " + option.ListName);
            }

            if (!string.IsNullOrWhiteSpace(option.AccentHex))
            {
                lines.Add("Title color: #" + option.AccentHex.Trim().TrimStart('#').ToUpperInvariant());
            }

            if (!string.IsNullOrWhiteSpace(option.SecondaryText))
            {
                lines.Add(option.SecondaryText);
            }

            if (!string.IsNullOrWhiteSpace(option.Description))
            {
                lines.Add(string.Empty);
                lines.Add(option.Description);
            }

            detailsBox.Text = string.Join(Environment.NewLine, lines.ToArray()).Trim();
            UpdateTitleEditor();
        }

        private Label CreateEditorLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.FromArgb(188, 198, 214);
            label.Text = text;
            return label;
        }

        private TextBox CreateEditorTextBox(bool multiline = false)
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.Multiline = multiline;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Color.FromArgb(15, 18, 22);
            textBox.ForeColor = Color.FromArgb(226, 231, 239);
            textBox.TextChanged += (s, e) =>
            {
                if (!suppressTitleEditorUpdates)
                {
                    RefreshTitlePreview();
                }
            };
            return textBox;
        }

        private void UpdateTitleEditor()
        {
            if (!isTitlePicker || titleNameTextBox == null)
            {
                return;
            }

            suppressTitleEditorUpdates = true;
            try
            {
                ItemReferenceOption option = listBox != null ? listBox.SelectedItem as ItemReferenceOption : null;
                if (option == null || !TitleDefinitionCatalog.TryGetEditableDefinition(option.Id, out EditableTitleDefinition definition))
                {
                    titleNameTextBox.Text = string.Empty;
                    titleColorTextBox.Text = string.Empty;
                    titleIconPathTextBox.Text = string.Empty;
                    titleDescriptionTextBox.Text = string.Empty;
                    for (int i = 0; i < titleAddonTextBoxes.Length; i++)
                    {
                        titleAddonTextBoxes[i].Text = string.Empty;
                    }
                }
                else
                {
                    titleNameTextBox.Text = definition.TitleText ?? string.Empty;
                    titleColorTextBox.Text = definition.AccentHex ?? string.Empty;
                    titleIconPathTextBox.Text = definition.IconPath ?? string.Empty;
                    titleDescriptionTextBox.Text = definition.Description ?? string.Empty;
                    for (int i = 0; i < titleAddonTextBoxes.Length; i++)
                    {
                        string value = definition.AddonDescriptions != null && i < definition.AddonDescriptions.Length
                            ? definition.AddonDescriptions[i] ?? string.Empty
                            : string.Empty;
                        titleAddonTextBoxes[i].Text = value;
                    }
                }
            }
            finally
            {
                suppressTitleEditorUpdates = false;
            }

            RefreshTitlePreview();
            UpdateApplyButtonState();
        }

        private void RefreshTitlePreview()
        {
            if (titlePreviewLabel == null)
            {
                return;
            }

            string titleText = titleNameTextBox != null ? (titleNameTextBox.Text ?? string.Empty).Trim() : string.Empty;
            string accentHex = titleColorTextBox != null ? (titleColorTextBox.Text ?? string.Empty).Trim().TrimStart('#') : string.Empty;
            titlePreviewLabel.Text = string.IsNullOrWhiteSpace(titleText) ? "Title preview" : titleText;

            if (accentHex.Length == 6
                && int.TryParse(accentHex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out int rgb))
            {
                titlePreviewLabel.ForeColor = Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            }
            else
            {
                titlePreviewLabel.ForeColor = Color.FromArgb(226, 231, 239);
            }

            UpdateApplyButtonState();
        }

        private async void HandleSaveTitleClick(object sender, EventArgs e)
        {
            if (!isTitlePicker || titleSaveInProgress)
            {
                return;
            }

            ItemReferenceOption option = listBox != null ? listBox.SelectedItem as ItemReferenceOption : null;
            if (option == null)
            {
                MessageBox.Show(this, "Select a title first.", "Title editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            EditableTitleDefinition definition = BuildCurrentTitleDefinition(option.Id);
            string error = string.Empty;
            bool saved = false;

            titleSaveInProgress = true;
            SetTitleSaveBusy(true);

            try
            {
                saved = await Task.Run(() => TitleDefinitionCatalog.SaveEditableDefinition(definition, assetManager, out error));
            }
            finally
            {
                SetTitleSaveBusy(false);
                titleSaveInProgress = false;
            }

            if (!saved)
            {
                MessageBox.Show(this, string.IsNullOrWhiteSpace(error) ? "Unable to save the title." : error, "Title editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            suppressTitleSelectionGuard = true;
            try
            {
                allOptions.Clear();
                allOptions.AddRange(TitleDefinitionCatalog.BuildOptions());
                LoadOptions();
                SelectValue(definition.Id);
                lastSelectedTitleId = definition.Id;
            }
            finally
            {
                suppressTitleSelectionGuard = false;
            }

            UpdateApplyButtonState();
            MessageBox.Show(this, "The title was saved successfully.", "Title editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool SaveCurrentTitleDefinition(bool showSuccessMessage)
        {
            if (!isTitlePicker)
            {
                return false;
            }

            ItemReferenceOption option = listBox != null ? listBox.SelectedItem as ItemReferenceOption : null;
            if (option == null)
            {
                if (showSuccessMessage)
                {
                    MessageBox.Show(this, "Select a title first.", "Title editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return false;
            }

            EditableTitleDefinition definition = BuildCurrentTitleDefinition(option.Id);

            if (!TitleDefinitionCatalog.SaveEditableDefinition(definition, assetManager, out string error))
            {
                MessageBox.Show(this, string.IsNullOrWhiteSpace(error) ? "Unable to save the title." : error, "Title editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            titleSaveInProgress = true;
            suppressTitleSelectionGuard = true;
            try
            {
                allOptions.Clear();
                allOptions.AddRange(TitleDefinitionCatalog.BuildOptions());
                LoadOptions();
                SelectValue(definition.Id);
                lastSelectedTitleId = definition.Id;
            }
            finally
            {
                suppressTitleSelectionGuard = false;
                titleSaveInProgress = false;
            }

            UpdateApplyButtonState();
            if (showSuccessMessage)
            {
                MessageBox.Show(this, "The title was saved successfully.", "Title editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return true;
        }

        private EditableTitleDefinition BuildCurrentTitleDefinition(int titleId)
        {
            EditableTitleDefinition definition = new EditableTitleDefinition
            {
                Id = titleId,
                TitleText = titleNameTextBox != null ? titleNameTextBox.Text ?? string.Empty : string.Empty,
                AccentHex = titleColorTextBox != null ? (titleColorTextBox.Text ?? string.Empty).Trim().TrimStart('#') : string.Empty,
                Description = titleDescriptionTextBox != null ? titleDescriptionTextBox.Text ?? string.Empty : string.Empty,
                AddonDescriptions = new string[5],
                IconPath = titleIconPathTextBox != null ? titleIconPathTextBox.Text ?? string.Empty : string.Empty,
                IsGraphicTitle = titleIconPathTextBox != null && !string.IsNullOrWhiteSpace(titleIconPathTextBox.Text)
            };

            for (int i = 0; i < titleAddonTextBoxes.Length; i++)
            {
                definition.AddonDescriptions[i] = titleAddonTextBoxes[i] != null
                    ? titleAddonTextBoxes[i].Text ?? string.Empty
                    : string.Empty;
            }

            return definition;
        }

        private bool PromptSaveTitleChanges()
        {
            if (!isTitlePicker || !titleEditorInitialized || !HasPendingTitleChanges())
            {
                return true;
            }

            DialogResult result = MessageBox.Show(
                this,
                "You have unsaved title changes. Apply them before leaving this title?",
                "Unsaved title changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                return SaveCurrentTitleDefinition(false);
            }

            return result == DialogResult.No;
        }

        private bool HasPendingTitleChanges()
        {
            if (!isTitlePicker || !titleEditorInitialized || lastSelectedTitleId <= 0)
            {
                return false;
            }

            if (!TitleDefinitionCatalog.TryGetEditableDefinition(lastSelectedTitleId, out EditableTitleDefinition definition))
            {
                return false;
            }

            if (!string.Equals(definition.TitleText ?? string.Empty, titleNameTextBox != null ? titleNameTextBox.Text ?? string.Empty : string.Empty, StringComparison.Ordinal)
                || !string.Equals(definition.AccentHex ?? string.Empty, titleColorTextBox != null ? (titleColorTextBox.Text ?? string.Empty).Trim().TrimStart('#') : string.Empty, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(definition.IconPath ?? string.Empty, titleIconPathTextBox != null ? titleIconPathTextBox.Text ?? string.Empty : string.Empty, StringComparison.Ordinal)
                || !string.Equals(definition.Description ?? string.Empty, titleDescriptionTextBox != null ? titleDescriptionTextBox.Text ?? string.Empty : string.Empty, StringComparison.Ordinal))
            {
                return true;
            }

            for (int i = 0; i < titleAddonTextBoxes.Length; i++)
            {
                string currentValue = titleAddonTextBoxes[i] != null ? titleAddonTextBoxes[i].Text ?? string.Empty : string.Empty;
                string storedValue = definition.AddonDescriptions != null && i < definition.AddonDescriptions.Length
                    ? definition.AddonDescriptions[i] ?? string.Empty
                    : string.Empty;
                if (!string.Equals(storedValue, currentValue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateApplyButtonState()
        {
            if (applyTitleButton == null || !isTitlePicker)
            {
                return;
            }

            bool pendingChanges = HasPendingTitleChanges();
            applyTitleButton.Enabled = pendingChanges;
            applyTitleButton.Text = pendingChanges ? "Apply*" : "Apply";
        }

        private void SetTitleSaveBusy(bool isBusy)
        {
            UseWaitCursor = isBusy;
            Cursor = isBusy ? Cursors.WaitCursor : Cursors.Default;

            if (applyTitleButton != null)
            {
                applyTitleButton.Enabled = !isBusy && HasPendingTitleChanges();
                applyTitleButton.Text = isBusy ? "Applying..." : (HasPendingTitleChanges() ? "Apply*" : "Apply");
            }

            if (listComboBox != null)
            {
                listComboBox.Enabled = !isBusy;
            }

            if (searchBox != null)
            {
                searchBox.Enabled = !isBusy;
            }

            if (listBox != null)
            {
                listBox.Enabled = !isBusy;
            }
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

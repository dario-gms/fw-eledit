using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using FWEledit.DDSReader;

namespace FWEledit
{
    public sealed class TitleEditorWindow : Form
    {
        private readonly AssetManager assetManager;
        private readonly List<EditableTitleDefinition> titles = new List<EditableTitleDefinition>();
        private readonly List<int> deletedIds = new List<int>();
        private readonly Dictionary<int, EditableTitleDefinition> originalTitles = new Dictionary<int, EditableTitleDefinition>();
        private readonly List<ColorOption> colorOptions = new List<ColorOption>();
        private readonly List<TitleCategoryOption> categoryOptions = new List<TitleCategoryOption>();
        private readonly Dictionary<string, Bitmap> graphicTitleCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly PckEntryReaderService pckReader = new PckEntryReaderService();
        private readonly TgaImageService tgaImageService = new TgaImageService();

        private TextBox searchTextBox;
        private DataGridView titleGrid;
        private NumericUpDown idBox;
        private ComboBox colorBox;
        private ComboBox categoryBox;
        private TextBox nameBox;
        private CheckBox graphicTitleCheckBox;
        private CheckBox graphicChatCheckBox;
        private TextBox iconPathBox;
        private Button iconPathPickerButton;
        private Button iconPathImportButton;
        private ComboBox graphicSizePresetBox;
        private NumericUpDown graphicWidthBox;
        private NumericUpDown graphicHeightBox;
        private TextBox descriptionBox;
        private TextBox[] bonusBoxes;
        private Label previewNameLabel;
        private RichTextBox previewDescriptionBox;
        private GraphicTitlePreviewBox graphicTitlePictureBox;
        private Label graphicTitleStatusLabel;
        private DataGridView categoryGrid;
        private Label statusLabel;
        private Button applyButton;
        private Button resetButton;
        private Button saveButton;
        private Button reloadButton;
        private bool loadingSelection;
        private bool dirty;
        private List<TitleGraphicIconOption> graphicIconOptions;
        private bool pendingSurfaceChanges;
        private readonly Dictionary<string, string> pendingSurfaceAssetFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex ColorCodeRegex = new Regex("\\^[0-9a-fA-F]{6}", RegexOptions.Compiled);

        public TitleEditorWindow(AssetManager assetManager)
        {
            this.assetManager = assetManager;
            InitializeComponent();
            LoadTitles();
        }

        private void InitializeComponent()
        {
            Text = "FWEledit - Advanced Title Editor";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(1180, 720);
            Size = new Size(1480, 860);

            Color back = Color.FromArgb(15, 19, 24);
            Color panel = Color.FromArgb(27, 33, 41);
            Color raised = Color.FromArgb(38, 46, 57);
            Color text = Color.White;
            Color accent = Color.FromArgb(83, 151, 213);

            BackColor = back;
            ForeColor = text;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.BackColor = back;
            Controls.Add(root);

            TableLayoutPanel top = new TableLayoutPanel();
            top.Dock = DockStyle.Fill;
            top.ColumnCount = 4;
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.Padding = new Padding(8, 7, 8, 4);
            top.BackColor = back;
            root.Controls.Add(top, 0, 0);

            Label scriptLabel = BuildLabel("script.pck:");
            top.Controls.Add(scriptLabel, 0, 0);

            TextBox scriptPathBox = BuildTextBox();
            scriptPathBox.ReadOnly = true;
            scriptPathBox.Text = string.IsNullOrWhiteSpace(AssetManager.GameRootPath)
                ? string.Empty
                : System.IO.Path.Combine(AssetManager.GameRootPath, "resources", "script.pck");
            top.Controls.Add(scriptPathBox, 1, 0);

            reloadButton = BuildButton("Reload");
            reloadButton.Click += reloadButton_Click;
            top.Controls.Add(reloadButton, 2, 0);

            saveButton = BuildButton("Save");
            saveButton.Click += saveButton_Click;
            top.Controls.Add(saveButton, 3, 0);

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel2;
            split.Panel1MinSize = 120;
            split.Panel2MinSize = 120;
            split.BackColor = back;
            split.Panel1.BackColor = back;
            split.Panel2.BackColor = back;
            split.SizeChanged += delegate { ResizeMainSplit(split); };
            Shown += delegate { BeginInvoke(new Action(delegate { ResizeMainSplit(split); })); };
            root.Controls.Add(split, 0, 1);

            TableLayoutPanel left = new TableLayoutPanel();
            left.Dock = DockStyle.Fill;
            left.RowCount = 3;
            left.ColumnCount = 1;
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            left.Padding = new Padding(8);
            left.BackColor = back;
            split.Panel1.Controls.Add(left);

            searchTextBox = BuildTextBox();
            searchTextBox.Dock = DockStyle.Fill;
            searchTextBox.TextChanged += delegate { RebuildGrid(); };
            left.Controls.Add(searchTextBox, 0, 0);

            titleGrid = new DataGridView();
            titleGrid.Dock = DockStyle.Fill;
            titleGrid.AllowUserToAddRows = false;
            titleGrid.AllowUserToDeleteRows = false;
            titleGrid.AllowUserToResizeRows = false;
            titleGrid.MultiSelect = false;
            titleGrid.ReadOnly = true;
            titleGrid.RowHeadersVisible = false;
            titleGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            titleGrid.ScrollBars = ScrollBars.Vertical;
            titleGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            titleGrid.BackgroundColor = back;
            titleGrid.GridColor = Color.FromArgb(43, 51, 61);
            titleGrid.BorderStyle = BorderStyle.FixedSingle;
            titleGrid.EnableHeadersVisualStyles = false;
            titleGrid.ColumnHeadersDefaultCellStyle.BackColor = raised;
            titleGrid.ColumnHeadersDefaultCellStyle.ForeColor = text;
            titleGrid.DefaultCellStyle.BackColor = Color.FromArgb(18, 23, 29);
            titleGrid.DefaultCellStyle.ForeColor = text;
            titleGrid.DefaultCellStyle.SelectionBackColor = accent;
            titleGrid.DefaultCellStyle.SelectionForeColor = Color.White;
            titleGrid.RowTemplate.Height = 30;
            titleGrid.Columns.Add("Id", "ID");
            DataGridViewImageColumn graphicColumn = new DataGridViewImageColumn();
            graphicColumn.Name = "Graphic";
            graphicColumn.HeaderText = "Graphic";
            graphicColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
            graphicColumn.DefaultCellStyle.NullValue = null;
            titleGrid.Columns.Add(graphicColumn);
            titleGrid.Columns.Add("Name", "Name");
            titleGrid.Columns.Add("Category", "Category");
            titleGrid.Columns[0].Width = 64;
            titleGrid.Columns[1].Width = 58;
            titleGrid.Columns[0].FillWeight = 14;
            titleGrid.Columns[1].FillWeight = 12;
            titleGrid.Columns[2].FillWeight = 43;
            titleGrid.Columns[3].FillWeight = 31;
            titleGrid.SelectionChanged += titleGrid_SelectionChanged;
            titleGrid.CellFormatting += titleGrid_CellFormatting;
            left.Controls.Add(titleGrid, 0, 1);

            FlowLayoutPanel leftButtons = new FlowLayoutPanel();
            leftButtons.Dock = DockStyle.Fill;
            leftButtons.FlowDirection = FlowDirection.LeftToRight;
            leftButtons.BackColor = back;
            left.Controls.Add(leftButtons, 0, 2);

            Button newButton = BuildButton("+ New");
            newButton.Width = 92;
            newButton.Click += newButton_Click;
            leftButtons.Controls.Add(newButton);

            Button cloneButton = BuildButton("Clone");
            cloneButton.Width = 92;
            cloneButton.Click += cloneButton_Click;
            leftButtons.Controls.Add(cloneButton);

            Button deleteButton = BuildButton("Delete");
            deleteButton.Width = 92;
            deleteButton.Click += deleteButton_Click;
            leftButtons.Controls.Add(deleteButton);

            TableLayoutPanel editor = new TableLayoutPanel();
            editor.Dock = DockStyle.Fill;
            editor.RowCount = 3;
            editor.ColumnCount = 1;
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 484));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            editor.Padding = new Padding(8);
            editor.BackColor = back;
            split.Panel2.Controls.Add(editor);

            GroupBox editGroup = BuildGroup("Edit Title");
            editor.Controls.Add(editGroup, 0, 0);

            TableLayoutPanel editLayout = new TableLayoutPanel();
            editLayout.Dock = DockStyle.Fill;
            editLayout.ColumnCount = 2;
            editLayout.RowCount = 14;
            editLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
            editLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            editGroup.Controls.Add(editLayout);

            idBox = BuildNumericBox(0, int.MaxValue);
            colorBox = new ComboBox();
            colorBox.Dock = DockStyle.Fill;
            colorBox.DropDownStyle = ComboBoxStyle.DropDownList;
            colorBox.DrawMode = DrawMode.OwnerDrawFixed;
            colorBox.Height = 24;
            colorBox.BackColor = Color.FromArgb(18, 23, 29);
            colorBox.ForeColor = text;
            colorBox.DrawItem += colorBox_DrawItem;
            colorBox.SelectedIndexChanged += editorValueChanged;
            categoryBox = new ComboBox();
            categoryBox.Dock = DockStyle.Fill;
            categoryBox.DropDownStyle = ComboBoxStyle.DropDownList;
            categoryBox.BackColor = Color.FromArgb(18, 23, 29);
            categoryBox.ForeColor = text;
            nameBox = BuildTextBox();
            graphicTitleCheckBox = new CheckBox();
            graphicTitleCheckBox.Text = "Enable graphic title";
            graphicTitleCheckBox.Dock = DockStyle.Left;
            graphicTitleCheckBox.AutoSize = true;
            graphicTitleCheckBox.BackColor = back;
            graphicTitleCheckBox.ForeColor = text;
            graphicChatCheckBox = new CheckBox();
            graphicChatCheckBox.Text = "Show graphic title in chat";
            graphicChatCheckBox.Dock = DockStyle.Left;
            graphicChatCheckBox.AutoSize = true;
            graphicChatCheckBox.BackColor = back;
            graphicChatCheckBox.ForeColor = text;
            iconPathBox = BuildTextBox();
            iconPathPickerButton = BuildButton("...");
            iconPathPickerButton.Width = 34;
            iconPathPickerButton.Margin = new Padding(4, 0, 0, 0);
            iconPathPickerButton.Click += iconPathPickerButton_Click;
            iconPathImportButton = BuildButton("Import");
            iconPathImportButton.Width = 70;
            iconPathImportButton.Margin = new Padding(4, 0, 0, 0);
            iconPathImportButton.Click += iconPathImportButton_Click;
            graphicSizePresetBox = new ComboBox();
            graphicSizePresetBox.Dock = DockStyle.Fill;
            graphicSizePresetBox.DropDownStyle = ComboBoxStyle.DropDownList;
            graphicSizePresetBox.BackColor = Color.FromArgb(18, 23, 29);
            graphicSizePresetBox.ForeColor = text;
            graphicSizePresetBox.SelectedIndexChanged += graphicSizePresetBox_SelectedIndexChanged;
            graphicWidthBox = BuildNumericBox(1, 1024);
            graphicWidthBox.Value = 80;
            graphicHeightBox = BuildNumericBox(1, 1024);
            graphicHeightBox.Value = 40;
            descriptionBox = BuildTextBox();
            descriptionBox.Multiline = true;
            descriptionBox.ScrollBars = ScrollBars.Vertical;
            descriptionBox.Height = 128;
            bonusBoxes = new TextBox[5];

            AddEditorRow(editLayout, 0, "ID:", idBox);
            AddEditorRow(editLayout, 1, "Color:", colorBox);
            AddEditorRow(editLayout, 2, "Category:", categoryBox);
            AddEditorRow(editLayout, 3, "Name:", nameBox);
            AddEditorRow(editLayout, 4, "Graphic:", graphicTitleCheckBox);
            AddEditorRow(editLayout, 5, "Chat:", graphicChatCheckBox);
            AddEditorRow(editLayout, 6, "Icon path:", BuildIconPathEditor());
            AddEditorRow(editLayout, 7, "Texture size:", BuildGraphicImportSizeEditor());
            AddEditorRow(editLayout, 8, "Description:", descriptionBox);
            for (int i = 0; i < bonusBoxes.Length; i++)
            {
                bonusBoxes[i] = BuildTextBox();
                AddEditorRow(editLayout, i + 9, "Bonus " + (i + 1).ToString(CultureInfo.InvariantCulture) + ":", bonusBoxes[i]);
            }

            TabControl lowerTabs = new TabControl();
            lowerTabs.Dock = DockStyle.Fill;
            lowerTabs.Appearance = TabAppearance.Normal;
            lowerTabs.BackColor = back;
            editor.Controls.Add(lowerTabs, 0, 1);

            TabPage previewPage = new TabPage("Preview");
            previewPage.BackColor = back;
            lowerTabs.TabPages.Add(previewPage);

            TabPage categoriesPage = new TabPage("Categories");
            categoriesPage.BackColor = back;
            lowerTabs.TabPages.Add(categoriesPage);

            GroupBox previewGroup = BuildGroup("Preview");
            previewPage.Controls.Add(previewGroup);

            TableLayoutPanel previewLayout = new TableLayoutPanel();
            previewLayout.Dock = DockStyle.Fill;
            previewLayout.RowCount = 3;
            previewLayout.ColumnCount = 2;
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            previewLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewGroup.Controls.Add(previewLayout);

            previewNameLabel = new Label();
            previewNameLabel.Dock = DockStyle.Fill;
            previewNameLabel.TextAlign = ContentAlignment.MiddleCenter;
            previewNameLabel.BackColor = panel;
            previewNameLabel.ForeColor = accent;
            previewNameLabel.Font = new Font(Font, FontStyle.Bold);
            previewLayout.Controls.Add(previewNameLabel, 0, 0);
            previewLayout.SetColumnSpan(previewNameLabel, 2);

            Label descPreviewLabel = BuildLabel("Description:");
            previewLayout.Controls.Add(descPreviewLabel, 0, 1);

            Label graphicPreviewLabel = BuildLabel("Graphic:");
            previewLayout.Controls.Add(graphicPreviewLabel, 1, 1);

            previewDescriptionBox = new RichTextBox();
            previewDescriptionBox.Dock = DockStyle.Fill;
            previewDescriptionBox.BorderStyle = BorderStyle.FixedSingle;
            previewDescriptionBox.BackColor = Color.FromArgb(18, 23, 29);
            previewDescriptionBox.ForeColor = Color.White;
            previewDescriptionBox.Multiline = true;
            previewDescriptionBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            previewDescriptionBox.ReadOnly = true;
            previewLayout.Controls.Add(previewDescriptionBox, 0, 2);

            Panel graphicPanel = new Panel();
            graphicPanel.Dock = DockStyle.Fill;
            graphicPanel.BackColor = Color.FromArgb(18, 23, 29);
            graphicPanel.BorderStyle = BorderStyle.FixedSingle;
            previewLayout.Controls.Add(graphicPanel, 1, 2);

            TableLayoutPanel graphicLayout = new TableLayoutPanel();
            graphicLayout.Dock = DockStyle.Fill;
            graphicLayout.RowCount = 2;
            graphicLayout.ColumnCount = 1;
            graphicLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            graphicLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            graphicLayout.BackColor = graphicPanel.BackColor;
            graphicPanel.Controls.Add(graphicLayout);

            graphicTitlePictureBox = new GraphicTitlePreviewBox();
            graphicTitlePictureBox.Dock = DockStyle.Fill;
            graphicTitlePictureBox.BackColor = Color.FromArgb(12, 15, 20);
            graphicLayout.Controls.Add(graphicTitlePictureBox, 0, 0);

            graphicTitleStatusLabel = BuildLabel("No graphic title");
            graphicTitleStatusLabel.TextAlign = ContentAlignment.MiddleCenter;
            graphicTitleStatusLabel.ForeColor = Color.FromArgb(160, 174, 192);
            graphicLayout.Controls.Add(graphicTitleStatusLabel, 0, 1);

            categoryGrid = new DataGridView();
            categoryGrid.Dock = DockStyle.Fill;
            categoryGrid.AllowUserToAddRows = false;
            categoryGrid.AllowUserToDeleteRows = false;
            categoryGrid.AllowUserToResizeRows = false;
            categoryGrid.ReadOnly = true;
            categoryGrid.RowHeadersVisible = false;
            categoryGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            categoryGrid.BackgroundColor = back;
            categoryGrid.GridColor = Color.FromArgb(43, 51, 61);
            categoryGrid.BorderStyle = BorderStyle.FixedSingle;
            categoryGrid.EnableHeadersVisualStyles = false;
            categoryGrid.ColumnHeadersDefaultCellStyle.BackColor = raised;
            categoryGrid.ColumnHeadersDefaultCellStyle.ForeColor = text;
            categoryGrid.DefaultCellStyle.BackColor = Color.FromArgb(18, 23, 29);
            categoryGrid.DefaultCellStyle.ForeColor = text;
            categoryGrid.DefaultCellStyle.SelectionBackColor = accent;
            categoryGrid.DefaultCellStyle.SelectionForeColor = Color.White;
            categoryGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            categoryGrid.Columns.Add("Category", "Category");
            categoryGrid.Columns.Add("Titles", "Titles");
            categoryGrid.Columns[0].FillWeight = 82;
            categoryGrid.Columns[1].FillWeight = 18;
            categoryGrid.CellDoubleClick += categoryGrid_CellDoubleClick;
            categoriesPage.Controls.Add(categoryGrid);

            FlowLayoutPanel editorButtons = new FlowLayoutPanel();
            editorButtons.Dock = DockStyle.Fill;
            editorButtons.FlowDirection = FlowDirection.LeftToRight;
            editorButtons.BackColor = back;
            editor.Controls.Add(editorButtons, 0, 2);

            applyButton = BuildButton("Apply");
            applyButton.Width = 92;
            applyButton.Click += applyButton_Click;
            editorButtons.Controls.Add(applyButton);

            resetButton = BuildButton("Reset");
            resetButton.Width = 92;
            resetButton.Click += resetButton_Click;
            editorButtons.Controls.Add(resetButton);

            statusLabel = BuildLabel("Ready");
            statusLabel.Dock = DockStyle.Fill;
            root.Controls.Add(statusLabel, 0, 2);

            InitializeColorOptions();
            InitializeGraphicSizeOptions();
            HookEditorChangedEvents();
        }

        private void LoadTitles()
        {
            Cursor previous = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                TitleDefinitionCatalog.InvalidateCache();
                titles.Clear();
                deletedIds.Clear();
                originalTitles.Clear();
                categoryOptions.Clear();
                graphicIconOptions = null;
                ClearGraphicTitleCache();
                pendingSurfaceChanges = false;
                pendingSurfaceAssetFiles.Clear();

                categoryOptions.Add(new TitleCategoryOption { PathKey = string.Empty, Display = "! Uncategorized", TitleCount = 0 });
                List<TitleCategoryOption> loadedCategories = TitleDefinitionCatalog.BuildCategoryOptions();
                for (int i = 0; i < loadedCategories.Count; i++)
                {
                    if (loadedCategories[i] != null)
                    {
                        categoryOptions.Add(loadedCategories[i]);
                    }
                }
                categoryBox.DataSource = null;
                categoryBox.DataSource = categoryOptions;
                categoryBox.DisplayMember = "Display";
                categoryBox.ValueMember = "PathKey";
                RebuildCategoryGrid();

                List<EditableTitleDefinition> loaded = TitleDefinitionCatalog.BuildEditableDefinitions();
                for (int i = 0; i < loaded.Count; i++)
                {
                    EditableTitleDefinition title = NormalizeTitle(loaded[i]);
                    titles.Add(title);
                    originalTitles[title.Id] = CloneTitle(title);
                }

                RebuildGrid();
                statusLabel.Text = "Loaded " + titles.Count.ToString(CultureInfo.InvariantCulture) + " titles from script.pck";
                dirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to load title_def_u.lua.\n\n" + ex.Message, "Advanced Title Editor");
            }
            finally
            {
                Cursor.Current = previous;
            }
        }

        private void RebuildGrid()
        {
            string query = (searchTextBox != null ? searchTextBox.Text : string.Empty) ?? string.Empty;
            query = query.Trim();
            int selectedId = GetSelectedTitleId();
            titleGrid.Rows.Clear();

            IEnumerable<EditableTitleDefinition> source = titles.OrderBy(t => t.Id);
            if (!string.IsNullOrWhiteSpace(query))
            {
                source = source.Where(delegate (EditableTitleDefinition title)
                {
                    if (title == null)
                    {
                        return false;
                    }

                    return title.Id.ToString(CultureInfo.InvariantCulture).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                        || (title.TitleText ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                        || (title.Description ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                        || (title.CategoryDisplay ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                });
            }

            foreach (EditableTitleDefinition title in source)
            {
                int rowIndex = titleGrid.Rows.Add(
                    title.Id.ToString(CultureInfo.InvariantCulture),
                    BuildGraphicTitleGridIcon(title),
                    title.TitleText ?? string.Empty,
                    title.CategoryDisplay ?? string.Empty);
                titleGrid.Rows[rowIndex].Tag = title;
                if (title.Id == selectedId)
                {
                    titleGrid.Rows[rowIndex].Selected = true;
                    titleGrid.CurrentCell = titleGrid.Rows[rowIndex].Cells[0];
                }
            }

            if (titleGrid.SelectedRows.Count == 0 && titleGrid.Rows.Count > 0)
            {
                titleGrid.Rows[0].Selected = true;
                titleGrid.CurrentCell = titleGrid.Rows[0].Cells[0];
            }
        }

        private void titleGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (loadingSelection)
            {
                return;
            }

            EditableTitleDefinition title = GetSelectedTitle();
            LoadTitleIntoEditor(title);
        }

        private Image BuildGraphicTitleGridIcon(EditableTitleDefinition title)
        {
            if (title == null || !title.IsGraphicTitle || string.IsNullOrWhiteSpace(title.IconPath))
            {
                return null;
            }

            return LoadGraphicTitleImage(NormalizeAssetPath(title.IconPath));
        }

        private void iconPathPickerButton_Click(object sender, EventArgs e)
        {
            List<TitleGraphicIconOption> options = BuildGraphicIconOptions();
            using (TitleGraphicIconPickerWindow picker = new TitleGraphicIconPickerWindow(
                options,
                iconPathBox.Text,
                LoadGraphicTitleImage))
            {
                if (picker.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                iconPathBox.Text = picker.SelectedPath ?? string.Empty;
                graphicTitleCheckBox.Checked = !string.IsNullOrWhiteSpace(iconPathBox.Text);
                if (graphicTitleCheckBox.Checked)
                {
                    graphicChatCheckBox.Checked = true;
                }
                ApplyEditorTextToSelected();
                MarkDirty();
                RebuildGrid();
                UpdatePreview();
            }
        }

        private void iconPathImportButton_Click(object sender, EventArgs e)
        {
            EditableTitleDefinition title = GetSelectedTitle();
            if (title == null)
            {
                MessageBox.Show(this, "Select a title before importing a graphic.", "Advanced Title Editor");
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Import title graphic";
                dialog.Filter = "Title graphics (*.png;*.gif;*.svg)|*.png;*.gif;*.svg|PNG (*.png)|*.png|GIF (*.gif)|*.gif|SVG (*.svg)|*.svg|All files (*.*)|*.*";
                dialog.Multiselect = false;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                string importedPath;
                string error;
                if (!TryImportGraphicTitleImage(dialog.FileName, title, out importedPath, out error))
                {
                    MessageBox.Show(this, error, "Advanced Title Editor");
                    return;
                }

                iconPathBox.Text = importedPath;
                graphicTitleCheckBox.Checked = true;
                graphicChatCheckBox.Checked = true;
                ApplyEditorTextToSelected();
                MarkDirty();
                pendingSurfaceChanges = true;
                graphicIconOptions = null;
                RebuildGrid();
                UpdatePreview();
                statusLabel.Text = "Imported graphic title: " + importedPath + " (" + GetGraphicImportSizeLabel() + ")";
            }
        }

        private void graphicSizePresetBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateGraphicSizeControls();
        }

        private void LoadTitleIntoEditor(EditableTitleDefinition title)
        {
            loadingSelection = true;
            try
            {
                if (title == null)
                {
                    idBox.Value = 0;
                    nameBox.Text = string.Empty;
                    graphicTitleCheckBox.Checked = false;
                    graphicChatCheckBox.Checked = false;
                    iconPathBox.Text = string.Empty;
                    descriptionBox.Text = string.Empty;
                    for (int i = 0; i < bonusBoxes.Length; i++)
                    {
                        bonusBoxes[i].Text = string.Empty;
                    }

                    SelectColor(string.Empty);
                    SelectCategory(string.Empty);
                    UpdatePreview();
                    return;
                }

                idBox.Value = Math.Max(idBox.Minimum, Math.Min(idBox.Maximum, title.Id));
                nameBox.Text = title.TitleText ?? string.Empty;
                graphicTitleCheckBox.Checked = title.IsGraphicTitle;
                graphicChatCheckBox.Checked = title.IsGraphicTitle && title.ShowGraphicInChat;
                iconPathBox.Text = title.IconPath ?? string.Empty;
                descriptionBox.Text = title.Description ?? string.Empty;
                for (int i = 0; i < bonusBoxes.Length; i++)
                {
                    bonusBoxes[i].Text = title.AddonDescriptions != null && i < title.AddonDescriptions.Length
                        ? title.AddonDescriptions[i] ?? string.Empty
                        : string.Empty;
                }

                SelectColor(title.AccentHex);
                SelectCategory(title.CategoryPathKey);
                UpdatePreview();
            }
            finally
            {
                loadingSelection = false;
            }
        }

        private void ApplyEditorToSelected()
        {
            EditableTitleDefinition title = GetSelectedTitle();
            if (title == null)
            {
                return;
            }

            int newId = Convert.ToInt32(idBox.Value);
            if (newId <= 0)
            {
                MessageBox.Show(this, "Title ID must be greater than zero.", "Advanced Title Editor");
                return;
            }

            EditableTitleDefinition duplicated = titles.FirstOrDefault(t => t != null && t.Id == newId && !object.ReferenceEquals(t, title));
            if (duplicated != null)
            {
                MessageBox.Show(this, "Another title already uses this ID.", "Advanced Title Editor");
                return;
            }

            if (title.Id != newId)
            {
                if (originalTitles.ContainsKey(title.Id) && !deletedIds.Contains(title.Id))
                {
                    deletedIds.Add(title.Id);
                }
                title.Id = newId;
            }

            title.TitleText = nameBox.Text.Trim();
            title.AccentHex = GetSelectedColorHex();
            title.CategoryPathKey = GetSelectedCategoryPath();
            title.CategoryDisplay = GetSelectedCategoryDisplay();
            title.IconPath = iconPathBox.Text.Trim();
            title.IsGraphicTitle = graphicTitleCheckBox.Checked && !string.IsNullOrWhiteSpace(title.IconPath);
            title.ShowGraphicInChat = title.IsGraphicTitle && graphicChatCheckBox.Checked;
            title.Description = descriptionBox.Text;
            title.AddonDescriptions = new string[5];
            for (int i = 0; i < bonusBoxes.Length; i++)
            {
                title.AddonDescriptions[i] = bonusBoxes[i].Text;
            }

            MarkDirty();
            RebuildGrid();
            UpdatePreview();
        }

        private void ApplyEditorTextToSelected()
        {
            EditableTitleDefinition title = GetSelectedTitle();
            if (title == null)
            {
                return;
            }

            title.TitleText = nameBox.Text.Trim();
            title.AccentHex = GetSelectedColorHex();
            title.CategoryPathKey = GetSelectedCategoryPath();
            title.CategoryDisplay = GetSelectedCategoryDisplay();
            title.IconPath = iconPathBox.Text.Trim();
            title.IsGraphicTitle = graphicTitleCheckBox.Checked && !string.IsNullOrWhiteSpace(title.IconPath);
            title.ShowGraphicInChat = title.IsGraphicTitle && graphicChatCheckBox.Checked;
            title.Description = descriptionBox.Text;
            title.AddonDescriptions = new string[5];
            for (int i = 0; i < bonusBoxes.Length; i++)
            {
                title.AddonDescriptions[i] = bonusBoxes[i].Text;
            }
        }

        private void SaveTitles()
        {
            ApplyEditorToSelected();
            Cursor previous = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                saveButton.Enabled = false;
                if (pendingSurfaceChanges)
                {
                    string surfaceSummary;
                    statusLabel.Text = "Updating surfaces.pck with imported title graphics...";
                    Application.DoEvents();
                    if (!ImportPendingSurfaceAssets(out surfaceSummary))
                    {
                        MessageBox.Show(this, surfaceSummary, "Advanced Title Editor");
                        statusLabel.Text = "Save failed";
                        return;
                    }

                    pendingSurfaceChanges = false;
                    pendingSurfaceAssetFiles.Clear();
                }

                statusLabel.Text = "Saving title_def_u.lua and updating script.pck...";
                Application.DoEvents();

                string error;
                if (!TitleDefinitionCatalog.SaveEditableDefinitions(titles, deletedIds, assetManager, out error))
                {
                    MessageBox.Show(this, error, "Advanced Title Editor");
                    statusLabel.Text = "Save failed";
                    return;
                }

                originalTitles.Clear();
                for (int i = 0; i < titles.Count; i++)
                {
                    originalTitles[titles[i].Id] = CloneTitle(titles[i]);
                }
                deletedIds.Clear();
                dirty = false;
                statusLabel.Text = "Saved " + titles.Count.ToString(CultureInfo.InvariantCulture) + " titles.";
            }
            finally
            {
                saveButton.Enabled = true;
                Cursor.Current = previous;
            }
        }

        private bool ImportPendingSurfaceAssets(out string summary)
        {
            summary = string.Empty;
            if (pendingSurfaceAssetFiles.Count == 0)
            {
                summary = "No imported title graphics are pending.";
                return true;
            }

            if (assetManager == null)
            {
                summary = "Asset manager is not available.";
                return false;
            }

            string stagingRoot = Path.Combine(Path.GetTempPath(), "FWEledit", "pck-stage", "title-surfaces");
            try
            {
                if (Directory.Exists(stagingRoot))
                {
                    Directory.Delete(stagingRoot, true);
                }
            }
            catch
            { }

            Directory.CreateDirectory(stagingRoot);
            foreach (KeyValuePair<string, string> pair in pendingSurfaceAssetFiles)
            {
                string assetPath = NormalizeAssetPath(pair.Key);
                string sourcePath = pair.Value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
                {
                    summary = "Imported title graphic was not found: " + assetPath;
                    return false;
                }

                string targetPath = Path.Combine(stagingRoot, assetPath);
                string targetDirectory = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }
                File.Copy(sourcePath, targetPath, true);
            }

            return assetManager.ImportStagedPackageAssets("surfaces", stagingRoot, out summary);
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            ApplyEditorToSelected();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            EditableTitleDefinition title = GetSelectedTitle();
            if (title == null)
            {
                return;
            }

            EditableTitleDefinition original;
            if (originalTitles.TryGetValue(title.Id, out original))
            {
                CopyTitle(original, title);
                LoadTitleIntoEditor(title);
                RebuildGrid();
                MarkDirty();
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveTitles();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            if (dirty)
            {
                DialogResult result = MessageBox.Show(this, "Discard pending title changes?", "Advanced Title Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    return;
                }
            }

            LoadTitles();
        }

        private void newButton_Click(object sender, EventArgs e)
        {
            int nextId = 1;
            if (titles.Count > 0)
            {
                nextId = titles.Max(t => t.Id) + 1;
            }

            EditableTitleDefinition title = new EditableTitleDefinition
            {
                Id = nextId,
                TitleText = "New Title",
                AccentHex = "FFFFFF",
                Description = string.Empty,
                AddonDescriptions = new string[5],
                IconPath = string.Empty,
                IsGraphicTitle = false,
                ShowGraphicInChat = false,
                CategoryPathKey = string.Empty,
                CategoryDisplay = "! Uncategorized"
            };
            SelectCategory(string.Empty);
            titles.Add(title);
            MarkDirty();
            RebuildGrid();
            SelectTitle(nextId);
        }

        private void cloneButton_Click(object sender, EventArgs e)
        {
            EditableTitleDefinition current = GetSelectedTitle();
            if (current == null)
            {
                return;
            }

            EditableTitleDefinition clone = CloneTitle(current);
            clone.Id = titles.Max(t => t.Id) + 1;
            clone.TitleText = (clone.TitleText ?? "Title") + " Copy";
            titles.Add(clone);
            MarkDirty();
            RebuildGrid();
            SelectTitle(clone.Id);
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            EditableTitleDefinition current = GetSelectedTitle();
            if (current == null)
            {
                return;
            }

            DialogResult result = MessageBox.Show(this, "Delete title " + current.Id.ToString(CultureInfo.InvariantCulture) + "?", "Advanced Title Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes)
            {
                return;
            }

            if (originalTitles.ContainsKey(current.Id) && !deletedIds.Contains(current.Id))
            {
                deletedIds.Add(current.Id);
            }
            titles.Remove(current);
            MarkDirty();
            RebuildGrid();
        }

        private void editorValueChanged(object sender, EventArgs e)
        {
            if (loadingSelection)
            {
                return;
            }

            ApplyEditorTextToSelected();
            MarkDirty();
            UpdatePreview();
        }

        private void titleGrid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= titleGrid.Rows.Count)
            {
                return;
            }

            EditableTitleDefinition title = titleGrid.Rows[e.RowIndex].Tag as EditableTitleDefinition;
            if (title == null || e.ColumnIndex != 2)
            {
                return;
            }

            Color color;
            if (TryParseHexColor(title.AccentHex, out color))
            {
                e.CellStyle.ForeColor = EnsureReadableOnDark(color);
                e.CellStyle.SelectionForeColor = EnsureReadableOnAccent(color);
            }
        }

        private void categoryGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= categoryGrid.Rows.Count)
            {
                return;
            }

            TitleCategoryOption category = categoryGrid.Rows[e.RowIndex].Tag as TitleCategoryOption;
            EditableTitleDefinition title = GetSelectedTitle();
            if (category == null || title == null)
            {
                return;
            }

            SelectCategory(category.PathKey);
            ApplyEditorTextToSelected();
            MarkDirty();
            RebuildGrid();
        }

        private void colorBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            if (e.Index < 0 || e.Index >= colorOptions.Count)
            {
                return;
            }

            ColorOption option = colorOptions[e.Index];
            Rectangle swatch = new Rectangle(e.Bounds.Left + 4, e.Bounds.Top + 4, 28, Math.Max(8, e.Bounds.Height - 8));
            using (SolidBrush brush = new SolidBrush(option.Color))
            {
                e.Graphics.FillRectangle(brush, swatch);
            }
            e.Graphics.DrawRectangle(Pens.Gray, swatch);
            using (SolidBrush brush = new SolidBrush(e.ForeColor))
            {
                e.Graphics.DrawString(option.Label, e.Font, brush, e.Bounds.Left + 38, e.Bounds.Top + 3);
            }
            e.DrawFocusRectangle();
        }

        private void UpdatePreview()
        {
            string colorHex = GetSelectedColorHex();
            Color color;
            previewNameLabel.ForeColor = TryParseHexColor(colorHex, out color) ? color : Color.FromArgb(83, 151, 213);
            previewNameLabel.Text = RemoveColorCodes(nameBox.Text);

            List<string> lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(descriptionBox.Text))
            {
                lines.Add(descriptionBox.Text);
            }
            for (int i = 0; i < bonusBoxes.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(bonusBoxes[i].Text))
                {
                    lines.Add(bonusBoxes[i].Text);
                }
            }

            RenderColoredText(previewDescriptionBox, string.Join(Environment.NewLine + Environment.NewLine, lines.ToArray()));
            UpdateGraphicPreview(graphicTitleCheckBox != null && graphicTitleCheckBox.Checked ? iconPathBox.Text : string.Empty);
        }

        private void UpdateGraphicPreview(string iconPath)
        {
            if (graphicTitlePictureBox == null || graphicTitleStatusLabel == null)
            {
                return;
            }

            if (graphicTitlePictureBox.Image != null)
            {
                Image previous = graphicTitlePictureBox.Image;
                graphicTitlePictureBox.Image = null;
                previous.Dispose();
            }

            string normalized = NormalizeAssetPath(iconPath);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                graphicTitleStatusLabel.Text = "No graphic title";
                return;
            }

            Bitmap bitmap = LoadGraphicTitleImage(normalized);
            if (bitmap == null)
            {
                graphicTitleStatusLabel.Text = "Graphic not found";
                return;
            }

            graphicTitlePictureBox.Image = new Bitmap(bitmap);
            graphicTitleStatusLabel.Text = normalized + "  (" + bitmap.Width.ToString(CultureInfo.InvariantCulture) + "x" + bitmap.Height.ToString(CultureInfo.InvariantCulture) + ")";
        }

        private Bitmap LoadGraphicTitleImage(string normalizedPath)
        {
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return null;
            }

            Bitmap cached;
            if (graphicTitleCache.TryGetValue(normalizedPath, out cached) && cached != null)
            {
                return cached;
            }

            Bitmap loaded = LoadGraphicTitleImageFromPackage(normalizedPath);

            if (loaded != null)
            {
                graphicTitleCache[normalizedPath] = loaded;
            }

            return loaded;
        }

        private List<TitleGraphicIconOption> BuildGraphicIconOptions()
        {
            if (graphicIconOptions != null)
            {
                return graphicIconOptions;
            }

            Dictionary<string, TitleGraphicIconOption> options = new Dictionary<string, TitleGraphicIconOption>(StringComparer.OrdinalIgnoreCase);
            AddGraphicIconOptionsFromPackage(options);
            AddGraphicIconOptionsFromTitles(options);

            graphicIconOptions = options.Values
                .OrderBy(option => option.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(option => option.Path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return graphicIconOptions;
        }

        private void AddGraphicIconOptionsFromPackage(Dictionary<string, TitleGraphicIconOption> options)
        {
            List<string> entries;
            string error;
            if (!pckReader.TryEnumerateEntries("surfaces", out entries, out error) || entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                AddGraphicIconOption(options, entries[i], "surfaces.pck");
            }
        }

        private static void AddGraphicIconOption(Dictionary<string, TitleGraphicIconOption> options, string path, string source)
        {
            string normalized = NormalizeAssetPath(path);
            if (!IsGraphicTitleIconPath(normalized))
            {
                return;
            }

            if (!options.ContainsKey(normalized))
            {
                options[normalized] = new TitleGraphicIconOption
                {
                    Path = normalized,
                    FileName = Path.GetFileName(normalized),
                    Source = source ?? string.Empty
                };
            }
        }

        private void AddGraphicIconOptionsFromTitles(Dictionary<string, TitleGraphicIconOption> options)
        {
            for (int i = 0; i < titles.Count; i++)
            {
                EditableTitleDefinition title = titles[i];
                if (title == null || string.IsNullOrWhiteSpace(title.IconPath))
                {
                    continue;
                }

                AddGraphicIconOption(options, title.IconPath, "title_def_u.lua");
            }
        }

        private static bool IsGraphicTitleIconPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = NormalizeAssetPath(StripPackagePrefix(path, "surfaces"));
            string lower = normalized.ToLowerInvariant();
            if (!lower.StartsWith("sm\\smtitle\\", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string extension = Path.GetExtension(lower);
            return string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase);
        }

        private Bitmap LoadGraphicTitleImageFromPackage(string normalizedPath)
        {
            List<string> candidates = BuildGraphicTitlePathCandidates(normalizedPath);
            for (int i = 0; i < candidates.Count; i++)
            {
                string relative = StripPackagePrefix(candidates[i], "surfaces");
                if (string.IsNullOrWhiteSpace(relative))
                {
                    continue;
                }

                byte[] payload;
                string error;
                if (!pckReader.TryReadFile("surfaces", relative, out payload, out error))
                {
                    continue;
                }

                Bitmap bitmap = TryLoadBitmapFromBytes(payload, relative);
                if (bitmap != null)
                {
                    return bitmap;
                }
            }

            return null;
        }

        private Bitmap TryLoadBitmapFromFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return null;
                }

                string extension = Path.GetExtension(filePath) ?? string.Empty;
                if (string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase))
                {
                    return tgaImageService.TryLoad(filePath);
                }
                if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    return DDS.LoadImage(filePath, true);
                }

                using (Bitmap source = new Bitmap(filePath))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }

        private Bitmap TryLoadBitmapFromBytes(byte[] payload, string relativePath)
        {
            try
            {
                if (payload == null || payload.Length == 0)
                {
                    return null;
                }

                string extension = Path.GetExtension(relativePath) ?? string.Empty;
                if (string.Equals(extension, ".tga", StringComparison.OrdinalIgnoreCase))
                {
                    return tgaImageService.TryLoad(payload);
                }
                if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    return DDS.LoadImage(payload, true);
                }

                using (MemoryStream stream = new MemoryStream(payload, false))
                using (Bitmap source = new Bitmap(stream))
                {
                    return new Bitmap(source);
                }
            }
            catch
            {
                return null;
            }
        }

        private bool TryImportGraphicTitleImage(string sourcePath, EditableTitleDefinition title, out string importedPath, out string error)
        {
            importedPath = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
            {
                error = "Source image not found.";
                return false;
            }

            string extension = Path.GetExtension(sourcePath) ?? string.Empty;
            Bitmap source = null;
            try
            {
                if (string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase))
                {
                    source = TryRenderSvgWithExternalConverter(sourcePath, out error);
                    if (source == null)
                    {
                        if (string.IsNullOrWhiteSpace(error))
                        {
                            error = "SVG import needs Inkscape or ImageMagick in PATH so transparency can be rendered correctly.";
                        }
                        return false;
                    }
                }
                else
                {
                    using (Bitmap loaded = new Bitmap(sourcePath))
                    {
                        source = new Bitmap(loaded);
                    }
                }

                using (source)
                {
                    Size targetSize = GetGraphicImportSize(source.Width, source.Height);
                    using (Bitmap converted = BuildTitleGraphicBitmap(source, targetSize.Width, targetSize.Height))
                    {
                        string relativePath = BuildImportedTitleGraphicPath(title, sourcePath);
                        string target = Path.Combine(
                            Path.GetTempPath(),
                            "FWEledit",
                            "title-import-cache",
                            relativePath);

                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                        WriteTga32(target, converted);
                        importedPath = NormalizeAssetPath(relativePath);
                        pendingSurfaceAssetFiles[importedPath] = target;

                        Bitmap old;
                        if (graphicTitleCache.TryGetValue(importedPath, out old) && old != null)
                        {
                            old.Dispose();
                        }
                        graphicTitleCache[importedPath] = new Bitmap(converted);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                if (source != null)
                {
                    source.Dispose();
                }

                error = ex.Message;
                return false;
            }
        }

        private Size GetGraphicImportSize(int sourceWidth, int sourceHeight)
        {
            GraphicSizePresetOption option = graphicSizePresetBox != null
                ? graphicSizePresetBox.SelectedItem as GraphicSizePresetOption
                : null;

            if (option == null || option.IsOriginal)
            {
                return new Size(Math.Max(1, sourceWidth), Math.Max(1, sourceHeight));
            }

            if (option.IsCustom)
            {
                return new Size(
                    Math.Max(1, Convert.ToInt32(graphicWidthBox.Value)),
                    Math.Max(1, Convert.ToInt32(graphicHeightBox.Value)));
            }

            return new Size(Math.Max(1, option.Width), Math.Max(1, option.Height));
        }

        private string GetGraphicImportSizeLabel()
        {
            GraphicSizePresetOption option = graphicSizePresetBox != null
                ? graphicSizePresetBox.SelectedItem as GraphicSizePresetOption
                : null;

            if (option == null)
            {
                return "Original image size";
            }

            if (option.IsOriginal)
            {
                return option.Label;
            }

            int width = option.IsCustom ? Convert.ToInt32(graphicWidthBox.Value) : option.Width;
            int height = option.IsCustom ? Convert.ToInt32(graphicHeightBox.Value) : option.Height;
            return width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
        }

        private static Bitmap BuildTitleGraphicBitmap(Bitmap source, int targetWidth, int targetHeight)
        {
            Bitmap result = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);
                graphics.CompositingMode = CompositingMode.SourceOver;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                float scale = Math.Min((float)targetWidth / source.Width, (float)targetHeight / source.Height);
                int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
                int x = (targetWidth - drawWidth) / 2;
                int y = (targetHeight - drawHeight) / 2;
                graphics.DrawImage(source, new Rectangle(x, y, drawWidth, drawHeight));
            }

            return result;
        }

        private static string BuildImportedTitleGraphicPath(EditableTitleDefinition title, string sourcePath)
        {
            string name = title != null && !string.IsNullOrWhiteSpace(title.TitleText)
                ? title.TitleText
                : Path.GetFileNameWithoutExtension(sourcePath);
            string safeName = MakeSafeAssetName(name);
            string id = title != null ? title.Id.ToString(CultureInfo.InvariantCulture) : DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            return Path.Combine("sm", "smtitle", "fweledit_" + id + "_" + safeName + ".tga");
        }

        private static string MakeSafeAssetName(string value)
        {
            string safe = Regex.Replace(value ?? string.Empty, "[^A-Za-z0-9_-]+", "_").Trim('_');
            if (string.IsNullOrWhiteSpace(safe))
            {
                safe = "title";
            }
            if (safe.Length > 40)
            {
                safe = safe.Substring(0, 40).Trim('_');
            }
            return safe.ToLowerInvariant();
        }

        private static void WriteTga32(string path, Bitmap bitmap)
        {
            using (FileStream stream = File.Create(path))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((byte)2);
                writer.Write(new byte[5]);
                writer.Write((short)0);
                writer.Write((short)0);
                writer.Write((ushort)bitmap.Width);
                writer.Write((ushort)bitmap.Height);
                writer.Write((byte)32);
                writer.Write((byte)0x28);

                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        writer.Write(color.B);
                        writer.Write(color.G);
                        writer.Write(color.R);
                        writer.Write(color.A);
                    }
                }
            }
        }

        private static Bitmap TryRenderSvgWithExternalConverter(string sourcePath, out string error)
        {
            error = string.Empty;
            string tempPng = Path.Combine(Path.GetTempPath(), "fweledit_title_" + Guid.NewGuid().ToString("N") + ".png");
            try
            {
                string converter = FindExecutable("inkscape.exe");
                if (!string.IsNullOrWhiteSpace(converter))
                {
                    if (RunConverter(converter, "\"" + sourcePath + "\" --export-type=png --export-filename=\"" + tempPng + "\" --export-background-opacity=0"))
                    {
                        using (Bitmap loaded = new Bitmap(tempPng))
                        {
                            return new Bitmap(loaded);
                        }
                    }
                }

                converter = FindExecutable("magick.exe");
                if (!string.IsNullOrWhiteSpace(converter))
                {
                    if (RunConverter(converter, "-background none \"" + sourcePath + "\" \"" + tempPng + "\""))
                    {
                        using (Bitmap loaded = new Bitmap(tempPng))
                        {
                            return new Bitmap(loaded);
                        }
                    }
                }

                error = "SVG import needs Inkscape or ImageMagick in PATH so transparency can be rendered correctly.";
                return null;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPng))
                    {
                        File.Delete(tempPng);
                    }
                }
                catch
                {
                }
            }
        }

        private static string FindExecutable(string executable)
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] folders = path.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < folders.Length; i++)
            {
                string candidate = Path.Combine(folders[i], executable);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static bool RunConverter(string executable, string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = executable;
            psi.Arguments = arguments;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            using (Process process = Process.Start(psi))
            {
                if (process == null)
                {
                    return false;
                }

                return process.WaitForExit(30000) && process.ExitCode == 0;
            }
        }

        private static List<string> BuildGraphicTitlePathCandidates(string path)
        {
            List<string> result = new List<string>();
            string normalized = NormalizeAssetPath(path);
            AddPathCandidate(result, normalized);

            string extension = Path.GetExtension(normalized);
            if (string.IsNullOrWhiteSpace(extension))
            {
                AddPathCandidate(result, normalized + ".tga");
                AddPathCandidate(result, normalized + ".dds");
                AddPathCandidate(result, normalized + ".png");
                AddPathCandidate(result, normalized + ".jpg");
                AddPathCandidate(result, normalized + ".bmp");
            }

            return result;
        }

        private static void AddPathCandidate(List<string> result, string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }
            if (!result.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(candidate);
            }
        }

        private static string NormalizeAssetPath(string path)
        {
            string normalized = (path ?? string.Empty).Trim().TrimStart('\\', '/').Replace('/', '\\');
            while (normalized.Contains("\\\\"))
            {
                normalized = normalized.Replace("\\\\", "\\");
            }
            return normalized;
        }

        private static string StripPackagePrefix(string path, string packageName)
        {
            string normalized = NormalizeAssetPath(path);
            string prefix = (packageName ?? string.Empty).Trim('\\', '/') + "\\";
            if (!string.IsNullOrWhiteSpace(prefix) && normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(prefix.Length);
            }

            return normalized;
        }

        private void MarkDirty()
        {
            dirty = true;
            statusLabel.Text = "Pending changes";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (dirty)
            {
                DialogResult result = MessageBox.Show(this, "There are pending title changes. Close without saving?", "Advanced Title Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (graphicTitlePictureBox != null && graphicTitlePictureBox.Image != null)
            {
                Image previous = graphicTitlePictureBox.Image;
                graphicTitlePictureBox.Image = null;
                previous.Dispose();
            }

            ClearGraphicTitleCache();

            base.OnClosed(e);
        }

        private void ClearGraphicTitleCache()
        {
            foreach (Bitmap bitmap in graphicTitleCache.Values)
            {
                if (bitmap != null)
                {
                    bitmap.Dispose();
                }
            }
            graphicTitleCache.Clear();
        }

        private void HookEditorChangedEvents()
        {
            idBox.ValueChanged += editorValueChanged;
            categoryBox.SelectedIndexChanged += editorValueChanged;
            nameBox.TextChanged += editorValueChanged;
            graphicTitleCheckBox.CheckedChanged += editorValueChanged;
            graphicChatCheckBox.CheckedChanged += editorValueChanged;
            iconPathBox.TextChanged += editorValueChanged;
            descriptionBox.TextChanged += editorValueChanged;
            for (int i = 0; i < bonusBoxes.Length; i++)
            {
                bonusBoxes[i].TextChanged += editorValueChanged;
            }
        }

        private void InitializeColorOptions()
        {
            colorOptions.Clear();
            AddColor("FFFFFF", "FFFFFF - White");
            AddColor("006EFF", "006EFF - Blue");
            AddColor("00FF00", "00FF00 - Green");
            AddColor("00FFFF", "00FFFF - Cyan");
            AddColor("1EB0FF", "1EB0FF - Sky");
            AddColor("93DF00", "93DF00 - Lime");
            AddColor("9800CD", "9800CD - Purple");
            AddColor("A1A3A4", "A1A3A4 - Silver");
            AddColor("B468FF", "B468FF - Violet");
            AddColor("E319D1", "E319D1 - Pink");
            AddColor("F7CC00", "F7CC00 - Gold");
            AddColor("FA800A", "FA800A - Orange");
            AddColor("FF0000", "FF0000 - Red");
            AddColor("FF7CC3", "FF7CC3 - Rose");
            AddColor("FF8400", "FF8400 - Amber");
            AddColor("FFBC3C", "FFBC3C - Warm Gold");
            colorBox.DataSource = colorOptions;
            colorBox.DisplayMember = "Label";
            colorBox.ValueMember = "Hex";
        }

        private void InitializeGraphicSizeOptions()
        {
            List<GraphicSizePresetOption> options = new List<GraphicSizePresetOption>
            {
                new GraphicSizePresetOption { Label = "Original image size", Width = 0, Height = 0, IsOriginal = true },
                new GraphicSizePresetOption { Label = "32 x 16 - Tiny", Width = 32, Height = 16 },
                new GraphicSizePresetOption { Label = "40 x 20 - Very small", Width = 40, Height = 20 },
                new GraphicSizePresetOption { Label = "60 x 30 - Small", Width = 60, Height = 30 },
                new GraphicSizePresetOption { Label = "80 x 40 - Common", Width = 80, Height = 40 },
                new GraphicSizePresetOption { Label = "120 x 60 - Medium", Width = 120, Height = 60 },
                new GraphicSizePresetOption { Label = "151 x 86 - Wide title", Width = 151, Height = 86 },
                new GraphicSizePresetOption { Label = "160 x 80 - Large", Width = 160, Height = 80 },
                new GraphicSizePresetOption { Label = "200 x 100 - Extra large", Width = 200, Height = 100 },
                new GraphicSizePresetOption { Label = "Custom size", Width = 80, Height = 40, IsCustom = true }
            };

            graphicSizePresetBox.DataSource = options;
            graphicSizePresetBox.DisplayMember = "Label";
            graphicSizePresetBox.ValueMember = "Label";
            graphicSizePresetBox.SelectedIndex = 0;
            UpdateGraphicSizeControls();
        }

        private void UpdateGraphicSizeControls()
        {
            GraphicSizePresetOption option = graphicSizePresetBox != null
                ? graphicSizePresetBox.SelectedItem as GraphicSizePresetOption
                : null;

            bool isCustom = option != null && option.IsCustom;
            graphicWidthBox.Enabled = isCustom;
            graphicHeightBox.Enabled = isCustom;

            if (option != null && !option.IsOriginal && !option.IsCustom)
            {
                graphicWidthBox.Value = Math.Max(graphicWidthBox.Minimum, Math.Min(graphicWidthBox.Maximum, option.Width));
                graphicHeightBox.Value = Math.Max(graphicHeightBox.Minimum, Math.Min(graphicHeightBox.Maximum, option.Height));
            }
        }

        private void AddColor(string hex, string label)
        {
            Color color;
            colorOptions.Add(new ColorOption
            {
                Hex = hex,
                Label = label,
                Color = TryParseHexColor(hex, out color) ? color : Color.White
            });
        }

        private string GetSelectedColorHex()
        {
            ColorOption option = colorBox.SelectedItem as ColorOption;
            return option != null ? option.Hex : string.Empty;
        }

        private void SelectColor(string hex)
        {
            string normalized = NormalizeHex(hex);
            for (int i = 0; i < colorOptions.Count; i++)
            {
                if (string.Equals(colorOptions[i].Hex, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    colorBox.SelectedIndex = i;
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(normalized))
            {
                Color color;
                colorOptions.Add(new ColorOption
                {
                    Hex = normalized,
                    Label = normalized + " - Custom",
                    Color = TryParseHexColor(normalized, out color) ? color : Color.White
                });
                colorBox.DataSource = null;
                colorBox.DataSource = colorOptions;
                colorBox.DisplayMember = "Label";
                colorBox.ValueMember = "Hex";
                colorBox.SelectedIndex = colorOptions.Count - 1;
                return;
            }

            colorBox.SelectedIndex = colorOptions.Count > 0 ? 0 : -1;
        }

        private EditableTitleDefinition GetSelectedTitle()
        {
            if (titleGrid.SelectedRows.Count == 0)
            {
                return null;
            }

            return titleGrid.SelectedRows[0].Tag as EditableTitleDefinition;
        }

        private int GetSelectedTitleId()
        {
            EditableTitleDefinition title = GetSelectedTitle();
            return title != null ? title.Id : 0;
        }

        private void SelectTitle(int id)
        {
            for (int i = 0; i < titleGrid.Rows.Count; i++)
            {
                EditableTitleDefinition title = titleGrid.Rows[i].Tag as EditableTitleDefinition;
                if (title != null && title.Id == id)
                {
                    titleGrid.ClearSelection();
                    titleGrid.Rows[i].Selected = true;
                    titleGrid.CurrentCell = titleGrid.Rows[i].Cells[0];
                    return;
                }
            }
        }

        private void RebuildCategoryGrid()
        {
            if (categoryGrid == null)
            {
                return;
            }

            categoryGrid.Rows.Clear();
            for (int i = 0; i < categoryOptions.Count; i++)
            {
                TitleCategoryOption category = categoryOptions[i];
                if (category == null || string.IsNullOrWhiteSpace(category.PathKey))
                {
                    continue;
                }

                int rowIndex = categoryGrid.Rows.Add(
                    category.Display ?? string.Empty,
                    category.TitleCount.ToString(CultureInfo.InvariantCulture));
                categoryGrid.Rows[rowIndex].Tag = category;
            }
        }

        private string GetSelectedCategoryPath()
        {
            TitleCategoryOption category = categoryBox != null ? categoryBox.SelectedItem as TitleCategoryOption : null;
            return category != null ? category.PathKey ?? string.Empty : string.Empty;
        }

        private string GetSelectedCategoryDisplay()
        {
            TitleCategoryOption category = categoryBox != null ? categoryBox.SelectedItem as TitleCategoryOption : null;
            return category != null ? category.Display ?? string.Empty : string.Empty;
        }

        private void SelectCategory(string pathKey)
        {
            string normalized = pathKey ?? string.Empty;
            for (int i = 0; i < categoryOptions.Count; i++)
            {
                if (string.Equals(categoryOptions[i].PathKey ?? string.Empty, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    categoryBox.SelectedIndex = i;
                    return;
                }
            }

            categoryBox.SelectedIndex = categoryOptions.Count > 0 ? 0 : -1;
        }

        private static EditableTitleDefinition NormalizeTitle(EditableTitleDefinition title)
        {
            EditableTitleDefinition normalized = CloneTitle(title);
            if (normalized.AddonDescriptions == null || normalized.AddonDescriptions.Length < 5)
            {
                string[] addons = new string[5];
                if (normalized.AddonDescriptions != null)
                {
                    Array.Copy(normalized.AddonDescriptions, addons, Math.Min(normalized.AddonDescriptions.Length, addons.Length));
                }
                normalized.AddonDescriptions = addons;
            }
            normalized.AccentHex = NormalizeHex(normalized.AccentHex);
            return normalized;
        }

        private static EditableTitleDefinition CloneTitle(EditableTitleDefinition title)
        {
            if (title == null)
            {
                return new EditableTitleDefinition { AddonDescriptions = new string[5] };
            }

            return new EditableTitleDefinition
            {
                Id = title.Id,
                TitleText = title.TitleText ?? string.Empty,
                AccentHex = NormalizeHex(title.AccentHex),
                Description = title.Description ?? string.Empty,
                AddonDescriptions = title.AddonDescriptions != null ? (string[])title.AddonDescriptions.Clone() : new string[5],
                IconPath = title.IconPath ?? string.Empty,
                IsGraphicTitle = title.IsGraphicTitle,
                ShowGraphicInChat = title.ShowGraphicInChat,
                CategoryPathKey = title.CategoryPathKey ?? string.Empty,
                CategoryDisplay = title.CategoryDisplay ?? string.Empty
            };
        }

        private static void CopyTitle(EditableTitleDefinition source, EditableTitleDefinition target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.Id = source.Id;
            target.TitleText = source.TitleText;
            target.AccentHex = source.AccentHex;
            target.Description = source.Description;
            target.AddonDescriptions = source.AddonDescriptions != null ? (string[])source.AddonDescriptions.Clone() : new string[5];
            target.IconPath = source.IconPath;
            target.IsGraphicTitle = source.IsGraphicTitle;
            target.ShowGraphicInChat = source.ShowGraphicInChat;
            target.CategoryPathKey = source.CategoryPathKey;
            target.CategoryDisplay = source.CategoryDisplay;
        }

        private static string NormalizeHex(string value)
        {
            string hex = (value ?? string.Empty).Trim().TrimStart('#', '^');
            return hex.Length == 6 ? hex.ToUpperInvariant() : string.Empty;
        }

        private static bool TryParseHexColor(string hex, out Color color)
        {
            color = Color.White;
            string value = NormalizeHex(hex);
            if (value.Length != 6)
            {
                return false;
            }

            int number;
            if (!int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number))
            {
                return false;
            }

            color = Color.FromArgb((number >> 16) & 255, (number >> 8) & 255, number & 255);
            return true;
        }

        private static string CleanDisplayText(string value)
        {
            string text = RemoveColorCodes(value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length > 120 ? text.Substring(0, 120) + "..." : text;
        }

        private static string RemoveColorCodes(string value)
        {
            return ColorCodeRegex.Replace(value ?? string.Empty, string.Empty).Replace("\\n", Environment.NewLine);
        }

        private static void RenderColoredText(RichTextBox target, string rawText)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();
            string text = (rawText ?? string.Empty).Replace("\\r", Environment.NewLine).Replace("\\n", Environment.NewLine);
            Color currentColor = Color.White;
            int position = 0;
            while (position < text.Length)
            {
                Match match = ColorCodeRegex.Match(text, position);
                if (!match.Success)
                {
                    AppendColoredText(target, text.Substring(position), currentColor);
                    break;
                }

                if (match.Index > position)
                {
                    AppendColoredText(target, text.Substring(position, match.Index - position), currentColor);
                }

                Color parsed;
                if (TryParseHexColor(match.Value, out parsed))
                {
                    currentColor = parsed;
                }
                position = match.Index + match.Length;
            }
        }

        private static void AppendColoredText(RichTextBox target, string text, Color color)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            target.SelectionStart = target.TextLength;
            target.SelectionLength = 0;
            target.SelectionColor = color;
            target.AppendText(text);
            target.SelectionColor = target.ForeColor;
        }

        private static Color EnsureReadableOnDark(Color color)
        {
            double luminance = GetLuminance(color);
            if (luminance >= 85)
            {
                return color;
            }

            int red = Math.Min(255, color.R + 90);
            int green = Math.Min(255, color.G + 90);
            int blue = Math.Min(255, color.B + 90);
            return Color.FromArgb(red, green, blue);
        }

        private static Color EnsureReadableOnAccent(Color color)
        {
            return GetLuminance(color) < 110 ? Color.White : color;
        }

        private static double GetLuminance(Color color)
        {
            return (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
        }

        private static Label BuildLabel(string text)
        {
            Label label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Color.White;
            return label;
        }

        private static TextBox BuildTextBox()
        {
            TextBox textBox = new TextBox();
            textBox.Dock = DockStyle.Fill;
            textBox.BorderStyle = BorderStyle.FixedSingle;
            textBox.BackColor = Color.FromArgb(18, 23, 29);
            textBox.ForeColor = Color.White;
            return textBox;
        }

        private Control BuildIconPathEditor()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0);
            panel.Padding = new Padding(0);
            panel.ColumnCount = 3;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.BackColor = Color.FromArgb(15, 19, 24);

            iconPathBox.Margin = new Padding(0);
            panel.Controls.Add(iconPathBox, 0, 0);
            panel.Controls.Add(iconPathPickerButton, 1, 0);
            panel.Controls.Add(iconPathImportButton, 2, 0);
            return panel;
        }

        private Control BuildGraphicImportSizeEditor()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0);
            panel.Padding = new Padding(0);
            panel.ColumnCount = 5;
            panel.RowCount = 1;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 28F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.BackColor = Color.FromArgb(15, 19, 24);

            Label widthLabel = BuildLabel("W:");
            Label heightLabel = BuildLabel("H:");
            widthLabel.TextAlign = ContentAlignment.MiddleRight;
            heightLabel.TextAlign = ContentAlignment.MiddleRight;

            graphicSizePresetBox.Margin = new Padding(0);
            graphicWidthBox.Margin = new Padding(4, 0, 4, 0);
            graphicHeightBox.Margin = new Padding(4, 0, 0, 0);

            panel.Controls.Add(graphicSizePresetBox, 0, 0);
            panel.Controls.Add(widthLabel, 1, 0);
            panel.Controls.Add(graphicWidthBox, 2, 0);
            panel.Controls.Add(heightLabel, 3, 0);
            panel.Controls.Add(graphicHeightBox, 4, 0);
            return panel;
        }

        private static Button BuildButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Height = 26;
            button.Margin = new Padding(4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(38, 46, 57);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(116, 131, 151);
            return button;
        }

        private static NumericUpDown BuildNumericBox(decimal minimum, decimal maximum)
        {
            NumericUpDown box = new NumericUpDown();
            box.Dock = DockStyle.Fill;
            box.Minimum = minimum;
            box.Maximum = maximum;
            box.DecimalPlaces = 0;
            box.ThousandsSeparator = true;
            box.BackColor = Color.FromArgb(18, 23, 29);
            box.ForeColor = Color.White;
            return box;
        }

        private static GroupBox BuildGroup(string text)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Dock = DockStyle.Fill;
            group.ForeColor = Color.FromArgb(180, 213, 245);
            group.BackColor = Color.FromArgb(15, 19, 24);
            return group;
        }

        private static void ResizeMainSplit(SplitContainer split)
        {
            if (split == null || split.Width <= 0)
            {
                return;
            }

            int desiredPanel1Min = 340;
            int desiredPanel2Min = 660;
            int available = split.Width - split.SplitterWidth;
            if (available <= 0)
            {
                return;
            }

            int panel1Min = Math.Min(desiredPanel1Min, Math.Max(20, available / 2));
            int panel2Min = Math.Min(desiredPanel2Min, Math.Max(20, available - panel1Min));
            if (panel1Min + panel2Min > available)
            {
                panel2Min = Math.Max(20, available - panel1Min);
            }

            if (panel2Min <= 0 || panel1Min <= 0 || panel1Min + panel2Min > available)
            {
                return;
            }

            split.Panel1MinSize = panel1Min;
            split.Panel2MinSize = panel2Min;

            int preferredPanel2Width = Math.Min(860, Math.Max(panel2Min, available / 2));
            int desiredDistance = available - preferredPanel2Width;
            int minDistance = split.Panel1MinSize;
            int maxDistance = split.Width - split.Panel2MinSize - split.SplitterWidth;
            if (maxDistance < minDistance)
            {
                return;
            }

            int distance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));
            if (split.SplitterDistance != distance)
            {
                split.SplitterDistance = distance;
            }
        }

        private static void AddEditorRow(TableLayoutPanel layout, int row, string labelText, Control editor)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, string.Equals(labelText, "Description:", StringComparison.Ordinal) ? 96 : 28));
            Label label = BuildLabel(labelText);
            label.Margin = new Padding(8, 2, 4, 2);
            editor.Margin = new Padding(4, 2, 8, 2);
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(editor, 1, row);
        }

        private sealed class GraphicTitlePreviewBox : Control
        {
            private Image image;

            public Image Image
            {
                get { return image; }
                set
                {
                    image = value;
                    Invalidate();
                }
            }

            public GraphicTitlePreviewBox()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.Clear(BackColor);

                if (image == null || image.Width <= 0 || image.Height <= 0)
                {
                    using (Brush brush = new SolidBrush(Color.FromArgb(130, 148, 170)))
                    {
                        string text = "No graphic title";
                        SizeF size = e.Graphics.MeasureString(text, Font);
                        e.Graphics.DrawString(text, Font, brush, (Width - size.Width) / 2F, (Height - size.Height) / 2F);
                    }
                    return;
                }

                Rectangle bounds = ClientRectangle;
                bounds.Inflate(-10, -10);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                {
                    return;
                }

                float scale = Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);

                Rectangle destination = new Rectangle(
                    bounds.Left + (int)Math.Round((bounds.Width - image.Width * scale) / 2F),
                    bounds.Top + (int)Math.Round((bounds.Height - image.Height * scale) / 2F),
                    Math.Max(1, (int)Math.Round(image.Width * scale)),
                    Math.Max(1, (int)Math.Round(image.Height * scale)));

                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                e.Graphics.DrawImage(image, destination);
            }
        }

        private sealed class ColorOption
        {
            public string Hex { get; set; }
            public string Label { get; set; }
            public Color Color { get; set; }
        }

        private sealed class GraphicSizePresetOption
        {
            public string Label { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public bool IsOriginal { get; set; }
            public bool IsCustom { get; set; }
        }
    }
}

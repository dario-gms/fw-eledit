using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed partial class IconPickerWindow : Form
    {
        public static void BeginBackgroundWarmup(CacheSave db)
        {
            // Atlas mode is instant and does not require thumbnail warmup.
        }


        public static void CancelBackgroundWarmup()
        {
            // Atlas mode is instant and does not require thumbnail warmup.
        }


        public IconPickerWindow(CacheSave db, int currentPathId, Dictionary<int, int> usageLookup)
        {
            database = db;
            viewModel = new IconPickerViewModel(new IconPickerService());
            viewModel.Load(database, usageLookup ?? new Dictionary<int, int>(), currentPathId);

            InitializeUi();
            Load += IconPickerWindow_Load;
            KeyPreview = true;
            KeyDown += IconPickerWindow_KeyDown;
        }


        private void InitializeUi()
        {
            Text = "Image Picker [ESC - Cancel]";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Normal;
            Size = new Size(1280, 820);
            MinimumSize = new Size(900, 600);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            Controls.Add(root);

            Panel top = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(top, 0, 0);

            Label searchLabel = new Label { Text = "Search:", AutoSize = true, Location = new Point(8, 9) };
            top.Controls.Add(searchLabel);

            searchBox = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(62, 6),
                Width = 450
            };
            searchBox.TextChanged += searchBox_TextChanged;
            top.Controls.Add(searchBox);

            grid = new AtlasGrid { Dock = DockStyle.Fill };
            grid.SelectedIndexChanged += grid_SelectedIndexChanged;
            grid.ItemDoubleClick += grid_ItemDoubleClick;
            grid.SetTooltipFactory(BuildTooltip);
            root.Controls.Add(grid, 0, 1);

            Panel bottom = new Panel { Dock = DockStyle.Fill };
            root.Controls.Add(bottom, 0, 2);

            statusLabel = new Label { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            bottom.Controls.Add(statusLabel);

            okButton = new Button { Text = "OK", Width = 90, Dock = DockStyle.Right };
            okButton.Click += okButton_Click;
            bottom.Controls.Add(okButton);

            cancelButton = new Button { Text = "Cancel", Width = 90, Dock = DockStyle.Right };
            cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(cancelButton);
        }


        private void IconPickerWindow_Load(object sender, EventArgs e)
        {
            InitializeEntries();
            ApplyFilter();
            SelectPending();
            searchBox.Focus();
        }


        private void InitializeEntries()
        {
            iconPickerCoordinatorService.InitializeEntries(viewModel, grid, statusLabel);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class GameShopIconPickerWindow : Form
    {
        private readonly List<GameShopIconOption> allOptions;
        private readonly bool darkMode;
        private readonly TextBox searchBox;
        private readonly DataGridView grid;
        private readonly PictureBox previewBox;
        private readonly Label previewLabel;
        private readonly Button okButton;

        public int SelectedPathId { get; private set; }

        public GameShopIconPickerWindow(List<GameShopIconOption> options, int currentPathId, bool darkMode)
        {
            allOptions = options ?? new List<GameShopIconOption>();
            SelectedPathId = currentPathId;
            this.darkMode = darkMode;

            Text = "Choose GShop icon...";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 520);
            Size = new Size(900, 680);
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += HandleKeyDown;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.ColumnCount = 2;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            searchBox = new TextBox();
            searchBox.Dock = DockStyle.Fill;
            searchBox.Margin = new Padding(0, 0, 8, 8);
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.TextChanged += (sender, args) => LoadRows();

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.Margin = new Padding(0, 0, 8, 8);
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.AutoGenerateColumns = false;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.RowTemplate.Height = 88;
            grid.ColumnHeadersHeight = 26;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.Columns.Add(new DataGridViewImageColumn { Name = "icon", HeaderText = string.Empty, Width = 88, ImageLayout = DataGridViewImageCellLayout.Normal });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "pathId", HeaderText = "PathID", Width = 76 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "fileName", HeaderText = "File", Width = 210 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "path", HeaderText = "Path", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 240 });
            grid.SelectionChanged += (sender, args) => RefreshPreview();
            grid.CellDoubleClick += (sender, args) =>
            {
                if (args.RowIndex >= 0)
                {
                    ConfirmSelection();
                }
            };

            Panel previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Margin = new Padding(0, 0, 0, 8);

            previewBox = new PictureBox();
            previewBox.Dock = DockStyle.Top;
            previewBox.Height = 96;
            previewBox.SizeMode = PictureBoxSizeMode.CenterImage;
            previewBox.Margin = new Padding(0, 0, 0, 8);

            previewLabel = new Label();
            previewLabel.Dock = DockStyle.Fill;
            previewLabel.TextAlign = ContentAlignment.TopLeft;

            previewPanel.Controls.Add(previewLabel);
            previewPanel.Controls.Add(previewBox);

            okButton = CreateButton("OK");
            okButton.Click += (sender, args) => ConfirmSelection();
            Button cancelButton = CreateButton("Cancel");
            cancelButton.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.FlowDirection = FlowDirection.RightToLeft;
            footer.Controls.Add(cancelButton);
            footer.Controls.Add(okButton);

            layout.Controls.Add(searchBox, 0, 0);
            layout.SetColumnSpan(searchBox, 2);
            layout.Controls.Add(grid, 0, 1);
            layout.Controls.Add(previewPanel, 1, 1);
            layout.Controls.Add(footer, 0, 2);
            layout.SetColumnSpan(footer, 2);
            Controls.Add(layout);

            ApplyTheme(layout, previewPanel, footer, cancelButton);
            LoadRows();
            SelectCurrent();

            Shown += (sender, args) => searchBox.Focus();
        }

        private void LoadRows()
        {
            string search = (searchBox.Text ?? string.Empty).Trim();
            string[] terms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            IEnumerable<GameShopIconOption> filtered = allOptions;
            if (terms.Length > 0)
            {
                filtered = filtered.Where(option => Matches(option, terms));
            }

            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                foreach (GameShopIconOption option in filtered)
                {
                    int rowIndex = grid.Rows.Add(
                        option.Image ?? Properties.Resources.NoIcon,
                        option.PathId.ToString(),
                        option.FileName ?? string.Empty,
                        option.Path ?? string.Empty);
                    grid.Rows[rowIndex].Tag = option;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }

            SelectCurrent();
            RefreshPreview();
        }

        private static bool Matches(GameShopIconOption option, string[] terms)
        {
            if (option == null)
            {
                return false;
            }

            string haystack = string.Join(" ", new[]
            {
                option.PathId.ToString(),
                option.FileName ?? string.Empty,
                option.Path ?? string.Empty
            }).ToLowerInvariant();

            for (int i = 0; i < terms.Length; i++)
            {
                if (!haystack.Contains(terms[i].ToLowerInvariant()))
                {
                    return false;
                }
            }

            return true;
        }

        private void SelectCurrent()
        {
            if (grid.Rows.Count == 0)
            {
                okButton.Enabled = false;
                return;
            }

            okButton.Enabled = true;
            for (int i = 0; i < grid.Rows.Count; i++)
            {
                GameShopIconOption option = grid.Rows[i].Tag as GameShopIconOption;
                if (option != null && option.PathId == SelectedPathId)
                {
                    grid.ClearSelection();
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells["fileName"];
                    return;
                }
            }

            if (grid.SelectedRows.Count == 0)
            {
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells["fileName"];
            }
        }

        private void RefreshPreview()
        {
            GameShopIconOption option = SelectedOption;
            if (option == null)
            {
                previewBox.Image = Properties.Resources.NoIcon;
                previewLabel.Text = "No icon selected.";
                return;
            }

            previewBox.Image = option.Image ?? Properties.Resources.NoIcon;
            previewLabel.Text = "PathID: " + option.PathId + Environment.NewLine
                + (option.FileName ?? string.Empty) + Environment.NewLine
                + (option.Path ?? string.Empty);
        }

        private GameShopIconOption SelectedOption
        {
            get
            {
                if (grid.SelectedRows.Count == 0)
                {
                    return null;
                }

                return grid.SelectedRows[0].Tag as GameShopIconOption;
            }
        }

        private void ConfirmSelection()
        {
            GameShopIconOption option = SelectedOption;
            if (option == null)
            {
                return;
            }

            SelectedPathId = option.PathId;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Enter && !(ActiveControl is TextBox))
            {
                ConfirmSelection();
                e.SuppressKeyPress = true;
            }
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 96;
            button.Height = 30;
            button.Margin = new Padding(8, 4, 0, 4);
            button.FlatStyle = FlatStyle.Flat;
            return button;
        }

        private void ApplyTheme(params Control[] roots)
        {
            Color back = darkMode ? Color.FromArgb(17, 20, 24) : SystemColors.Control;
            Color panelBack = darkMode ? Color.FromArgb(15, 18, 22) : Color.White;
            Color editorBack = darkMode ? Color.FromArgb(28, 32, 37) : Color.White;
            Color fore = darkMode ? Color.White : SystemColors.ControlText;
            Color muted = darkMode ? Color.FromArgb(204, 214, 226) : SystemColors.ControlText;
            Color gridLine = darkMode ? Color.FromArgb(38, 45, 54) : SystemColors.ControlDark;
            Color selection = darkMode ? Color.FromArgb(65, 104, 142) : SystemColors.Highlight;

            BackColor = back;
            ForeColor = fore;
            searchBox.BackColor = editorBack;
            searchBox.ForeColor = fore;
            previewLabel.ForeColor = muted;
            previewBox.BackColor = panelBack;

            grid.BackgroundColor = panelBack;
            grid.GridColor = gridLine;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.DefaultCellStyle.BackColor = panelBack;
            grid.DefaultCellStyle.ForeColor = fore;
            grid.DefaultCellStyle.SelectionBackColor = selection;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = darkMode ? Color.FromArgb(20, 24, 29) : Color.FromArgb(246, 248, 250);
            grid.ColumnHeadersDefaultCellStyle.BackColor = darkMode ? Color.FromArgb(35, 41, 50) : SystemColors.Control;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = fore;
            grid.EnableHeadersVisualStyles = false;

            for (int i = 0; i < roots.Length; i++)
            {
                ApplyThemeRecursive(roots[i], back, panelBack, fore);
            }
        }

        private void ApplyThemeRecursive(Control control, Color back, Color panelBack, Color fore)
        {
            if (control == null)
            {
                return;
            }

            control.ForeColor = fore;
            if (control is TextBox)
            {
                control.BackColor = darkMode ? Color.FromArgb(28, 32, 37) : Color.White;
            }
            else if (control is DataGridView || control is PictureBox)
            {
                control.BackColor = panelBack;
            }
            else if (control is Button)
            {
                control.BackColor = darkMode ? Color.FromArgb(44, 54, 72) : SystemColors.Control;
            }
            else
            {
                control.BackColor = back;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeRecursive(child, back, panelBack, fore);
            }
        }
    }
}

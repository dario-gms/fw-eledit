using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class TitleGraphicIconOption
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public string Source { get; set; }
    }

    public sealed class TitleGraphicIconPickerWindow : Form
    {
        private readonly List<TitleGraphicIconOption> allOptions;
        private readonly Func<string, Bitmap> imageLoader;
        private readonly Dictionary<string, Bitmap> thumbnailCache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        private readonly string currentPath;
        private readonly TextBox searchBox;
        private readonly DataGridView grid;
        private readonly PictureBox previewBox;
        private readonly Label previewLabel;
        private readonly Button okButton;

        public string SelectedPath { get; private set; }

        public TitleGraphicIconPickerWindow(
            List<TitleGraphicIconOption> options,
            string currentPath,
            Func<string, Bitmap> imageLoader)
        {
            allOptions = options ?? new List<TitleGraphicIconOption>();
            this.currentPath = NormalizePath(currentPath);
            SelectedPath = this.currentPath;
            this.imageLoader = imageLoader;

            Text = "Choose title graphic...";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(820, 560);
            Size = new Size(1040, 720);
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += HandleKeyDown;

            Color back = Color.FromArgb(15, 19, 24);
            Color panel = Color.FromArgb(18, 23, 29);
            Color raised = Color.FromArgb(38, 46, 57);
            Color text = Color.White;
            Color accent = Color.FromArgb(83, 151, 213);

            BackColor = back;
            ForeColor = text;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(10);
            root.ColumnCount = 2;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            root.BackColor = back;
            Controls.Add(root);

            searchBox = new TextBox();
            searchBox.Dock = DockStyle.Fill;
            searchBox.Margin = new Padding(0, 0, 8, 8);
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.BackColor = panel;
            searchBox.ForeColor = text;
            searchBox.TextChanged += delegate { LoadRows(); };
            root.Controls.Add(searchBox, 0, 0);
            root.SetColumnSpan(searchBox, 2);

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
            grid.RowTemplate.Height = 58;
            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            grid.ColumnHeadersHeight = 26;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.BackgroundColor = panel;
            grid.GridColor = Color.FromArgb(43, 51, 61);
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = raised;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = text;
            grid.DefaultCellStyle.BackColor = panel;
            grid.DefaultCellStyle.ForeColor = text;
            grid.DefaultCellStyle.SelectionBackColor = accent;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(21, 26, 32);
            grid.Columns.Add(new DataGridViewImageColumn { Name = "Graphic", HeaderText = "Graphic", Width = 74, ImageLayout = DataGridViewImageCellLayout.Zoom });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "File", HeaderText = "File", Width = 210 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Path", HeaderText = "Path", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 260 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Source", HeaderText = "Source", Width = 100 });
            grid.SelectionChanged += delegate { RefreshPreview(); };
            grid.CellDoubleClick += delegate(object sender, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0)
                {
                    ConfirmSelection();
                }
            };
            root.Controls.Add(grid, 0, 1);

            Panel previewPanel = new Panel();
            previewPanel.Dock = DockStyle.Fill;
            previewPanel.Margin = new Padding(0, 0, 0, 8);
            previewPanel.BackColor = panel;
            previewPanel.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(previewPanel, 1, 1);

            TableLayoutPanel previewLayout = new TableLayoutPanel();
            previewLayout.Dock = DockStyle.Fill;
            previewLayout.Padding = new Padding(8);
            previewLayout.RowCount = 2;
            previewLayout.ColumnCount = 1;
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            previewLayout.BackColor = panel;
            previewPanel.Controls.Add(previewLayout);

            previewBox = new PictureBox();
            previewBox.Dock = DockStyle.Fill;
            previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewBox.BackColor = Color.FromArgb(12, 15, 20);
            previewLayout.Controls.Add(previewBox, 0, 0);

            previewLabel = new Label();
            previewLabel.Dock = DockStyle.Fill;
            previewLabel.TextAlign = ContentAlignment.TopLeft;
            previewLabel.ForeColor = Color.FromArgb(204, 214, 226);
            previewLayout.Controls.Add(previewLabel, 0, 1);

            okButton = BuildButton("OK");
            okButton.Click += delegate { ConfirmSelection(); };
            Button cancelButton = BuildButton("Cancel");
            cancelButton.DialogResult = DialogResult.Cancel;

            FlowLayoutPanel footer = new FlowLayoutPanel();
            footer.Dock = DockStyle.Fill;
            footer.FlowDirection = FlowDirection.RightToLeft;
            footer.BackColor = back;
            footer.Controls.Add(cancelButton);
            footer.Controls.Add(okButton);
            root.Controls.Add(footer, 0, 2);
            root.SetColumnSpan(footer, 2);

            LoadRows();
            SelectCurrent();
            Shown += delegate { searchBox.Focus(); };
        }

        private void LoadRows()
        {
            string[] terms = (searchBox.Text ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            IEnumerable<TitleGraphicIconOption> filtered = allOptions;
            if (terms.Length > 0)
            {
                filtered = filtered.Where(option => Matches(option, terms));
            }

            grid.SuspendLayout();
            try
            {
                grid.Rows.Clear();
                foreach (TitleGraphicIconOption option in filtered)
                {
                    int row = grid.Rows.Add(
                        GetThumbnail(option.Path),
                        option.FileName ?? string.Empty,
                        option.Path ?? string.Empty,
                        option.Source ?? string.Empty);
                    grid.Rows[row].Tag = option;
                }
            }
            finally
            {
                grid.ResumeLayout();
            }

            SelectCurrent();
            RefreshPreview();
        }

        private static bool Matches(TitleGraphicIconOption option, string[] terms)
        {
            if (option == null)
            {
                return false;
            }

            string haystack = string.Join(" ", new[]
            {
                option.FileName ?? string.Empty,
                option.Path ?? string.Empty,
                option.Source ?? string.Empty
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

        private Image GetThumbnail(string path)
        {
            string normalized = NormalizePath(path);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return null;
            }

            Bitmap thumbnail;
            if (thumbnailCache.TryGetValue(normalized, out thumbnail))
            {
                return thumbnail;
            }

            Bitmap source = imageLoader != null ? imageLoader(normalized) : null;
            if (source == null)
            {
                return null;
            }

            thumbnail = CreateThumbnail(source, 64, 48);
            thumbnailCache[normalized] = thumbnail;
            return thumbnail;
        }

        private static Bitmap CreateThumbnail(Bitmap source, int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                float scale = Math.Min((float)width / source.Width, (float)height / source.Height);
                int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
                int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
                int x = (width - drawWidth) / 2;
                int y = (height - drawHeight) / 2;
                graphics.DrawImage(source, new Rectangle(x, y, drawWidth, drawHeight));
            }

            return result;
        }

        private void SelectCurrent()
        {
            okButton.Enabled = grid.Rows.Count > 0;
            if (grid.Rows.Count == 0)
            {
                return;
            }

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                TitleGraphicIconOption option = grid.Rows[i].Tag as TitleGraphicIconOption;
                if (option != null && string.Equals(NormalizePath(option.Path), currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    grid.ClearSelection();
                    grid.Rows[i].Selected = true;
                    grid.CurrentCell = grid.Rows[i].Cells["File"];
                    return;
                }
            }

            if (grid.SelectedRows.Count == 0)
            {
                grid.Rows[0].Selected = true;
                grid.CurrentCell = grid.Rows[0].Cells["File"];
            }
        }

        private void RefreshPreview()
        {
            TitleGraphicIconOption option = SelectedOption;
            if (option == null)
            {
                previewBox.Image = null;
                previewLabel.Text = "No graphic selected.";
                return;
            }

            Bitmap image = imageLoader != null ? imageLoader(NormalizePath(option.Path)) : null;
            previewBox.Image = image;
            previewLabel.Text = (option.FileName ?? string.Empty) + Environment.NewLine
                + (option.Path ?? string.Empty) + Environment.NewLine
                + "Source: " + (option.Source ?? string.Empty)
                + (image != null
                    ? Environment.NewLine + image.Width.ToString(CultureInfo.InvariantCulture) + "x" + image.Height.ToString(CultureInfo.InvariantCulture)
                    : Environment.NewLine + "Preview unavailable");
        }

        private TitleGraphicIconOption SelectedOption
        {
            get
            {
                if (grid.SelectedRows.Count == 0)
                {
                    return null;
                }

                return grid.SelectedRows[0].Tag as TitleGraphicIconOption;
            }
        }

        private void ConfirmSelection()
        {
            TitleGraphicIconOption option = SelectedOption;
            if (option == null)
            {
                return;
            }

            SelectedPath = option.Path ?? string.Empty;
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

        private static Button BuildButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Width = 92;
            button.Height = 30;
            button.Margin = new Padding(8, 4, 0, 4);
            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = Color.FromArgb(38, 46, 57);
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = Color.FromArgb(116, 131, 151);
            return button;
        }

        private static string NormalizePath(string path)
        {
            return (path ?? string.Empty).Trim().TrimStart('\\', '/').Replace('/', '\\');
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Bitmap bitmap in thumbnailCache.Values)
                {
                    if (bitmap != null)
                    {
                        bitmap.Dispose();
                    }
                }

                thumbnailCache.Clear();
            }

            base.Dispose(disposing);
        }
    }
}

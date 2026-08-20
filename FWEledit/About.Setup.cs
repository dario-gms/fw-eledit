using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace FWEledit
{
    partial class About : Form
    {
        public About()
        {
            InitializeComponent();
            viewModel = new AboutViewModel();
            aboutCoordinatorService.ApplyViewModel(
                viewModel,
                this,
                labelProductName,
                labelVersion,
                labelCopyright,
                labelCompanyName,
                textBoxDescription);
            ApplyModernLayout();
        }

        private void ApplyModernLayout()
        {
            bool dark = Properties.Settings.Default.UseDarkMode;
            Color background = dark ? Color.FromArgb(14, 16, 20) : Color.FromArgb(238, 241, 246);
            Color panelFill = dark ? Color.FromArgb(244, 20, 24, 31) : Color.FromArgb(248, 255, 255, 255);
            Color border = dark ? Color.FromArgb(112, 124, 142) : Color.FromArgb(180, 190, 206);
            Color text = dark ? Color.FromArgb(235, 240, 248) : Color.FromArgb(26, 30, 36);
            Color muted = dark ? Color.FromArgb(170, 180, 194) : Color.FromArgb(82, 91, 104);
            Color gold = dark ? Color.FromArgb(232, 184, 92) : Color.FromArgb(146, 86, 15);
            Color link = dark ? Color.FromArgb(108, 169, 255) : Color.FromArgb(20, 88, 170);
            Image logo = LoadAboutLogo();
            Image appIcon = LoadApplicationIconBitmap();

            SuspendLayout();
            Text = "FWEledit";
            ShowIcon = true;
            Icon icon = LoadApplicationIcon();
            if (icon != null)
            {
                Icon = icon;
            }
            BackColor = background;
            ForeColor = text;
            Padding = new Padding(0);
            ClientSize = new Size(640, 330);
            MinimumSize = new Size(640, 330);

            Controls.Clear();

            AboutBackgroundPanel backgroundPanel = new AboutBackgroundPanel(background);
            backgroundPanel.Dock = DockStyle.Fill;
            Controls.Add(backgroundPanel);

            FillPanel contentPanel = new FillPanel(panelFill, border, logo, dark ? 0.06F : 0.05F);
            contentPanel.Size = new Size(520, 232);
            contentPanel.Padding = new Padding(24, 22, 24, 20);
            backgroundPanel.Controls.Add(contentPanel);
            CenterChild(backgroundPanel, contentPanel);
            backgroundPanel.Resize += (sender, args) => CenterChild(backgroundPanel, contentPanel);

            TableLayoutPanel contentLayout = new TableLayoutPanel();
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.BackColor = Color.Transparent;
            contentLayout.ColumnCount = 1;
            contentLayout.RowCount = 4;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            contentPanel.Controls.Add(contentLayout);

            TableLayoutPanel headerLayout = new TableLayoutPanel();
            headerLayout.Dock = DockStyle.Fill;
            headerLayout.BackColor = Color.Transparent;
            headerLayout.ColumnCount = 2;
            headerLayout.RowCount = 2;
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));

            PictureBox iconBox = new PictureBox();
            iconBox.Dock = DockStyle.Fill;
            iconBox.Margin = new Padding(0, 5, 12, 5);
            iconBox.BackColor = Color.Transparent;
            iconBox.SizeMode = PictureBoxSizeMode.Zoom;
            iconBox.Image = appIcon;

            labelProductName.AutoSize = false;
            labelProductName.Dock = DockStyle.Fill;
            labelProductName.Margin = new Padding(0);
            labelProductName.MaximumSize = Size.Empty;
            labelProductName.BackColor = Color.Transparent;
            labelProductName.Font = new Font(Font.FontFamily, 18F, FontStyle.Bold);
            labelProductName.ForeColor = gold;
            labelProductName.Text = viewModel.Product;
            labelProductName.TextAlign = ContentAlignment.MiddleLeft;

            labelVersion.AutoSize = false;
            labelVersion.Dock = DockStyle.Fill;
            labelVersion.Margin = new Padding(0);
            labelVersion.MaximumSize = Size.Empty;
            labelVersion.BackColor = Color.Transparent;
            labelVersion.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular);
            labelVersion.ForeColor = muted;
            labelVersion.Text = "Version " + viewModel.Version;
            labelVersion.TextAlign = ContentAlignment.MiddleLeft;

            Label descriptionLabel = new Label();
            descriptionLabel.Dock = DockStyle.Fill;
            descriptionLabel.Margin = new Padding(0);
            descriptionLabel.BackColor = Color.Transparent;
            descriptionLabel.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular);
            descriptionLabel.ForeColor = text;
            descriptionLabel.Text =
                "Forsaken World elements.data editor\r\n" +
                "\r\n" +
                "Open source tooling for client data, PCK resources, model previews and item workflows.";
            descriptionLabel.TextAlign = ContentAlignment.TopLeft;

            LinkLabel repositoryLink = new LinkLabel();
            repositoryLink.Text = "FWEledit GitHub page";
            repositoryLink.AutoSize = false;
            repositoryLink.Dock = DockStyle.Fill;
            repositoryLink.Margin = new Padding(0);
            repositoryLink.BackColor = Color.Transparent;
            repositoryLink.LinkColor = link;
            repositoryLink.ActiveLinkColor = Color.FromArgb(255, 178, 70);
            repositoryLink.VisitedLinkColor = link;
            repositoryLink.TextAlign = ContentAlignment.MiddleLeft;
            repositoryLink.LinkClicked += repositoryLink_LinkClicked;

            okButton.Text = "OK";
            okButton.Width = 86;
            okButton.Height = 26;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.FlatAppearance.BorderColor = border;
            okButton.BackColor = dark ? Color.FromArgb(38, 45, 54) : Color.FromArgb(238, 241, 246);
            okButton.ForeColor = text;
            okButton.Anchor = AnchorStyles.Right;
            okButton.Margin = new Padding(0);

            TableLayoutPanel footerLayout = new TableLayoutPanel();
            footerLayout.Dock = DockStyle.Fill;
            footerLayout.BackColor = Color.Transparent;
            footerLayout.ColumnCount = 2;
            footerLayout.RowCount = 1;
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            footerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96F));
            footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            headerLayout.Controls.Add(iconBox, 0, 0);
            headerLayout.SetRowSpan(iconBox, 2);
            headerLayout.Controls.Add(labelProductName, 1, 0);
            headerLayout.Controls.Add(labelVersion, 1, 1);
            footerLayout.Controls.Add(repositoryLink, 0, 0);
            footerLayout.Controls.Add(okButton, 1, 0);
            contentLayout.Controls.Add(headerLayout, 0, 0);
            contentLayout.Controls.Add(descriptionLabel, 0, 2);
            contentLayout.Controls.Add(footerLayout, 0, 3);

            AcceptButton = okButton;
            ResumeLayout(false);
        }

        private static void CenterChild(Control parent, Control child)
        {
            child.Left = Math.Max(0, (parent.ClientSize.Width - child.Width) / 2);
            child.Top = Math.Max(0, (parent.ClientSize.Height - child.Height) / 2);
        }

        private static Rectangle FitImage(Size imageSize, Size bounds)
        {
            if (imageSize.Width <= 0 || imageSize.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return new Rectangle(Point.Empty, bounds);
            }

            float scale = Math.Min((float)bounds.Width / imageSize.Width, (float)bounds.Height / imageSize.Height);
            int width = Math.Max(1, (int)(imageSize.Width * scale));
            int height = Math.Max(1, (int)(imageSize.Height * scale));
            return new Rectangle((bounds.Width - width) / 2, (bounds.Height - height) / 2, width, height);
        }

        private static Image LoadAboutLogo()
        {
            string path = Path.Combine(Application.StartupPath, "Resources", "about_logo.png");
            if (!File.Exists(path))
            {
                return null;
            }

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }

        private static Icon LoadApplicationIcon()
        {
            string path = Path.Combine(Application.StartupPath, "0000.ico");
            if (File.Exists(path))
            {
                return new Icon(path);
            }

            Icon executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            return executableIcon;
        }

        private static Image LoadApplicationIconBitmap()
        {
            using (Icon icon = LoadApplicationIcon())
            {
                return icon != null ? icon.ToBitmap() : null;
            }
        }

        private void repositoryLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "https://github.com/dario-gms/fw-eledit",
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }

        private sealed class AboutBackgroundPanel : Panel
        {
            private readonly Color background;

            public AboutBackgroundPanel(Color background)
            {
                this.background = background;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                BackColor = background;
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                e.Graphics.Clear(background);
            }
        }

        private sealed class FillPanel : Panel
        {
            private readonly Color fillColor;
            private readonly Color borderColor;
            private readonly Image watermark;
            private readonly float watermarkOpacity;

            public FillPanel(Color fillColor, Color borderColor, Image watermark, float watermarkOpacity)
            {
                this.fillColor = fillColor;
                this.borderColor = borderColor;
                this.watermark = watermark;
                this.watermarkOpacity = watermarkOpacity;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                using (SolidBrush brush = new SolidBrush(fillColor))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }

                if (watermark == null)
                {
                    return;
                }

                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                Rectangle target = FitImage(watermark.Size, ClientSize);
                target.Offset(ClientSize.Width / 6, ClientSize.Height / 10);
                using (ImageAttributes attributes = new ImageAttributes())
                {
                    ColorMatrix matrix = new ColorMatrix();
                    matrix.Matrix33 = Math.Max(0F, Math.Min(1F, watermarkOpacity));
                    attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    e.Graphics.DrawImage(watermark, target, 0, 0, watermark.Width, watermark.Height, GraphicsUnit.Pixel, attributes);
                }
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (Pen pen = new Pen(borderColor))
                {
                    Rectangle rect = ClientRectangle;
                    rect.Width -= 1;
                    rect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, rect);
                    rect.Inflate(-1, -1);
                    using (Pen innerPen = new Pen(Color.FromArgb(60, borderColor)))
                    {
                        e.Graphics.DrawRectangle(innerPen, rect);
                    }
                }
            }
        }
    }
}



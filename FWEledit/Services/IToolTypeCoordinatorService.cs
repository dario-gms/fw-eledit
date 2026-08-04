using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class IToolTypeCoordinatorService
    {
        public void InitializePreview(
            IToolTypeViewModel viewModel,
            ISessionService sessionService,
            ToolPreviewUiService previewUiService,
            InfoTool data,
            Label titleLabel,
            PictureBox iconBox,
            RichTextBox previewBox)
        {
            if (viewModel == null || sessionService == null || previewUiService == null)
            {
                return;
            }

            ToolPreviewData preview = viewModel.Load(data, sessionService.Database);
            if (titleLabel != null)
            {
                titleLabel.Text = preview.TitleText;
                titleLabel.ForeColor = preview.TitleColor;
            }
            if (iconBox != null)
            {
                iconBox.Image = preview.IconImage;
                iconBox.SizeMode = PictureBoxSizeMode.Zoom;
                iconBox.Visible = preview.IconImage != null;
            }
            if (previewBox != null)
            {
                previewUiService.RenderPreview(previewBox, preview.PreviewText);
            }
        }

        public void ApplyTheme(
            ToolPreviewUiService previewUiService,
            CacheSave database,
            Form owner,
            RichTextBox previewBox,
            Label titleLabel)
        {
            if (previewUiService == null)
            {
                return;
            }

            previewUiService.ApplyTheme(database, owner, previewBox, titleLabel);
        }

        public void AdjustLayout(Form owner, RichTextBox previewBox)
        {
            if (owner == null || previewBox == null)
            {
                return;
            }

            const int minWidth = 360;
            const int maxWidth = 460;
            const int horizontalPadding = 24;
            const int headerHeight = 58;
            const int bottomPadding = 12;
            const int maxHeight = 560;

            int measuredWidth = TextRenderer.MeasureText(
                previewBox.Text ?? string.Empty,
                previewBox.Font,
                new Size(maxWidth - horizontalPadding, int.MaxValue),
                TextFormatFlags.WordBreak).Width + horizontalPadding + 8;
            owner.Width = Math.Max(minWidth, Math.Min(maxWidth, measuredWidth));
            previewBox.Width = owner.Width - horizontalPadding;

            int desiredTextHeight = Math.Max(42, previewBox.Height);
            int desiredHeight = headerHeight + desiredTextHeight + bottomPadding;
            owner.Height = Math.Min(maxHeight, desiredHeight);
            previewBox.Height = Math.Max(42, owner.Height - headerHeight - bottomPadding);

            Size screen = Screen.PrimaryScreen.WorkingArea.Size;
            int bottomLimit = screen.Height;
            if (owner.Bottom > bottomLimit)
            {
                owner.Top = owner.Top - owner.Height;
            }

            int rightLimit = screen.Width;
            if (owner.Right > rightLimit)
            {
                owner.Left = Math.Max(0, rightLimit - owner.Width);
            }
        }

        public void HandleContentsResized(RichTextBox previewBox, ContentsResizedEventArgs e)
        {
            if (previewBox == null || e == null)
            {
                return;
            }

            previewBox.Height = Math.Min(490, e.NewRectangle.Height + 8);
        }

        public void HandleFadeTick(Form owner, Timer timer, double increment)
        {
            if (owner == null || timer == null || owner.IsDisposed)
            {
                return;
            }

            owner.Opacity += increment;
            if (owner.Opacity >= 0.99)
            {
                timer.Enabled = false;
            }
        }
    }
}

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
            const int maxWidth = 560;
            const int horizontalPadding = 24;
            const int headerHeight = 58;
            const int bottomPadding = 12;
            Rectangle workingArea = Screen.FromPoint(owner.Location).WorkingArea;
            int maxHeight = Math.Max(260, workingArea.Height - 40);

            int measuredWidth = TextRenderer.MeasureText(
                previewBox.Text ?? string.Empty,
                previewBox.Font,
                new Size(maxWidth - horizontalPadding, int.MaxValue),
                TextFormatFlags.WordBreak).Width + horizontalPadding + 8;
            owner.Width = Math.Max(minWidth, Math.Min(maxWidth, measuredWidth));
            previewBox.Width = owner.Width - horizontalPadding;
            if (titleLabelFromOwner(owner) != null)
            {
                titleLabelFromOwner(owner).Width = Math.Max(80, owner.Width - 70);
            }

            int measuredTextHeight = TextRenderer.MeasureText(
                previewBox.Text ?? string.Empty,
                previewBox.Font,
                new Size(previewBox.Width - 8, int.MaxValue),
                TextFormatFlags.WordBreak).Height + 18;
            int desiredTextHeight = Math.Max(Math.Max(42, previewBox.Height), measuredTextHeight);
            int desiredHeight = headerHeight + desiredTextHeight + bottomPadding;
            owner.Height = Math.Min(maxHeight, desiredHeight);
            previewBox.Height = Math.Max(42, owner.Height - headerHeight - bottomPadding);
            previewBox.ScrollBars = desiredHeight > owner.Height
                ? RichTextBoxScrollBars.Vertical
                : RichTextBoxScrollBars.None;

            int bottomLimit = workingArea.Bottom;
            if (owner.Bottom > bottomLimit)
            {
                owner.Top = Math.Max(workingArea.Top, bottomLimit - owner.Height);
            }

            int rightLimit = workingArea.Right;
            if (owner.Right > rightLimit)
            {
                owner.Left = Math.Max(workingArea.Left, rightLimit - owner.Width);
            }
        }

        public void HandleContentsResized(RichTextBox previewBox, ContentsResizedEventArgs e)
        {
            if (previewBox == null || e == null)
            {
                return;
            }

            Rectangle workingArea = Screen.FromControl(previewBox).WorkingArea;
            int maxTextHeight = Math.Max(180, workingArea.Height - 110);
            previewBox.Height = Math.Min(maxTextHeight, e.NewRectangle.Height + 12);
            previewBox.ScrollBars = e.NewRectangle.Height + 12 > maxTextHeight
                ? RichTextBoxScrollBars.Vertical
                : RichTextBoxScrollBars.None;
        }

        private static Label titleLabelFromOwner(Form owner)
        {
            if (owner == null)
            {
                return null;
            }

            Control[] controls = owner.Controls.Find("titleText", false);
            return controls != null && controls.Length > 0 ? controls[0] as Label : null;
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

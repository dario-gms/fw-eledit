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

            owner.Height = 50 + previewBox.Height;

            Size screen = Screen.PrimaryScreen.WorkingArea.Size;
            int bottomLimit = screen.Height;
            if (owner.Bottom > bottomLimit)
            {
                owner.Top = owner.Top - owner.Height;
            }
        }

        public void HandleContentsResized(RichTextBox previewBox, ContentsResizedEventArgs e)
        {
            if (previewBox == null || e == null)
            {
                return;
            }

            previewBox.Height = e.NewRectangle.Height + 5;
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

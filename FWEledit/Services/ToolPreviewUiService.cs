using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ToolPreviewUiService
    {
        private readonly DescriptionPreviewService descriptionPreviewService = new DescriptionPreviewService();
        private readonly DescriptionPreviewUiService descriptionPreviewUiService = new DescriptionPreviewUiService();

        public void ApplyTheme(CacheSave database, Form form, RichTextBox previewBox, Label title)
        {
            Color bg = Color.FromArgb(18, 21, 26);
            if (database != null && database.arrTheme != null && database.arrTheme.Count > 16)
            {
                Color themeBg = Color.FromName(database.arrTheme[16]);
                if (!themeBg.IsEmpty)
                {
                    bg = themeBg;
                }
            }
            if (form != null)
            {
                form.BackColor = bg;
            }
            if (previewBox != null)
            {
                previewBox.BackColor = bg;
                previewBox.ForeColor = Color.FromArgb(219, 226, 235);
            }
            if (title != null)
            {
                title.BackColor = bg;
            }
        }

        public void RenderPreview(RichTextBox previewBox, string description)
        {
            if (previewBox == null)
            {
                return;
            }

            descriptionPreviewUiService.RenderPreview(previewBox, descriptionPreviewService, description);
            previewBox.Multiline = true;
            previewBox.DeselectAll();
        }
    }
}

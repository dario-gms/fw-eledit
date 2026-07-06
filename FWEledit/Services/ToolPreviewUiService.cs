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
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            Color bg = Color.FromName(database.arrTheme[16]);
            if (form != null)
            {
                form.BackColor = bg;
            }
            if (previewBox != null)
            {
                previewBox.BackColor = bg;
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

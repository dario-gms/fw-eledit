using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class DescriptionPreviewUiService
    {
        public void RenderPreview(RichTextBox previewBox, DescriptionPreviewService previewService, string rawText)
        {
            if (previewBox == null || previewService == null)
            {
                return;
            }

            previewBox.SuspendLayout();
            previewBox.Clear();

            List<DescriptionPreviewSegment> segments = previewService.BuildSegments(rawText);
            for (int i = 0; i < segments.Count; i++)
            {
                DescriptionPreviewSegment segment = segments[i];
                AppendSegment(previewBox, segment.Text, segment.Color);
            }

            previewBox.SelectionStart = 0;
            previewBox.SelectionLength = 0;
            previewBox.ResumeLayout();
        }

        private void AppendSegment(RichTextBox previewBox, string text, Color color)
        {
            if (previewBox == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            int start = previewBox.TextLength;
            previewBox.SelectionStart = start;
            previewBox.SelectionLength = 0;
            previewBox.SelectionColor = color;
            previewBox.AppendText(text);
        }
    }
}

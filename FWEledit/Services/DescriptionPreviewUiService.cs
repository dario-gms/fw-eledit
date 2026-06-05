using System.Collections.Generic;
using System.Drawing;
using System.Text;
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
                AppendSegment(previewBox, RemoveResidualTags(segment.Text), segment.Color);
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

        private static string RemoveResidualTags(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder clean = new StringBuilder(text.Length);
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '^')
                {
                    if (i + 7 <= text.Length && IsHexSequence(text, i + 1, 6))
                    {
                        i += 7;
                        continue;
                    }

                    if (i + 5 <= text.Length && IsControlMarkerBody(text, i + 1))
                    {
                        i += 5;
                        continue;
                    }
                }

                clean.Append(text[i]);
                i++;
            }

            return clean.ToString();
        }

        private static bool IsHexSequence(string text, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex + length > text.Length)
            {
                return false;
            }

            for (int i = startIndex; i < startIndex + length; i++)
            {
                char c = text[i];
                bool isHex = (c >= '0' && c <= '9')
                    || (c >= 'a' && c <= 'f')
                    || (c >= 'A' && c <= 'F');
                if (!isHex)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsControlMarkerBody(string text, int startIndex)
        {
            if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex + 4 > text.Length)
            {
                return false;
            }

            for (int i = startIndex; i < startIndex + 4; i++)
            {
                char c = text[i];
                bool isMarkerChar = (c >= '0' && c <= '9')
                    || (c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z');
                if (!isMarkerChar)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

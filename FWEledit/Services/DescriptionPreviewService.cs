using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace FWEledit
{
    public sealed class DescriptionPreviewService
    {
        public List<DescriptionPreviewSegment> BuildSegments(string rawText)
        {
            List<DescriptionPreviewSegment> segments = new List<DescriptionPreviewSegment>();
            string text = rawText ?? string.Empty;
            Color currentColor = Color.White;
            StringBuilder chunk = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '^')
                {
                    // FW color tag: ^RRGGBB
                    if (i + 7 <= text.Length && IsHexSequence(text, i + 1, 6))
                    {
                        FlushChunk(segments, chunk, currentColor);
                        string hex = text.Substring(i + 1, 6);
                        try
                        {
                            currentColor = ColorTranslator.FromHtml("#" + hex);
                        }
                        catch
                        {
                            currentColor = Color.White;
                        }
                        i += 7;
                        continue;
                    }

                    // Some FW strings include short formatting markers like ^0037 before a real color tag.
                    if (i + 5 <= text.Length && IsHexSequence(text, i + 1, 4))
                    {
                        FlushChunk(segments, chunk, currentColor);
                        i += 5;
                        continue;
                    }
                }

                chunk.Append(text[i]);
                i++;
            }

            FlushChunk(segments, chunk, currentColor);
            return segments;
        }

        private static void FlushChunk(List<DescriptionPreviewSegment> segments, StringBuilder chunk, Color color)
        {
            if (chunk.Length == 0)
            {
                return;
            }
            segments.Add(new DescriptionPreviewSegment
            {
                Text = chunk.ToString(),
                Color = color
            });
            chunk.Clear();
        }

        private static bool IsHexSequence(string text, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(text) || startIndex < 0 || length <= 0)
            {
                return false;
            }
            int end = startIndex + length;
            if (end > text.Length)
            {
                return false;
            }

            for (int i = startIndex; i < end; i++)
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
    }
}

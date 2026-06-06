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
            string text = NormalizeText(rawText);
            Color currentColor = Color.White;
            float currentFontScale = 1F;
            FontStyle currentFontStyle = FontStyle.Regular;
            bool currentUnderline = false;
            StringBuilder chunk = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '^')
                {
                    // FW color tag: ^RRGGBB
                    if (i + 7 <= text.Length && IsHexSequence(text, i + 1, 6))
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
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

                    if (i + 2 <= text.Length && text[i + 1] == 'N')
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
                        currentColor = Color.White;
                        i += 2;
                        continue;
                    }

                    if (i + 5 <= text.Length && text[i + 1] == 'O' && IsDigitSequence(text, i + 2, 3))
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
                        ApplyFontTag(text.Substring(i + 2, 3), out currentFontScale, out currentFontStyle);
                        i += 5;
                        continue;
                    }

                    if (i + 8 <= text.Length && text[i + 1] == 'U' && IsHexSequence(text, i + 2, 6))
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
                        currentUnderline = !currentUnderline;
                        i += 8;
                        continue;
                    }

                    if (i + 2 <= text.Length && text[i + 1] == 'u')
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
                        currentUnderline = !currentUnderline;
                        i += 2;
                        continue;
                    }

                    if (i + 2 <= text.Length && text[i + 1] == 'p')
                    {
                        i += 2;
                        continue;
                    }

                    if (i + 2 <= text.Length && text[i + 1] == 'o')
                    {
                        chunk.Append(' ');
                        i += 2;
                        continue;
                    }

                    if (IsShortFormatMarker(text, i) || IsChainedControlMarker(text, i))
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
                        i += 5;
                        continue;
                    }

                    // Some FW strings include short formatting markers like ^0037.
                    if (IsFourDigitControlMarker(text, i))
                    {
                        FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
                        i += 5;
                        continue;
                    }
                }

                chunk.Append(text[i]);
                i++;
            }

            FlushChunk(segments, chunk, currentColor, currentFontScale, currentFontStyle, currentUnderline);
            return segments;
        }

        private static void FlushChunk(
            List<DescriptionPreviewSegment> segments,
            StringBuilder chunk,
            Color color,
            float fontScale,
            FontStyle fontStyle,
            bool underline)
        {
            if (chunk.Length == 0)
            {
                return;
            }
            segments.Add(new DescriptionPreviewSegment
            {
                Text = chunk.ToString(),
                Color = color,
                FontScale = fontScale,
                FontStyle = fontStyle,
                Underline = underline
            });
            chunk.Clear();
        }

        private static void ApplyFontTag(string fontTag, out float fontScale, out FontStyle fontStyle)
        {
            fontScale = 1F;
            fontStyle = FontStyle.Regular;

            switch (fontTag)
            {
                case "005":
                case "009":
                    fontScale = 0.92F;
                    break;
                case "037":
                case "041":
                    fontScale = 1F;
                    fontStyle = FontStyle.Bold;
                    break;
                case "057":
                    fontScale = 1.12F;
                    fontStyle = FontStyle.Bold;
                    break;
                case "061":
                    fontScale = 1.2F;
                    fontStyle = FontStyle.Bold;
                    break;
                case "065":
                case "069":
                    fontScale = 1.15F;
                    fontStyle = FontStyle.Bold;
                    break;
            }
        }

        private static string NormalizeText(string rawText)
        {
            return (rawText ?? string.Empty)
                .Replace("\\r", "\r")
                .Replace("\\n", "\n");
        }

        private static bool IsShortFormatMarker(string text, int caretIndex)
        {
            if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex + 5 > text.Length)
            {
                return false;
            }

            if (text[caretIndex] != '^')
            {
                return false;
            }

            for (int i = caretIndex + 1; i < caretIndex + 5; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return false;
                }
            }

            if (caretIndex + 5 >= text.Length)
            {
                return true;
            }

            char next = text[caretIndex + 5];
            return next == '^' || !IsHexChar(next);
        }

        private static bool IsChainedControlMarker(string text, int caretIndex)
        {
            if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex + 12 > text.Length)
            {
                return false;
            }

            if (text[caretIndex] != '^' || text[caretIndex + 5] != '^')
            {
                return false;
            }

            return IsControlMarkerBody(text, caretIndex + 1)
                && IsHexSequence(text, caretIndex + 6, 6);
        }

        private static bool IsFourDigitControlMarker(string text, int caretIndex)
        {
            if (string.IsNullOrEmpty(text) || caretIndex < 0 || caretIndex + 5 > text.Length)
            {
                return false;
            }

            if (text[caretIndex] != '^' || !IsControlMarkerBody(text, caretIndex + 1))
            {
                return false;
            }

            if (caretIndex + 7 <= text.Length && IsHexSequence(text, caretIndex + 1, 6))
            {
                return false;
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

        private static bool IsDigitSequence(string text, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(text) || startIndex < 0 || startIndex + length > text.Length)
            {
                return false;
            }

            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (text[i] < '0' || text[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsHexChar(char c)
        {
            return (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F');
        }
    }
}

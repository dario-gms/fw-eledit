using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ToolPreviewUiService
    {
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

            string line = (description ?? string.Empty).Replace("\\r", Environment.NewLine).Replace("\\n", Environment.NewLine);
            string defaultColor = "^FFFFFF";
            Color defaultColorValue = Color.FromArgb(int.Parse(defaultColor.Substring(1, 6), NumberStyles.HexNumber));
            string[] blocks = line.Split(new char[] { '^' });

            if (blocks.Length > 1)
            {
                previewBox.Text = string.Empty;
                int firstCaret = line.IndexOf('^');
                if (firstCaret > 0)
                {
                    previewBox.AppendText(line.Substring(0, firstCaret));
                    previewBox.Select(0, firstCaret);
                    previewBox.SelectionColor = defaultColorValue;
                }

                int length;
                int start;
                Color color = defaultColorValue;
                for (int i = 1; i < blocks.Length; i++)
                {
                    if (blocks[i] == string.Empty)
                    {
                        continue;
                    }

                    start = previewBox.Text.Length;
                    try
                    {
                        string colorCode = blocks[i].Substring(0, 6).ToUpperInvariant();
                        if (colorCode == "FFFFFF")
                        {
                            color = defaultColorValue;
                        }
                        else
                        {
                            color = Color.FromArgb(int.Parse(colorCode, NumberStyles.HexNumber));
                        }
                        previewBox.AppendText(blocks[i].Substring(6));
                    }
                    catch
                    {
                        previewBox.AppendText("^" + blocks[i]);
                    }
                    length = previewBox.Text.Length - start;
                    previewBox.Select(start, length);
                    previewBox.SelectionColor = color;
                }
            }
            else
            {
                previewBox.Text = line;
                previewBox.Select(0, previewBox.Text.Length);
                previewBox.SelectionColor = defaultColorValue;
            }

            previewBox.Multiline = true;
            previewBox.DeselectAll();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed partial class IconPickerWindow : Form
    {
        public sealed class AtlasGrid : ScrollableControl
        {
            private readonly ToolTip tip;
            private List<IconEntryModel> entries = new List<IconEntryModel>();
            private Bitmap atlas;
            private int iconW = 32;
            private int iconH = 32;
            private int atlasCols = 1;
            private int cellW = 34;
            private int cellH = 34;
            private int columns = 1;
            private int selectedIndex = -1;
            private int hoverIndex = -1;
            private Func<IconEntryModel, string> tooltipFactory;
            private int tooltipIndex = -2;
            private string tooltipText = string.Empty;

            public event EventHandler SelectedIndexChanged;
            public event EventHandler ItemDoubleClick;

            public AtlasGrid()
            {
                DoubleBuffered = true;
                AutoScroll = true;
                BackColor = Color.Black;
                tip = new ToolTip();
            }

            public int SelectedIndex
            {
                get { return selectedIndex; }
                set
                {
                    if (value < -1 || value >= entries.Count)
                    {
                        value = -1;
                    }
                    if (selectedIndex == value)
                    {
                        return;
                    }
                    int old = selectedIndex;
                    selectedIndex = value;
                    InvalidateIndex(old);
                    InvalidateIndex(selectedIndex);
                    if (SelectedIndexChanged != null)
                    {
                        SelectedIndexChanged(this, EventArgs.Empty);
                    }
                }
            }

            public IconEntryModel SelectedEntry
            {
                get
                {
                    if (selectedIndex < 0 || selectedIndex >= entries.Count)
                    {
                        return null;
                    }
                    return entries[selectedIndex];
                }
            }

            public void SetTooltipFactory(Func<IconEntryModel, string> factory)
            {
                tooltipFactory = factory;
            }

            public void SetData(Bitmap atlasBitmap, int iconWidth, int iconHeight, int cols, List<IconEntryModel> list)
            {
                atlas = atlasBitmap;
                iconW = Math.Max(1, iconWidth);
                iconH = Math.Max(1, iconHeight);
                atlasCols = Math.Max(1, cols);
                entries = list ?? new List<IconEntryModel>();
                cellW = iconW + 2;
                cellH = iconH + 2;
                RecalcLayout();
                ResetTooltipState();
                SelectedIndex = entries.Count > 0 ? 0 : -1;
                Invalidate();
            }

            public void SetEntries(List<IconEntryModel> list)
            {
                entries = list ?? new List<IconEntryModel>();
                RecalcLayout();
                ResetTooltipState();
                if (entries.Count == 0)
                {
                    SelectedIndex = -1;
                }
                else if (SelectedIndex >= entries.Count)
                {
                    SelectedIndex = entries.Count - 1;
                }
                Invalidate();
            }

            public void EnsureSelectionVisible()
            {
                if (selectedIndex < 0 || selectedIndex >= entries.Count)
                {
                    return;
                }

                int row = selectedIndex / Math.Max(1, columns);
                int y = row * cellH;
                int top = -AutoScrollPosition.Y;
                int bottom = top + ClientSize.Height;
                if (y < top)
                {
                    AutoScrollPosition = new Point(0, y);
                }
                else if (y + cellH > bottom)
                {
                    AutoScrollPosition = new Point(0, Math.Max(0, y + cellH - ClientSize.Height));
                }
            }

            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                RecalcLayout();
                Invalidate();
            }

            private void RecalcLayout()
            {
                columns = Math.Max(1, ClientSize.Width / Math.Max(1, cellW));
                int rows = entries.Count == 0 ? 0 : ((entries.Count + columns - 1) / columns);
                AutoScrollMinSize = new Size(0, Math.Max(0, rows * cellH + 2));
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (atlas == null || entries.Count == 0)
                {
                    return;
                }

                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                e.Graphics.SmoothingMode = SmoothingMode.None;

                int offsetX = AutoScrollPosition.X;
                int offsetY = AutoScrollPosition.Y;
                int startRow = Math.Max(0, (-offsetY) / Math.Max(1, cellH));
                int endRow = Math.Min((entries.Count + columns - 1) / columns, ((-offsetY + ClientSize.Height) / Math.Max(1, cellH)) + 2);

                for (int row = startRow; row <= endRow; row++)
                {
                    int start = row * columns;
                    int end = Math.Min(entries.Count, start + columns);
                    for (int i = start; i < end; i++)
                    {
                        int col = i - start;
                        int x = offsetX + col * cellW + 1;
                        int y = offsetY + row * cellH + 1;

                        IconEntryModel entry = entries[i];
                        int aCol = entry.AtlasIndex % atlasCols;
                        int aRow = entry.AtlasIndex / atlasCols;
                        Rectangle src = new Rectangle(aCol * iconW, aRow * iconH, iconW, iconH);
                        Rectangle dst = new Rectangle(x, y, iconW, iconH);

                        if (src.X >= 0 && src.Y >= 0 && src.Right <= atlas.Width && src.Bottom <= atlas.Height)
                        {
                            e.Graphics.DrawImage(atlas, dst, src, GraphicsUnit.Pixel);
                        }
                        else
                        {
                            using (Brush b = new SolidBrush(Color.FromArgb(36, 36, 36)))
                            {
                                e.Graphics.FillRectangle(b, dst);
                            }
                        }

                        if (i == selectedIndex)
                        {
                            using (Pen p = new Pen(Color.Gold, 2))
                            {
                                e.Graphics.DrawRectangle(p, dst);
                            }
                            using (Pen p2 = new Pen(Color.FromArgb(255, 255, 220, 120), 1))
                            {
                                Rectangle glow = new Rectangle(dst.X - 1, dst.Y - 1, dst.Width + 2, dst.Height + 2);
                                e.Graphics.DrawRectangle(p2, glow);
                            }
                        }
                        else if (i == hoverIndex)
                        {
                            using (Pen hp = new Pen(Color.FromArgb(220, 120, 190, 255), 2))
                            {
                                e.Graphics.DrawRectangle(hp, dst);
                            }
                        }
                    }
                }
            }

            protected override void OnMouseMove(MouseEventArgs e)
            {
                base.OnMouseMove(e);
                int idx = HitTestIndex(e.Location);
                if (idx != hoverIndex)
                {
                    int oldHover = hoverIndex;
                    hoverIndex = idx;
                    InvalidateIndex(oldHover);
                    InvalidateIndex(hoverIndex);
                }
                if (idx < 0 || idx >= entries.Count || tooltipFactory == null)
                {
                    ClearTooltip();
                    return;
                }

                string text = tooltipFactory(entries[idx]);
                if (string.IsNullOrWhiteSpace(text))
                {
                    ClearTooltip();
                    return;
                }

                if (tooltipIndex == idx && string.Equals(tooltipText, text, StringComparison.Ordinal))
                {
                    return;
                }

                tooltipIndex = idx;
                tooltipText = text;
                tip.SetToolTip(this, text);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                if (hoverIndex != -1)
                {
                    int oldHover = hoverIndex;
                    hoverIndex = -1;
                    InvalidateIndex(oldHover);
                }
                ClearTooltip();
            }

            protected override void OnMouseClick(MouseEventArgs e)
            {
                base.OnMouseClick(e);
                int idx = HitTestIndex(e.Location);
                if (idx < 0 || idx >= entries.Count)
                {
                    return;
                }

                SelectedIndex = idx;
                if (e.Button == MouseButtons.Left)
                {
                    if (ItemDoubleClick != null)
                    {
                        ItemDoubleClick(this, EventArgs.Empty);
                    }
                }
            }

            private int HitTestIndex(Point p)
            {
                int x = p.X - AutoScrollPosition.X;
                int y = p.Y - AutoScrollPosition.Y;
                if (x < 0 || y < 0)
                {
                    return -1;
                }

                int col = x / Math.Max(1, cellW);
                int row = y / Math.Max(1, cellH);
                if (col < 0 || col >= columns)
                {
                    return -1;
                }
                int idx = row * columns + col;
                if (idx < 0 || idx >= entries.Count)
                {
                    return -1;
                }
                return idx;
            }

            private void InvalidateIndex(int idx)
            {
                Rectangle rect = GetCellRect(idx);
                if (!rect.IsEmpty)
                {
                    Invalidate(rect);
                }
            }

            private void ResetTooltipState()
            {
                tooltipIndex = -2;
                tooltipText = string.Empty;
                tip.SetToolTip(this, string.Empty);
            }

            private void ClearTooltip()
            {
                if (tooltipIndex == -1 && tooltipText.Length == 0)
                {
                    return;
                }

                tooltipIndex = -1;
                tooltipText = string.Empty;
                tip.SetToolTip(this, string.Empty);
            }

            private Rectangle GetCellRect(int idx)
            {
                if (idx < 0 || idx >= entries.Count || columns <= 0)
                {
                    return Rectangle.Empty;
                }

                int row = idx / columns;
                int col = idx % columns;
                int x = AutoScrollPosition.X + col * cellW + 1;
                int y = AutoScrollPosition.Y + row * cellH + 1;
                return new Rectangle(x - 2, y - 2, iconW + 5, iconH + 5);
            }
        }
    }
}




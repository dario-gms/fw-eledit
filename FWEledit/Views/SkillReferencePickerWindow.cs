using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SkillReferencePickerWindow : Form
    {
        private readonly List<SkillReferenceOption> allOptions;
        private readonly TextBox searchBox;
        private readonly ListBox listBox;
        private readonly TextBox descriptionBox;

        public int SelectedValue { get; private set; }
        public SkillReferenceOption SelectedOption { get; private set; }

        public SkillReferencePickerWindow(List<SkillReferenceOption> options, int currentValue, string title)
        {
            allOptions = options ?? new List<SkillReferenceOption>();
            SelectedValue = currentValue;

            Text = string.IsNullOrWhiteSpace(title) ? "Choose skill or buff..." : title;
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(560, 460);
            Size = new Size(720, 620);
            BackColor = Color.FromArgb(17, 20, 24);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            KeyPreview = true;
            KeyDown += HandleWindowKeyDown;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = new Padding(0);
            layout.Padding = new Padding(10);
            layout.ColumnCount = 1;
            layout.RowCount = 4;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F));

            searchBox = new TextBox();
            searchBox.Dock = DockStyle.Fill;
            searchBox.Margin = new Padding(0, 0, 0, 8);
            searchBox.BackColor = Color.FromArgb(28, 32, 37);
            searchBox.ForeColor = Color.White;
            searchBox.BorderStyle = BorderStyle.FixedSingle;
            searchBox.TextChanged += (s, e) => LoadOptions();

            listBox = new ListBox();
            listBox.Dock = DockStyle.Fill;
            listBox.BorderStyle = BorderStyle.FixedSingle;
            listBox.DrawMode = DrawMode.OwnerDrawFixed;
            listBox.ItemHeight = 34;
            listBox.IntegralHeight = false;
            listBox.BackColor = Color.FromArgb(15, 18, 22);
            listBox.ForeColor = Color.White;
            listBox.DrawItem += DrawItem;
            listBox.SelectedIndexChanged += (s, e) => UpdateDescription();
            listBox.DoubleClick += (s, e) => ConfirmSelection();
            listBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    ConfirmSelection();
                    e.SuppressKeyPress = true;
                }
            };

            Label descriptionLabel = new Label();
            descriptionLabel.Dock = DockStyle.Fill;
            descriptionLabel.Margin = new Padding(0, 8, 0, 4);
            descriptionLabel.Text = "Description";
            descriptionLabel.TextAlign = ContentAlignment.BottomLeft;
            descriptionLabel.ForeColor = Color.FromArgb(164, 177, 194);

            descriptionBox = new TextBox();
            descriptionBox.Dock = DockStyle.Fill;
            descriptionBox.Margin = new Padding(0);
            descriptionBox.Multiline = true;
            descriptionBox.ReadOnly = true;
            descriptionBox.ScrollBars = ScrollBars.Vertical;
            descriptionBox.BackColor = Color.FromArgb(15, 18, 22);
            descriptionBox.ForeColor = Color.White;
            descriptionBox.BorderStyle = BorderStyle.FixedSingle;

            layout.Controls.Add(searchBox, 0, 0);
            layout.Controls.Add(listBox, 0, 1);
            layout.Controls.Add(descriptionLabel, 0, 2);
            layout.Controls.Add(descriptionBox, 0, 3);
            Controls.Add(layout);

            LoadOptions();
            SelectValue(currentValue);
            UpdateDescription();
            Shown += (s, e) => searchBox.Focus();
        }

        private void HandleWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && !searchBox.Focused)
            {
                ConfirmSelection();
            }
        }

        private void LoadOptions()
        {
            string query = (searchBox.Text ?? string.Empty).Trim();
            SkillReferenceOption previouslySelected = listBox.SelectedItem as SkillReferenceOption;
            int selectedValue = previouslySelected != null ? previouslySelected.Value : SelectedValue;

            IEnumerable<SkillReferenceOption> filtered = allOptions;
            if (!string.IsNullOrWhiteSpace(query))
            {
                filtered = filtered.Where(option => Matches(option, query));
            }

            listBox.BeginUpdate();
            listBox.Items.Clear();
            foreach (SkillReferenceOption option in filtered)
            {
                listBox.Items.Add(option);
            }
            listBox.EndUpdate();

            SelectValue(selectedValue);
            if (listBox.SelectedIndex < 0 && listBox.Items.Count > 0)
            {
                listBox.SelectedIndex = 0;
            }
        }

        private static bool Matches(SkillReferenceOption option, string query)
        {
            if (option == null)
            {
                return false;
            }

            return option.Value.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Label ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Description ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || (option.Kind ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SelectValue(int value)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                SkillReferenceOption option = listBox.Items[i] as SkillReferenceOption;
                if (option != null && option.Value == value)
                {
                    listBox.SelectedIndex = i;
                    listBox.TopIndex = Math.Max(0, i - 6);
                    return;
                }
            }
        }

        private void UpdateDescription()
        {
            SkillReferenceOption option = listBox.SelectedItem as SkillReferenceOption;
            if (option == null)
            {
                descriptionBox.Text = string.Empty;
                return;
            }

            string kind = string.IsNullOrWhiteSpace(option.Kind) ? "Entry" : option.Kind;
            string header = kind + " ID: " + option.Value;
            string body = string.IsNullOrWhiteSpace(option.Description)
                ? "No in-game description was found for this entry."
                : option.Description;

            descriptionBox.Text = header + Environment.NewLine + Environment.NewLine + body;
        }

        private void ConfirmSelection()
        {
            SkillReferenceOption option = listBox.SelectedItem as SkillReferenceOption;
            if (option == null)
            {
                return;
            }

            SelectedOption = option;
            SelectedValue = option.Value;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= listBox.Items.Count)
            {
                return;
            }

            SkillReferenceOption option = listBox.Items[e.Index] as SkillReferenceOption;
            if (option == null)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color backColor = selected ? Color.FromArgb(54, 48, 88) : listBox.BackColor;
            Color idColor = selected ? Color.FromArgb(215, 225, 238) : Color.FromArgb(130, 144, 163);
            Color nameColor = selected ? Color.White : Color.FromArgb(232, 236, 241);
            Color kindColor = string.Equals(option.Kind, "Buff", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(120, 211, 255)
                : Color.FromArgb(255, 197, 92);
            if (selected)
            {
                kindColor = Color.FromArgb(255, 236, 180);
            }

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            Rectangle idBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + 4, 82, 16);
            Rectangle nameBounds = new Rectangle(e.Bounds.Left + 96, e.Bounds.Top + 4, Math.Max(40, e.Bounds.Width - 180), 16);
            Rectangle kindBounds = new Rectangle(e.Bounds.Right - 74, e.Bounds.Top + 4, 64, 16);
            Rectangle descBounds = new Rectangle(e.Bounds.Left + 96, e.Bounds.Top + 18, Math.Max(40, e.Bounds.Width - 106), 14);

            TextRenderer.DrawText(e.Graphics, option.Value.ToString(), Font, idBounds, idColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            TextRenderer.DrawText(e.Graphics, option.Label ?? string.Empty, Font, nameBounds, nameColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, option.Kind ?? string.Empty, Font, kindBounds, kindColor, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(
                e.Graphics,
                TrimSingleLine(option.Description),
                Font,
                descBounds,
                selected ? Color.FromArgb(220, 226, 235) : Color.FromArgb(133, 145, 160),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        private static string TrimSingleLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string singleLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
            while (singleLine.Contains("  "))
            {
                singleLine = singleLine.Replace("  ", " ");
            }
            return singleLine;
        }
    }
}

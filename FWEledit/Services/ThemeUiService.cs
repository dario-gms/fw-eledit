using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ThemeUiService
    {
        private static readonly Color Surface = Color.FromArgb(238, 241, 245);
        private static readonly Color SurfaceRaised = Color.FromArgb(250, 251, 253);
        private static readonly Color SurfaceInset = Color.White;
        private static readonly Color GridBack = Color.White;
        private static readonly Color GridAltBack = Color.FromArgb(247, 249, 252);
        private static readonly Color GridHeader = Color.FromArgb(225, 231, 238);
        private static readonly Color GridLine = Color.FromArgb(211, 218, 226);
        private static readonly Color TextPrimary = Color.FromArgb(29, 36, 45);
        private static readonly Color TextSecondary = Color.FromArgb(83, 96, 112);
        private static readonly Color Accent = Color.FromArgb(47, 111, 159);
        private static readonly Color ButtonBack = Color.FromArgb(232, 237, 243);
        private static readonly Color ProgressAccent = Color.FromArgb(48, 191, 132);

        public void ApplyTheme(
            CacheSave database,
            Control owner,
            ComboBox listComboBox,
            MenuStrip mainMenu,
            ContextMenuStrip itemMenu,
            ColorProgressBar.ColorProgressBar progressBar,
            Label headerLabel,
            CheckBox searchAll,
            CheckBox searchExact,
            CheckBox searchMatchCase,
            TextBox offsetText,
            TextBox searchText,
            TextBox setValueText,
            ListBox searchSuggestionList,
            DataGridView elementGrid,
            DataGridView itemGrid,
            Button searchButton,
            Button setValueButton,
            Button inlinePickIconButton,
            Button descriptionSaveButton,
            TextBox descriptionEditor,
            RichTextBox descriptionPreview,
            Label descriptionStatusLabel,
            Func<ToolStripRenderer> rendererFactory,
            ItemListThemeService itemListThemeService)
        {
            if (owner != null)
            {
                owner.BackColor = Surface;
                ApplyModernContainerTheme(owner);
            }

            if (listComboBox != null)
            {
                listComboBox.DrawMode = DrawMode.Normal;
                listComboBox.FlatStyle = FlatStyle.Standard;
                listComboBox.BackColor = SurfaceInset;
                listComboBox.ForeColor = TextPrimary;
            }

            if (mainMenu != null)
            {
                mainMenu.RenderMode = ToolStripRenderMode.Professional;
                mainMenu.BackColor = SurfaceRaised;
                mainMenu.ForeColor = TextPrimary;
                mainMenu.Renderer = new ModernMenuRenderer();
                StyleToolStripItems(mainMenu.Items);
            }

            if (progressBar != null)
            {
                progressBar.BarColor = ProgressAccent;
                progressBar.BorderColor = Color.Transparent;
            }

            if (headerLabel != null)
            {
                headerLabel.ForeColor = TextSecondary;
            }

            if (searchAll != null)
            {
                StyleCheckBox(searchAll);
            }
            if (searchExact != null)
            {
                StyleCheckBox(searchExact);
            }
            if (searchMatchCase != null)
            {
                StyleCheckBox(searchMatchCase);
            }

            if (offsetText != null)
            {
                StyleTextBox(offsetText);
            }
            if (searchText != null)
            {
                StyleTextBox(searchText);
            }
            if (setValueText != null)
            {
                StyleTextBox(setValueText);
            }

            if (searchSuggestionList != null)
            {
                searchSuggestionList.BackColor = SurfaceInset;
                searchSuggestionList.ForeColor = TextPrimary;
            }

            if (elementGrid != null)
            {
                if (itemListThemeService != null)
                {
                    itemListThemeService.ApplyDarkTheme(elementGrid);
                }
            }

            if (itemGrid != null)
            {
                ApplyInspectorGridTheme(itemGrid);
            }

            if (searchButton != null)
            {
                StyleButton(searchButton);
            }
            if (setValueButton != null)
            {
                StyleButton(setValueButton);
            }
            if (inlinePickIconButton != null)
            {
                StyleButton(inlinePickIconButton);
            }
            if (descriptionSaveButton != null)
            {
                StyleButton(descriptionSaveButton);
            }
            if (descriptionEditor != null)
            {
                descriptionEditor.BackColor = SurfaceInset;
                descriptionEditor.ForeColor = TextPrimary;
            }
            if (descriptionPreview != null)
            {
                descriptionPreview.BackColor = Color.FromArgb(24, 26, 30);
                descriptionPreview.ForeColor = Color.White;
            }
            if (descriptionStatusLabel != null)
            {
                descriptionStatusLabel.ForeColor = TextSecondary;
            }

            if (itemMenu != null)
            {
                itemMenu.Renderer = new ModernMenuRenderer();
                itemMenu.BackColor = SurfaceRaised;
                itemMenu.ForeColor = TextPrimary;
                StyleToolStripItems(itemMenu.Items);
            }
        }

        private static void ApplyModernContainerTheme(Control control)
        {
            if (control == null)
            {
                return;
            }

            foreach (Control child in control.Controls)
            {
                if (child is Panel || child is TableLayoutPanel || child is FlowLayoutPanel)
                {
                    child.BackColor = Surface;
                    child.ForeColor = TextPrimary;
                }
                else if (child is TabPage)
                {
                    child.BackColor = Surface;
                    child.ForeColor = TextPrimary;
                }
                else if (child is Label)
                {
                    child.ForeColor = TextSecondary;
                }

                ApplyModernContainerTheme(child);
            }
        }

        private static void ApplyInspectorGridTheme(DataGridView grid)
        {
            Color labelBack = Color.FromArgb(232, 237, 243);
            Color labelAltBack = Color.FromArgb(225, 231, 238);
            Color valueBack = Color.White;
            Color valueAltBack = Color.FromArgb(250, 251, 253);
            Color valueSelection = Color.FromArgb(47, 111, 159);
            Color labelSelection = Color.FromArgb(202, 211, 222);

            grid.BackgroundColor = GridBack;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = Color.FromArgb(224, 230, 237);
            grid.EnableHeadersVisualStyles = false;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.DefaultCellStyle.BackColor = valueBack;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = valueSelection;
            grid.DefaultCellStyle.SelectionForeColor = Color.White;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.AlternatingRowsDefaultCellStyle.BackColor = valueAltBack;
            grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeader;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeader;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            grid.RowHeadersDefaultCellStyle.BackColor = SurfaceRaised;
            grid.RowHeadersDefaultCellStyle.ForeColor = TextSecondary;
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = Accent;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = Color.White;
            grid.RowHeadersVisible = false;
            if (grid.Columns.Count > 0)
            {
                DataGridViewColumn nameColumn = grid.Columns[0];
                nameColumn.ReadOnly = true;
                nameColumn.DefaultCellStyle.BackColor = labelBack;
                nameColumn.DefaultCellStyle.ForeColor = TextSecondary;
                nameColumn.DefaultCellStyle.SelectionBackColor = labelSelection;
                nameColumn.DefaultCellStyle.SelectionForeColor = TextPrimary;
                nameColumn.DefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                nameColumn.DefaultCellStyle.Padding = new Padding(10, 0, 8, 0);
            }
            if (grid.Columns.Count > 1)
            {
                grid.Columns[1].Visible = false;
            }
            if (grid.Columns.Count > 2)
            {
                DataGridViewColumn valueColumn = grid.Columns[2];
                valueColumn.DefaultCellStyle.BackColor = valueBack;
                valueColumn.DefaultCellStyle.ForeColor = TextPrimary;
                valueColumn.DefaultCellStyle.SelectionBackColor = valueSelection;
                valueColumn.DefaultCellStyle.SelectionForeColor = Color.White;
                valueColumn.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
                valueColumn.DefaultCellStyle.Padding = new Padding(10, 0, 8, 0);
            }
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = SurfaceInset;
            textBox.ForeColor = TextPrimary;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.ForeColor = TextSecondary;
            checkBox.BackColor = Surface;
            checkBox.FlatStyle = FlatStyle.Flat;
        }

        private static void StyleButton(Button button)
        {
            button.BackColor = ButtonBack;
            button.ForeColor = TextPrimary;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = GridLine;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 73, 86);
            button.FlatAppearance.MouseDownBackColor = Accent;
        }

        private static void StyleToolStripItems(ToolStripItemCollection items)
        {
            if (items == null)
            {
                return;
            }

            foreach (ToolStripItem item in items)
            {
                item.BackColor = SurfaceRaised;
                item.ForeColor = TextPrimary;
                ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                if (menuItem != null && menuItem.DropDownItems != null)
                {
                    menuItem.DropDown.BackColor = SurfaceRaised;
                    menuItem.DropDown.ForeColor = TextPrimary;
                    StyleToolStripItems(menuItem.DropDownItems);
                }
            }
        }

        private sealed class ModernMenuRenderer : ToolStripProfessionalRenderer
        {
            public ModernMenuRenderer()
                : base(new ModernMenuColorTable())
            {
            }

            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (SolidBrush brush = new SolidBrush(SurfaceRaised))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                Rectangle bounds = new Rectangle(Point.Empty, e.Item.Size);
                bool topLevel = e.Item.Owner is MenuStrip;
                Color back = SurfaceRaised;
                if (e.Item.Selected && !topLevel)
                {
                    back = Color.FromArgb(225, 231, 238);
                }

                using (SolidBrush brush = new SolidBrush(back))
                {
                    e.Graphics.FillRectangle(brush, bounds);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                if (e.ToolStrip is MenuStrip)
                {
                    return;
                }

                using (Pen pen = new Pen(Color.FromArgb(195, 205, 217)))
                {
                    Rectangle bounds = new Rectangle(Point.Empty, e.ToolStrip.Size);
                    bounds.Width -= 1;
                    bounds.Height -= 1;
                    e.Graphics.DrawRectangle(pen, bounds);
                }
            }
        }

        private sealed class ModernMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected
            {
                get { return Color.FromArgb(225, 231, 238); }
            }

            public override Color MenuItemBorder
            {
                get { return Color.FromArgb(195, 205, 217); }
            }

            public override Color MenuItemSelectedGradientBegin
            {
                get { return Color.FromArgb(225, 231, 238); }
            }

            public override Color MenuItemSelectedGradientEnd
            {
                get { return Color.FromArgb(225, 231, 238); }
            }

            public override Color ToolStripDropDownBackground
            {
                get { return SurfaceRaised; }
            }

            public override Color ImageMarginGradientBegin
            {
                get { return SurfaceRaised; }
            }

            public override Color ImageMarginGradientMiddle
            {
                get { return SurfaceRaised; }
            }

            public override Color ImageMarginGradientEnd
            {
                get { return SurfaceRaised; }
            }

            public override Color MenuBorder
            {
                get { return Color.FromArgb(195, 205, 217); }
            }
        }

        private static Color ResolveThemeColor(CacheSave database, int index, Color fallback)
        {
            if (database == null || database.arrTheme == null || database.arrTheme.Count <= index)
            {
                return fallback;
            }

            Color themeColor = Color.FromName(database.arrTheme[index]);
            return themeColor.IsKnownColor || themeColor.IsNamedColor || themeColor.IsSystemColor
                ? themeColor
                : fallback;
        }
    }
}

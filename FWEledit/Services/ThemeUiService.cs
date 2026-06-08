using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ThemeUiService
    {
        private static bool currentDarkMode;
        private static Color Surface { get { return currentDarkMode ? Color.FromArgb(23, 26, 31) : Color.FromArgb(238, 241, 245); } }
        private static Color SurfaceRaised { get { return currentDarkMode ? Color.FromArgb(31, 35, 42) : Color.FromArgb(250, 251, 253); } }
        private static Color SurfaceInset { get { return currentDarkMode ? Color.FromArgb(18, 21, 26) : Color.White; } }
        private static Color GridBack { get { return currentDarkMode ? Color.FromArgb(18, 21, 26) : Color.White; } }
        private static Color GridAltBack { get { return currentDarkMode ? Color.FromArgb(22, 26, 32) : Color.FromArgb(247, 249, 252); } }
        private static Color GridHeader { get { return currentDarkMode ? Color.FromArgb(38, 44, 53) : Color.FromArgb(225, 231, 238); } }
        private static Color GridLine { get { return currentDarkMode ? Color.FromArgb(50, 58, 70) : Color.FromArgb(211, 218, 226); } }
        private static Color TextPrimary { get { return currentDarkMode ? Color.FromArgb(229, 234, 242) : Color.FromArgb(29, 36, 45); } }
        private static Color TextSecondary { get { return currentDarkMode ? Color.FromArgb(156, 169, 187) : Color.FromArgb(83, 96, 112); } }
        private static Color Accent { get { return currentDarkMode ? Color.FromArgb(68, 132, 184) : Color.FromArgb(47, 111, 159); } }
        private static Color ButtonBack { get { return currentDarkMode ? Color.FromArgb(41, 48, 59) : Color.FromArgb(232, 237, 243); } }
        private static Color ProgressAccent { get { return currentDarkMode ? Color.FromArgb(52, 211, 153) : Color.FromArgb(48, 191, 132); } }

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
            DataGridView referencesGrid,
            Button searchButton,
            Button setValueButton,
            Button inlinePickIconButton,
            Button themeToggleButton,
            Button descriptionSaveButton,
            TextBox descriptionEditor,
            RichTextBox descriptionPreview,
            Label descriptionStatusLabel,
            Func<ToolStripRenderer> rendererFactory,
            ItemListThemeService itemListThemeService,
            bool darkMode)
        {
            currentDarkMode = darkMode;

            if (owner != null)
            {
                owner.BackColor = Surface;
                ApplyNativeTheme(owner);
                ApplyModernContainerTheme(owner);
            }

            if (listComboBox != null)
            {
                listComboBox.DrawMode = DrawMode.OwnerDrawFixed;
                listComboBox.FlatStyle = FlatStyle.Flat;
                listComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                listComboBox.BackColor = SurfaceInset;
                listComboBox.ForeColor = TextPrimary;
                listComboBox.DrawItem -= comboBox_DrawItem;
                listComboBox.DrawItem += comboBox_DrawItem;
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
                    itemListThemeService.ApplyTheme(elementGrid, darkMode);
                }
            }

            if (itemGrid != null)
            {
                ApplyInspectorGridTheme(itemGrid);
            }
            if (referencesGrid != null)
            {
                ApplyReferencesGridTheme(referencesGrid);
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
            if (themeToggleButton != null)
            {
                StyleButton(themeToggleButton);
            }
            if (descriptionSaveButton != null)
            {
                StyleButton(descriptionSaveButton);
            }
            if (descriptionEditor != null)
            {
                descriptionEditor.BackColor = SurfaceInset;
                descriptionEditor.ForeColor = TextPrimary;
                descriptionEditor.BorderStyle = currentDarkMode ? BorderStyle.None : BorderStyle.FixedSingle;
            }
            if (descriptionPreview != null)
            {
                descriptionPreview.BackColor = Color.FromArgb(24, 26, 30);
                descriptionPreview.ForeColor = Color.White;
                descriptionPreview.BorderStyle = currentDarkMode ? BorderStyle.None : BorderStyle.FixedSingle;
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

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, SetLastError = false)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private static void ApplyNativeTheme(Control control)
        {
            if (control == null)
            {
                return;
            }

            if (control.IsHandleCreated)
            {
                TryApplyNativeTheme(control);
            }
            control.HandleCreated -= control_HandleCreated;
            control.HandleCreated += control_HandleCreated;

            foreach (Control child in control.Controls)
            {
                ApplyNativeTheme(child);
            }
        }

        private static void control_HandleCreated(object sender, EventArgs e)
        {
            TryApplyNativeTheme(sender as Control);
        }

        private static void TryApplyNativeTheme(Control control)
        {
            if (control == null)
            {
                return;
            }

            try
            {
                string themeName = currentDarkMode ? "DarkMode_Explorer" : "Explorer";
                SetWindowTheme(control.Handle, themeName, null);
            }
            catch
            {
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
                else if (child is SplitContainer)
                {
                    child.BackColor = currentDarkMode ? Color.FromArgb(44, 52, 63) : GridLine;
                    child.ForeColor = TextPrimary;
                }
                else if (child is TabControl)
                {
                    StyleTabControl((TabControl)child);
                }
                else if (child is TabPage)
                {
                    child.BackColor = Surface;
                    child.ForeColor = TextPrimary;
                }
                else if (child is Label)
                {
                    Label label = (Label)child;
                    if (label.Padding.Left > 0 || label.Height <= 30)
                    {
                        label.BackColor = GridHeader;
                    }
                    child.ForeColor = TextSecondary;
                }
                else if (child is TextBox)
                {
                    StyleTextBox((TextBox)child);
                }
                else if (child is CheckBox)
                {
                    StyleCheckBox((CheckBox)child);
                }
                else if (child is ScrollBar)
                {
                    StyleScrollBar((ScrollBar)child);
                }
                else if (child is Button)
                {
                    StyleButton((Button)child);
                }
                else if (child is ComboBox)
                {
                    ComboBox combo = (ComboBox)child;
                    combo.BackColor = SurfaceInset;
                    combo.ForeColor = TextPrimary;
                    combo.FlatStyle = FlatStyle.Flat;
                }

                ApplyModernContainerTheme(child);
            }
        }

        private static void StyleScrollBar(ScrollBar scrollBar)
        {
            scrollBar.BackColor = currentDarkMode ? Color.FromArgb(31, 35, 42) : Color.FromArgb(238, 241, 245);
            scrollBar.ForeColor = currentDarkMode ? Color.FromArgb(156, 169, 187) : Color.FromArgb(83, 96, 112);
        }

        private static void StyleTabControl(TabControl tabs)
        {
            ThemedTabControl themedTabs = tabs as ThemedTabControl;
            if (themedTabs != null)
            {
                themedTabs.ApplyTheme(currentDarkMode);
                return;
            }

            tabs.BackColor = Surface;
            tabs.ForeColor = TextPrimary;
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(92, 24);
            tabs.Padding = new Point(10, 3);
            tabs.DrawItem -= tabs_DrawItem;
            tabs.DrawItem += tabs_DrawItem;

            foreach (TabPage page in tabs.TabPages)
            {
                page.BackColor = Surface;
                page.ForeColor = TextPrimary;
            }

            tabs.Invalidate();
        }

        private static void tabs_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabControl tabs = sender as TabControl;
            if (tabs == null || e.Index < 0 || e.Index >= tabs.TabPages.Count)
            {
                return;
            }

            Rectangle bounds = tabs.GetTabRect(e.Index);
            bool selected = e.Index == tabs.SelectedIndex;
            Color back = selected ? Surface : (currentDarkMode ? Color.FromArgb(28, 33, 40) : Color.FromArgb(232, 237, 243));
            Color border = selected ? Surface : GridLine;
            Color text = selected ? TextPrimary : TextSecondary;

            using (SolidBrush brush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(brush, bounds);
            }
            using (Pen pen = new Pen(border))
            {
                Rectangle borderBounds = bounds;
                borderBounds.Width -= 1;
                borderBounds.Height -= 1;
                e.Graphics.DrawRectangle(pen, borderBounds);
            }
            TextRenderer.DrawText(
                e.Graphics,
                tabs.TabPages[e.Index].Text,
                tabs.Font,
                bounds,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static void ApplyInspectorGridTheme(DataGridView grid)
        {
            Color labelBack = currentDarkMode ? Color.FromArgb(32, 38, 47) : Color.FromArgb(232, 237, 243);
            Color valueBack = currentDarkMode ? Color.FromArgb(18, 21, 26) : Color.White;
            Color valueAltBack = currentDarkMode ? Color.FromArgb(22, 26, 32) : Color.FromArgb(250, 251, 253);
            Color valueSelection = Accent;
            Color labelSelection = currentDarkMode ? Color.FromArgb(48, 57, 70) : Color.FromArgb(202, 211, 222);

            grid.BackgroundColor = GridBack;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = currentDarkMode ? Color.FromArgb(38, 44, 54) : Color.FromArgb(224, 230, 237);
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

        private static void ApplyReferencesGridTheme(DataGridView grid)
        {
            Color valueBack = currentDarkMode ? Color.FromArgb(18, 21, 26) : Color.White;
            Color valueAltBack = currentDarkMode ? Color.FromArgb(22, 26, 32) : Color.FromArgb(250, 251, 253);
            Color valueSelection = Accent;

            grid.BackgroundColor = GridBack;
            grid.BorderStyle = BorderStyle.None;
            grid.GridColor = currentDarkMode ? Color.FromArgb(38, 44, 54) : Color.FromArgb(224, 230, 237);
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
            grid.RowHeadersVisible = false;

            if (grid.Columns.Count > 2)
            {
                DataGridViewColumn iconColumn = grid.Columns[2];
                iconColumn.DefaultCellStyle.BackColor = valueBack;
                iconColumn.DefaultCellStyle.SelectionBackColor = valueSelection;
                iconColumn.DefaultCellStyle.NullValue = null;
            }
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = SurfaceInset;
            textBox.ForeColor = TextPrimary;
            textBox.BorderStyle = currentDarkMode ? BorderStyle.None : BorderStyle.FixedSingle;
        }

        private static void comboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null)
            {
                return;
            }

            e.DrawBackground();
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color back = selected ? (currentDarkMode ? Color.FromArgb(43, 51, 63) : Color.FromArgb(225, 231, 238)) : SurfaceInset;
            Color text = TextPrimary;

            using (SolidBrush brush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            if (e.Index >= 0 && e.Index < combo.Items.Count)
            {
                Rectangle textBounds = new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height);
                TextRenderer.DrawText(
                    e.Graphics,
                    combo.Items[e.Index].ToString(),
                    e.Font,
                    textBounds,
                    text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
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
            button.FlatAppearance.MouseOverBackColor = currentDarkMode ? Color.FromArgb(55, 64, 78) : Color.FromArgb(219, 226, 235);
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
                    back = currentDarkMode ? Color.FromArgb(43, 51, 63) : Color.FromArgb(225, 231, 238);
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

                using (Pen pen = new Pen(GridLine))
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
                get { return currentDarkMode ? Color.FromArgb(43, 51, 63) : Color.FromArgb(225, 231, 238); }
            }

            public override Color MenuItemBorder
            {
                get { return GridLine; }
            }

            public override Color MenuItemSelectedGradientBegin
            {
                get { return currentDarkMode ? Color.FromArgb(43, 51, 63) : Color.FromArgb(225, 231, 238); }
            }

            public override Color MenuItemSelectedGradientEnd
            {
                get { return currentDarkMode ? Color.FromArgb(43, 51, 63) : Color.FromArgb(225, 231, 238); }
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
                get { return GridLine; }
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

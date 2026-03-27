using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ThemeUiService
    {
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
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (owner != null)
            {
                owner.BackColor = Color.FromName(database.arrTheme[0]);
            }

            if (listComboBox != null)
            {
                listComboBox.DrawMode = DrawMode.OwnerDrawFixed;
                listComboBox.FlatStyle = FlatStyle.Flat;
                listComboBox.BackColor = Color.FromName(database.arrTheme[7]);
            }

            if (mainMenu != null)
            {
                mainMenu.RenderMode = ToolStripRenderMode.Professional;
                mainMenu.BackColor = Color.FromName(database.arrTheme[2]);
            }

            if (progressBar != null)
            {
                progressBar.BarColor = Color.FromName(database.arrTheme[17]);
            }

            if (headerLabel != null)
            {
                headerLabel.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (searchAll != null)
            {
                searchAll.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (searchExact != null)
            {
                searchExact.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (searchMatchCase != null)
            {
                searchMatchCase.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (offsetText != null)
            {
                offsetText.ForeColor = Color.FromName(database.arrTheme[1]);
                offsetText.BackColor = Color.FromName(database.arrTheme[6]);
            }
            if (searchText != null)
            {
                searchText.ForeColor = Color.FromName(database.arrTheme[1]);
                searchText.BackColor = Color.FromName(database.arrTheme[6]);
            }
            if (setValueText != null)
            {
                setValueText.ForeColor = Color.FromName(database.arrTheme[1]);
                setValueText.BackColor = Color.FromName(database.arrTheme[6]);
            }

            if (searchSuggestionList != null)
            {
                searchSuggestionList.BackColor = Color.FromName(database.arrTheme[6]);
                searchSuggestionList.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (elementGrid != null)
            {
                elementGrid.BackgroundColor = Color.FromName(database.arrTheme[12]);
                elementGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                elementGrid.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                elementGrid.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                elementGrid.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                elementGrid.DefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                elementGrid.DefaultCellStyle.SelectionForeColor = Color.FromName(database.arrTheme[15]);
                elementGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                elementGrid.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                if (itemListThemeService != null)
                {
                    itemListThemeService.ApplyDarkTheme(elementGrid);
                }
            }

            if (itemGrid != null)
            {
                itemGrid.BackgroundColor = Color.FromName(database.arrTheme[12]);
                itemGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                itemGrid.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                itemGrid.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                itemGrid.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                itemGrid.RowHeadersDefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                itemGrid.DefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                itemGrid.DefaultCellStyle.SelectionForeColor = Color.FromName(database.arrTheme[15]);
                itemGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
                itemGrid.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[15]);
            }

            if (searchButton != null)
            {
                searchButton.BackColor = Color.FromName(database.arrTheme[11]);
                searchButton.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (setValueButton != null)
            {
                setValueButton.BackColor = Color.FromName(database.arrTheme[11]);
                setValueButton.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (inlinePickIconButton != null)
            {
                inlinePickIconButton.BackColor = Color.FromName(database.arrTheme[11]);
                inlinePickIconButton.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (descriptionSaveButton != null)
            {
                descriptionSaveButton.BackColor = Color.FromName(database.arrTheme[11]);
                descriptionSaveButton.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (descriptionEditor != null)
            {
                descriptionEditor.BackColor = Color.FromName(database.arrTheme[6]);
                descriptionEditor.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (descriptionPreview != null)
            {
                descriptionPreview.BackColor = Color.FromName(database.arrTheme[12]);
                descriptionPreview.ForeColor = Color.FromName(database.arrTheme[15]);
            }
            if (descriptionStatusLabel != null)
            {
                descriptionStatusLabel.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (rendererFactory != null)
            {
                ToolStripRenderer renderer = rendererFactory();
                if (renderer != null)
                {
                    if (mainMenu != null)
                    {
                        mainMenu.Renderer = renderer;
                    }
                    if (itemMenu != null)
                    {
                        itemMenu.Renderer = renderer;
                    }
                }
            }
        }
    }
}

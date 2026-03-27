using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            ThemeUiService themeUiService,
            Form owner,
            ComboBox listCombo,
            MenuStrip menuStrip,
            ContextMenuStrip contextMenu,
            ColorProgressBar.ColorProgressBar progressBar,
            Label offsetLabel,
            CheckBox searchAll,
            CheckBox searchExact,
            CheckBox searchMatchCase,
            TextBox offsetTextBox,
            TextBox searchBox,
            TextBox setValueBox,
            ListBox suggestionList,
            DataGridView listGrid,
            DataGridView valuesGrid,
            Button searchButton,
            Button setValueButton,
            Button inlinePickIconButton,
            Button descriptionSaveButton,
            TextBox descriptionEditor,
            RichTextBox descriptionPreview,
            Label descriptionStatusLabel,
            Func<ThemeMenuRenderer> menuRendererFactory,
            ItemListThemeService itemListThemeService)
        {
            if (themeUiService == null)
            {
                return;
            }

            themeUiService.ApplyTheme(
                database,
                owner,
                listCombo,
                menuStrip,
                contextMenu,
                progressBar,
                offsetLabel,
                searchAll,
                searchExact,
                searchMatchCase,
                offsetTextBox,
                searchBox,
                setValueBox,
                suggestionList,
                listGrid,
                valuesGrid,
                searchButton,
                setValueButton,
                inlinePickIconButton,
                descriptionSaveButton,
                descriptionEditor,
                descriptionPreview,
                descriptionStatusLabel,
                menuRendererFactory,
                itemListThemeService);
        }

        public void DrawComboBoxItem(
            CacheSave database,
            ThemeComboBoxDrawService drawService,
            ComboBoxThemeRendererService rendererService,
            object sender,
            DrawItemEventArgs e)
        {
            if (drawService == null)
            {
                return;
            }

            if (database != null && database.arrTheme != null)
            {
                drawService.DrawItem(rendererService, sender, e, database.arrTheme);
            }
        }
    }
}

using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigWindowSetupCoordinatorService
    {
        public bool EnsureDataLoaded(ConfigData data)
        {
            return data != null;
        }

        public void ApplyTheme(
            ConfigThemeUiService themeUiService,
            CacheSave database,
            Form owner,
            ComboBox listCombo,
            MenuStrip mainMenu,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            TextBox conversationIndexBox,
            TextBox listNameBox,
            TextBox offsetBox,
            TextBox setNameBox,
            TextBox setTypeBox,
            DataGridView grid,
            Button addRowButton,
            Button copyRowButton,
            Button pasteRowButton,
            Button deleteRowButton)
        {
            if (themeUiService == null)
            {
                return;
            }

            themeUiService.ApplyTheme(
                database,
                owner,
                listCombo,
                mainMenu,
                label1,
                label2,
                label3,
                label4,
                conversationIndexBox,
                listNameBox,
                offsetBox,
                setNameBox,
                setTypeBox,
                grid,
                addRowButton,
                copyRowButton,
                pasteRowButton,
                deleteRowButton,
                () => new ThemeMenuRenderer(() => database != null ? database.arrTheme : null));
        }

        public void DrawComboBoxItem(
            ThemeComboBoxDrawService drawService,
            ComboBoxThemeRendererService rendererService,
            CacheSave database,
            object sender,
            DrawItemEventArgs e)
        {
            if (drawService == null || database == null || database.arrTheme == null)
            {
                return;
            }

            drawService.DrawItem(rendererService, sender, e, database.arrTheme);
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Control owner,
            ComboBox listComboBox,
            MenuStrip mainMenu,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            TextBox conversationListIndexTextBox,
            TextBox listNameTextBox,
            TextBox offsetTextBox,
            TextBox setNameTextBox,
            TextBox setTypeTextBox,
            DataGridView itemGrid,
            Button button1,
            Button button2,
            Button button3,
            Button button4,
            Func<ToolStripRenderer> rendererFactory)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
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

            if (owner != null)
            {
                owner.BackColor = Color.FromName(database.arrTheme[0]);
            }

            if (label1 != null) label1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label2 != null) label2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label3 != null) label3.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label4 != null) label4.ForeColor = Color.FromName(database.arrTheme[1]);

            if (conversationListIndexTextBox != null)
            {
                conversationListIndexTextBox.BackColor = Color.FromName(database.arrTheme[6]);
                conversationListIndexTextBox.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (listNameTextBox != null)
            {
                listNameTextBox.BackColor = Color.FromName(database.arrTheme[6]);
                listNameTextBox.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (offsetTextBox != null)
            {
                offsetTextBox.BackColor = Color.FromName(database.arrTheme[6]);
                offsetTextBox.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (setNameTextBox != null)
            {
                setNameTextBox.BackColor = Color.FromName(database.arrTheme[6]);
                setNameTextBox.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (setTypeTextBox != null)
            {
                setTypeTextBox.BackColor = Color.FromName(database.arrTheme[6]);
                setTypeTextBox.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (itemGrid != null)
            {
                itemGrid.BackgroundColor = Color.FromName(database.arrTheme[12]);
                itemGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                itemGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[1]);
                itemGrid.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                itemGrid.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                itemGrid.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                itemGrid.RowHeadersDefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                itemGrid.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (button1 != null)
            {
                button1.BackColor = Color.FromName(database.arrTheme[11]);
                button1.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (button2 != null)
            {
                button2.BackColor = Color.FromName(database.arrTheme[11]);
                button2.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (button3 != null)
            {
                button3.BackColor = Color.FromName(database.arrTheme[11]);
                button3.ForeColor = Color.FromName(database.arrTheme[1]);
            }
            if (button4 != null)
            {
                button4.BackColor = Color.FromName(database.arrTheme[11]);
                button4.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (rendererFactory != null && mainMenu != null)
            {
                ToolStripRenderer renderer = rendererFactory();
                if (renderer != null)
                {
                    mainMenu.Renderer = renderer;
                }
            }
        }
    }
}

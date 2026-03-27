using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class RulesThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Form form,
            ComboBox listCombo,
            GroupBox group1,
            GroupBox group2,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            RadioButton baseOffset,
            RadioButton recentOffset,
            CheckBox removeList,
            TextBox baseFile,
            TextBox baseOffsetText,
            TextBox baseVersion,
            TextBox recentFile,
            TextBox recentOffsetText,
            TextBox recentVersion,
            DataGridView fieldsGrid,
            DataGridView valuesGrid,
            DataGridViewColumn column6,
            Button browseBase,
            Button browseRecent,
            Button export,
            Button import,
            Button view)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (listCombo != null)
            {
                listCombo.DrawMode = DrawMode.OwnerDrawFixed;
                listCombo.FlatStyle = FlatStyle.Flat;
                listCombo.BackColor = Color.FromName(database.arrTheme[7]);
            }

            if (form != null)
            {
                form.BackColor = Color.FromName(database.arrTheme[0]);
            }

            if (group1 != null) group1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (group2 != null) group2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label1 != null) label1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label2 != null) label2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label3 != null) label3.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label4 != null) label4.ForeColor = Color.FromName(database.arrTheme[1]);
            if (baseOffset != null) baseOffset.ForeColor = Color.FromName(database.arrTheme[1]);
            if (recentOffset != null) recentOffset.ForeColor = Color.FromName(database.arrTheme[1]);
            if (removeList != null) removeList.ForeColor = Color.FromName(database.arrTheme[1]);

            ApplyTextBoxTheme(database, baseFile);
            ApplyTextBoxTheme(database, baseOffsetText);
            ApplyTextBoxTheme(database, baseVersion);
            ApplyTextBoxTheme(database, recentFile);
            ApplyTextBoxTheme(database, recentOffsetText);
            ApplyTextBoxTheme(database, recentVersion);

            ApplyGridTheme(database, fieldsGrid);
            if (column6 != null)
            {
                column6.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
            }
            ApplyGridTheme(database, valuesGrid);

            ApplyButtonTheme(database, browseBase);
            ApplyButtonTheme(database, browseRecent);
            ApplyButtonTheme(database, export);
            ApplyButtonTheme(database, import);
            ApplyButtonTheme(database, view);
        }

        private void ApplyTextBoxTheme(CacheSave database, TextBox textBox)
        {
            if (textBox == null)
            {
                return;
            }
            textBox.BackColor = Color.FromName(database.arrTheme[6]);
            textBox.ForeColor = Color.FromName(database.arrTheme[1]);
        }

        private void ApplyGridTheme(CacheSave database, DataGridView grid)
        {
            if (grid == null)
            {
                return;
            }
            grid.BackgroundColor = Color.FromName(database.arrTheme[12]);
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[1]);
            grid.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
            grid.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[1]);
            grid.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
        }

        private void ApplyButtonTheme(CacheSave database, Button button)
        {
            if (button == null)
            {
                return;
            }
            button.BackColor = Color.FromName(database.arrTheme[11]);
            button.ForeColor = Color.FromName(database.arrTheme[1]);
        }
    }
}

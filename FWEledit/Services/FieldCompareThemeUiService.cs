using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldCompareThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Form form,
            ComboBox listCombo,
            ColorProgressBar.ColorProgressBar progressBar,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            TextBox elementPath,
            TextBox logDir,
            DataGridView fieldsGrid,
            Button browseElement,
            Button browseLog,
            Button cancel,
            Button compare,
            Button loadElement)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (progressBar != null)
            {
                progressBar.Value = 0;
                progressBar.BarColor = Color.FromName(database.arrTheme[17]);
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

            if (label1 != null) label1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label2 != null) label2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label3 != null) label3.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label4 != null) label4.ForeColor = Color.FromName(database.arrTheme[1]);

            if (elementPath != null)
            {
                elementPath.BackColor = Color.FromName(database.arrTheme[6]);
                elementPath.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (logDir != null)
            {
                logDir.BackColor = Color.FromName(database.arrTheme[6]);
                logDir.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            if (fieldsGrid != null)
            {
                fieldsGrid.BackgroundColor = Color.FromName(database.arrTheme[12]);
                fieldsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                fieldsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[1]);
                fieldsGrid.RowHeadersDefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                fieldsGrid.DefaultCellStyle.BackColor = Color.FromName(database.arrTheme[13]);
                fieldsGrid.DefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                fieldsGrid.RowHeadersDefaultCellStyle.SelectionBackColor = Color.FromName(database.arrTheme[14]);
                fieldsGrid.RowHeadersDefaultCellStyle.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            ApplyButtonTheme(database, browseElement);
            ApplyButtonTheme(database, browseLog);
            ApplyButtonTheme(database, cancel);
            ApplyButtonTheme(database, compare);
            ApplyButtonTheme(database, loadElement);
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

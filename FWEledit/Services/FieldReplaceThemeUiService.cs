using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class FieldReplaceThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Form form,
            ColorProgressBar.ColorProgressBar progressBar,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            ComboBox fieldCombo,
            ComboBox listCombo,
            TextBox elementPath,
            TextBox logDir,
            Button browseElement,
            Button browseLog,
            Button loadElement,
            Button cancel,
            Button replace)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (form != null)
            {
                form.BackColor = Color.FromName(database.arrTheme[0]);
            }

            if (progressBar != null)
            {
                progressBar.BarColor = Color.FromName(database.arrTheme[17]);
            }

            if (label1 != null) label1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label2 != null) label2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label3 != null) label3.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label4 != null) label4.ForeColor = Color.FromName(database.arrTheme[1]);

            ApplyComboTheme(database, fieldCombo);
            ApplyComboTheme(database, listCombo);

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

            ApplyButtonTheme(database, browseElement);
            ApplyButtonTheme(database, browseLog);
            ApplyButtonTheme(database, loadElement);
            ApplyButtonTheme(database, cancel);
            ApplyButtonTheme(database, replace);
        }

        private void ApplyComboTheme(CacheSave database, ComboBox combo)
        {
            if (combo == null)
            {
                return;
            }

            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.FlatStyle = FlatStyle.Flat;
            combo.BackColor = Color.FromName(database.arrTheme[7]);
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

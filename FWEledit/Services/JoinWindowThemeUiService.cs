using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class JoinWindowThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Form form,
            Label label1,
            Label label2,
            Label label3,
            Label label4,
            Label label5,
            TextBox elementFile,
            TextBox logDir,
            CheckBox addNew,
            CheckBox backupNew,
            CheckBox replaceChanged,
            CheckBox backupChanged,
            CheckBox removeMissing,
            CheckBox backupMissing,
            Button browseElement,
            Button browseLog,
            Button cancel,
            Button ok)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (form != null)
            {
                form.BackColor = Color.FromName(database.arrTheme[0]);
            }

            if (label1 != null) label1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label2 != null) label2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label3 != null) label3.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label4 != null) label4.ForeColor = Color.FromName(database.arrTheme[1]);
            if (label5 != null) label5.ForeColor = Color.FromName(database.arrTheme[1]);

            ApplyTextBoxTheme(database, elementFile);
            ApplyTextBoxTheme(database, logDir);

            ApplyCheckTheme(database, addNew);
            ApplyCheckTheme(database, backupNew);
            ApplyCheckTheme(database, replaceChanged);
            ApplyCheckTheme(database, backupChanged);
            ApplyCheckTheme(database, removeMissing);
            ApplyCheckTheme(database, backupMissing);

            ApplyButtonTheme(database, browseElement);
            ApplyButtonTheme(database, browseLog);
            ApplyButtonTheme(database, cancel);
            ApplyButtonTheme(database, ok);
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

        private void ApplyCheckTheme(CacheSave database, CheckBox checkBox)
        {
            if (checkBox == null)
            {
                return;
            }
            checkBox.ForeColor = Color.FromName(database.arrTheme[1]);
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

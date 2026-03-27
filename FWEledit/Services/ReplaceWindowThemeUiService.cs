using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ReplaceWindowThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Form form,
            ComboBox operationCombo,
            GroupBox group1,
            GroupBox group2,
            GroupBox group3,
            TextBox fieldText,
            TextBox itemText,
            TextBox listText,
            TextBox newValue,
            TextBox oldValue,
            RadioButton recalc,
            RadioButton replace,
            NumericUpDown operand,
            Button cancel,
            Button replaceButton)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (operationCombo != null)
            {
                operationCombo.DrawMode = DrawMode.OwnerDrawFixed;
                operationCombo.FlatStyle = FlatStyle.Flat;
                operationCombo.BackColor = Color.FromName(database.arrTheme[7]);
            }

            if (form != null)
            {
                form.BackColor = Color.FromName(database.arrTheme[0]);
            }

            if (group1 != null) group1.ForeColor = Color.FromName(database.arrTheme[1]);
            if (group2 != null) group2.ForeColor = Color.FromName(database.arrTheme[1]);
            if (group3 != null) group3.ForeColor = Color.FromName(database.arrTheme[1]);

            ApplyTextBoxTheme(database, fieldText);
            ApplyTextBoxTheme(database, itemText);
            ApplyTextBoxTheme(database, listText);
            ApplyTextBoxTheme(database, newValue);
            ApplyTextBoxTheme(database, oldValue);

            if (recalc != null) recalc.ForeColor = Color.FromName(database.arrTheme[1]);
            if (replace != null) replace.ForeColor = Color.FromName(database.arrTheme[1]);

            if (operand != null)
            {
                operand.BackColor = Color.FromName(database.arrTheme[6]);
                operand.ForeColor = Color.FromName(database.arrTheme[1]);
            }

            ApplyButtonTheme(database, cancel);
            ApplyButtonTheme(database, replaceButton);
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

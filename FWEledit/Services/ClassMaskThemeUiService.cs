using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ClassMaskThemeUiService
    {
        public void ApplyTheme(
            CacheSave database,
            Form form,
            CheckBox ar,
            CheckBox ass,
            CheckBox bar,
            CheckBox bm,
            CheckBox cle,
            CheckBox du,
            CheckBox my,
            CheckBox psy,
            CheckBox se,
            CheckBox st,
            CheckBox ven,
            CheckBox wiz,
            NumericUpDown mask)
        {
            if (database == null || database.arrTheme == null)
            {
                return;
            }

            if (form != null)
            {
                form.BackColor = Color.FromName(database.arrTheme[0]);
            }

            ApplyCheckTheme(database, ar);
            ApplyCheckTheme(database, ass);
            ApplyCheckTheme(database, bar);
            ApplyCheckTheme(database, bm);
            ApplyCheckTheme(database, cle);
            ApplyCheckTheme(database, du);
            ApplyCheckTheme(database, my);
            ApplyCheckTheme(database, psy);
            ApplyCheckTheme(database, se);
            ApplyCheckTheme(database, st);
            ApplyCheckTheme(database, ven);
            ApplyCheckTheme(database, wiz);

            if (mask != null)
            {
                mask.BackColor = Color.FromName(database.arrTheme[6]);
                mask.ForeColor = Color.FromName(database.arrTheme[1]);
            }
        }

        private void ApplyCheckTheme(CacheSave database, CheckBox checkBox)
        {
            if (checkBox == null)
            {
                return;
            }
            checkBox.ForeColor = Color.FromName(database.arrTheme[1]);
        }
    }
}

using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void textBox_value_enter(object sender, EventArgs e)
        {
            if (textBox_SetValue != null)
            {
                textBox_SetValue.SelectAll();
            }
        }


        private void textBox_value_leave(object sender, EventArgs e)
        {
        }

        private void raw_value_editor_key_down(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                AdjustRawValueEditor(1);
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                AdjustRawValueEditor(-1);
                e.SuppressKeyPress = true;
            }
        }

        private void raw_value_editor_changed(object sender, EventArgs e)
        {
        }
    }
}





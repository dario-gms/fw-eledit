using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class ReplaceWindow : Form
    {
        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            replaceWindowCoordinatorService.DrawComboBoxItem(
                themeComboBoxDrawService,
                comboBoxThemeRendererService,
                database,
                sender,
                e);
        }


        private void click_replace(object sender, EventArgs ea)
		{
            replaceWindowCoordinatorService.HandleReplace(
                replaceWindowUiService,
                viewModel,
                textBox_list,
                textBox_item,
                textBox_field,
                textBox_oldValue,
                textBox_newValue,
                comboBox_operation,
                numericUpDown_operand,
                radioButton_replace,
                radioButton_recalculate,
                cursor => Cursor = cursor,
                message => MessageBox.Show(message),
                Close);
		}


        private void click_close(object sender, EventArgs e)
		{
			replaceWindowCoordinatorService.HandleClose(Close);
		}
    }
}



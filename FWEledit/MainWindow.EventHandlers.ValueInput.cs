using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void textBox_value_enter(object sender, EventArgs e)
        {
            mainWindowValueInputCoordinatorService.HandleEnter(
                valueInputPlaceholderService,
                textBox_SetValue,
                placeholderTextService,
                "Set Value");
        }


        private void textBox_value_leave(object sender, EventArgs e)
        {
            mainWindowValueInputCoordinatorService.HandleLeave(
                valueInputPlaceholderService,
                textBox_SetValue,
                placeholderTextService,
                "Set Value");
        }
    }
}





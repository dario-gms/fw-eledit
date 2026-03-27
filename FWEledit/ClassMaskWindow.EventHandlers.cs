using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class ClassMaskWindow : Form
    {
        private void change_Dec(object sender, EventArgs e)
		{
            classMaskCoordinatorService.HandleDecimalChanged(
                viewModel,
                numericUpDown_mask,
                ref lockCheckBox,
                checkBox_BM,
                checkBox_WIZ,
                checkBox_PSY,
                checkBox_VEN,
                checkBox_BAR,
                checkBox_AS,
                checkBox_AR,
                checkBox_CLE,
                checkBox_SE,
                checkBox_MY,
                checkBox_DU,
                checkBox_ST);
		}


        private void change_Bin(object sender, EventArgs e)
		{
            classMaskCoordinatorService.HandleBinaryChanged(
                viewModel,
                numericUpDown_mask,
                ref lockCheckBox,
                checkBox_BM,
                checkBox_WIZ,
                checkBox_PSY,
                checkBox_VEN,
                checkBox_BAR,
                checkBox_AS,
                checkBox_AR,
                checkBox_CLE,
                checkBox_SE,
                checkBox_MY,
                checkBox_DU,
                checkBox_ST);
		}
    }
}



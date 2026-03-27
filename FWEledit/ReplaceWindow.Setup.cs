using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class ReplaceWindow : Form
    {
        public ReplaceWindow(ISessionService sessionService, eListCollection ListCollection)
		{
            this.sessionService = sessionService ?? new SessionService();
            database = this.sessionService.Database;
            viewModel = new ReplaceWindowViewModel(new ReplaceService());
            viewModel.ListCollection = ListCollection;
			InitializeComponent();
            colorTheme();
			comboBox_operation.SelectedIndex = 0;
		}


        private void colorTheme()
        {
            replaceWindowThemeUiService.ApplyTheme(
                database,
                this,
                comboBox_operation,
                groupBox1,
                groupBox2,
                groupBox3,
                textBox_field,
                textBox_item,
                textBox_list,
                textBox_newValue,
                textBox_oldValue,
                radioButton_recalculate,
                radioButton_replace,
                numericUpDown_operand,
                button_cancel,
                button_replace);
        }
    }
}



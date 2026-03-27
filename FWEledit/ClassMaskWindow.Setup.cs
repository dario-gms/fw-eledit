using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class ClassMaskWindow : Form
    {
        public ClassMaskWindow()
            : this(new SessionService())
        {
        }

        public ClassMaskWindow(ISessionService sessionService)
		{
            this.sessionService = sessionService ?? new SessionService();
            viewModel = new ClassMaskViewModel(new ClassMaskService());
			InitializeComponent();
            database = this.sessionService.Database;
            colorTheme();
        }


        private void colorTheme()
        {
            classMaskThemeUiService.ApplyTheme(
                database,
                this,
                checkBox_AR,
                checkBox_AS,
                checkBox_BAR,
                checkBox_BM,
                checkBox_CLE,
                checkBox_DU,
                checkBox_MY,
                checkBox_PSY,
                checkBox_SE,
                checkBox_ST,
                checkBox_VEN,
                checkBox_WIZ,
                numericUpDown_mask);
        }
    }
}



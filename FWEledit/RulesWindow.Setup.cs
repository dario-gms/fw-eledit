using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class RulesWindow : Form
    {
        public RulesWindow(ISessionService sessionService, ref ColorProgressBar.ColorProgressBar progressBar_prog)
		{
            this.sessionService = sessionService ?? new SessionService();
            viewModel = new RulesWindowViewModel(new RulesService());
            cpb2_prog = progressBar_prog;
			InitializeComponent();
            database = this.sessionService.Database;
            colorTheme();
        }


        private void colorTheme()
        {
            rulesWindowCoordinatorService.ApplyTheme(
                rulesThemeUiService,
                database,
                this,
                comboBox_lists,
                groupBox1,
                groupBox2,
                label1,
                label2,
                label3,
                label4,
                radioButton_baseOffset,
                radioButton_recentOffset,
                checkBox_removeList,
                textBox_baseFile,
                textBox_baseOffset,
                textBox_baseVersion,
                textBox_recentFile,
                textBox_recentOffset,
                textBox_recentVersion,
                dataGridView_fields,
                dataGridView_values,
                Column6,
                button_browseBase,
                button_browseRecent,
                button_export,
                button_import,
                button_view);
        }


        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            rulesWindowCoordinatorService.DrawComboBoxItem(
                themeComboBoxDrawService,
                comboBoxThemeRendererService,
                database,
                sender,
                e);
        }
    }
}


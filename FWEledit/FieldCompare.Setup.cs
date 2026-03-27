using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class FieldCompare : Form
    {
        public FieldCompare(ISessionService sessionService, eListCollection ListCollection, eListConversation ListConversation, ref ColorProgressBar.ColorProgressBar progressBar_prog)
		{
            this.sessionService = sessionService ?? new SessionService();
            viewModel = new FieldCompareViewModel(new FieldCompareService());
            viewModel.Source = ListCollection;
            viewModel.SourceConversation = ListConversation;
            cpb2_prog = progressBar_prog;
            InitializeComponent();
            fieldCompareUiService.PopulateListCombo(viewModel.Source, comboBox_List);
            CheckCompareButton(null, null);
            database = this.sessionService.Database;
            colorTheme();
        }


        private void colorTheme()
        {
            fieldCompareCoordinatorService.ApplyTheme(
                fieldCompareThemeUiService,
                database,
                this,
                comboBox_List,
                cpb2,
                label1,
                label2,
                label3,
                label4,
                textBox_ElementPath,
                textBox_LogDir,
                dataGridView_fields,
                button_browseElement,
                button_browseLogDit,
                button_cancel,
                button_compare,
                button_LoadElement);
        }


        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            fieldCompareCoordinatorService.DrawComboBoxItem(
                themeComboBoxDrawService,
                comboBoxThemeRendererService,
                database,
                sender,
                e);
        }
    }
}



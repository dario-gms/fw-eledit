using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class FieldReplaceWindow : Form
    {
        public FieldReplaceWindow(ISessionService sessionService, eListCollection ListCollection, eListConversation ListConversation, ref ColorProgressBar.ColorProgressBar progressBar_prog)
		{
            this.sessionService = sessionService ?? new SessionService();
            database = this.sessionService.Database;
            viewModel = new FieldReplaceViewModel(new FieldReplaceService());
            viewModel.Source = ListCollection;
            viewModel.SourceConversation = ListConversation;
            cpb2_prog = progressBar_prog;
            InitializeComponent();
            cpb2.Value = 0;
            colorTheme();
            fieldReplaceUiService.PopulateListCombo(viewModel.Source, comboBox_List);
            CheckReplaceButton(null, null);
		}


        private void colorTheme()
        {
            fieldReplaceCoordinatorService.ApplyTheme(
                fieldReplaceThemeUiService,
                database,
                this,
                cpb2,
                label1,
                label2,
                label3,
                label4,
                comboBox_Field,
                comboBox_List,
                textBox_ElementPath,
                textBox_LogDir,
                button_browseElement,
                button_browseLogDit,
                button_LoadElement,
                button_cancel,
                button_replace);
        }


        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            fieldReplaceCoordinatorService.DrawComboBoxItem(
                themeComboBoxDrawService,
                comboBoxThemeRendererService,
                database,
                sender,
                e);
        }
    }
}



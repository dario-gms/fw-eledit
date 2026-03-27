using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class ConfigWindow : Form
    {
        public ConfigWindow()
            : this(new SessionService())
        {
        }

        public ConfigWindow(ISessionService sessionService)
		{
            this.sessionService = sessionService ?? new SessionService();
			InitializeComponent();
            database = this.sessionService.Database;
            viewModel = new ConfigWindowViewModel(new ConfigFileService());
            dialogService = new DialogService();
            colorTheme();
		}


        private bool EnsureDataLoaded()
        {
            return configWindowSetupCoordinatorService.EnsureDataLoaded(Data);
        }


        private void colorTheme()
        {
            configWindowSetupCoordinatorService.ApplyTheme(
                configThemeUiService,
                database,
                this,
                comboBox_lists,
                menuStrip_mainMenu,
                label1,
                label2,
                label3,
                label4,
                textBox_conversationListIndex,
                textBox_listName,
                textBox_offset,
                textBox_SetName,
                textBox_SetType,
                dataGridView_item,
                button1,
                button2,
                button3,
                button4);
        }


        private void comboBoxDb_DrawItem(object sender, DrawItemEventArgs e)
        {
            configWindowSetupCoordinatorService.DrawComboBoxItem(
                themeComboBoxDrawService,
                comboBoxThemeRendererService,
                database,
                sender,
                e);
        }
    }
}


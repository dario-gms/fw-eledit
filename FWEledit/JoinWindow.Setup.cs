using System.Windows.Forms;

namespace FWEledit
{
    public partial class JoinWindow : Form
    {
        public JoinWindow()
            : this(new SessionService())
        {
        }

        public JoinWindow(ISessionService sessionService)
		{
            this.sessionService = sessionService ?? new SessionService();
			InitializeComponent();
            database = this.sessionService.Database;
            viewModel = new JoinWindowViewModel();
            BindViewModel();
            colorTheme();
        }


        private void BindViewModel()
        {
            textBox_ElementFile.DataBindings.Add("Text", viewModel, nameof(JoinWindowViewModel.ElementFile), false, DataSourceUpdateMode.OnPropertyChanged);
            textBox_LogDir.DataBindings.Add("Text", viewModel, nameof(JoinWindowViewModel.LogDirectory), false, DataSourceUpdateMode.OnPropertyChanged);
            checkBox_AddNew.DataBindings.Add("Checked", viewModel, nameof(JoinWindowViewModel.AddNew), false, DataSourceUpdateMode.OnPropertyChanged);
            checkBox_BackupNew.DataBindings.Add("Checked", viewModel, nameof(JoinWindowViewModel.BackupNew), false, DataSourceUpdateMode.OnPropertyChanged);
            checkBox_ReplaceChanged.DataBindings.Add("Checked", viewModel, nameof(JoinWindowViewModel.ReplaceChanged), false, DataSourceUpdateMode.OnPropertyChanged);
            checkBox_BackupChanged.DataBindings.Add("Checked", viewModel, nameof(JoinWindowViewModel.BackupChanged), false, DataSourceUpdateMode.OnPropertyChanged);
            checkBox_RemoveMissing.DataBindings.Add("Checked", viewModel, nameof(JoinWindowViewModel.RemoveMissing), false, DataSourceUpdateMode.OnPropertyChanged);
            checkBox_BackupMissing.DataBindings.Add("Checked", viewModel, nameof(JoinWindowViewModel.BackupMissing), false, DataSourceUpdateMode.OnPropertyChanged);
        }


        private void colorTheme()
        {
            joinWindowCoordinatorService.ApplyTheme(
                joinWindowThemeUiService,
                database,
                this,
                label1,
                label2,
                label3,
                label4,
                label5,
                textBox_ElementFile,
                textBox_LogDir,
                checkBox_AddNew,
                checkBox_BackupNew,
                checkBox_ReplaceChanged,
                checkBox_BackupChanged,
                checkBox_RemoveMissing,
                checkBox_BackupMissing,
                button_BrowseEL,
                button_BrowseLog,
                button_Cancel,
                button_OK);
        }
    }
}



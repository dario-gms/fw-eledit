using System;
using System.Windows.Forms;

namespace FWEledit
{
	public partial class JoinWindow : Form
	{
        private readonly JoinWindowViewModel viewModel;
        private readonly JoinWindowThemeUiService joinWindowThemeUiService = new JoinWindowThemeUiService();
        private readonly JoinWindowDialogUiService joinWindowDialogUiService = new JoinWindowDialogUiService();
        private readonly DialogService dialogService = new DialogService();
        private readonly GameFolderDialogService folderDialogService = new GameFolderDialogService();
        private readonly JoinWindowCoordinatorService joinWindowCoordinatorService = new JoinWindowCoordinatorService();
		public string FileName
		{
			get
			{
				return viewModel.ElementFile;
			}
			set
			{
                viewModel.ElementFile = value;
			}
		}
		public string LogDirectory
		{
			get
			{
				return viewModel.LogDirectory;
			}
			set
			{
                viewModel.LogDirectory = value;
			}
		}
        public bool AddNew { get { return viewModel.AddNew; } set { viewModel.AddNew = value; } }
        public bool BackupNew { get { return viewModel.BackupNew; } set { viewModel.BackupNew = value; } }
        public bool ReplaceChanged { get { return viewModel.ReplaceChanged; } set { viewModel.ReplaceChanged = value; } }
        public bool BackupChanged { get { return viewModel.BackupChanged; } set { viewModel.BackupChanged = value; } }
        public bool RemoveMissing { get { return viewModel.RemoveMissing; } set { viewModel.RemoveMissing = value; } }
        public bool BackupMissing { get { return viewModel.BackupMissing; } set { viewModel.BackupMissing = value; } }
        private readonly ISessionService sessionService;
        private CacheSave database;

	}
}


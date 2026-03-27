namespace FWEledit
{
    public sealed class JoinWindowViewModel : ViewModelBase
    {
        private string elementFile;
        private string logDirectory;
        private bool addNew;
        private bool backupNew;
        private bool replaceChanged;
        private bool backupChanged;
        private bool removeMissing;
        private bool backupMissing;

        public string ElementFile
        {
            get { return elementFile; }
            set { SetProperty(ref elementFile, value); }
        }

        public string LogDirectory
        {
            get { return logDirectory; }
            set { SetProperty(ref logDirectory, value); }
        }

        public bool AddNew
        {
            get { return addNew; }
            set { SetProperty(ref addNew, value); }
        }

        public bool BackupNew
        {
            get { return backupNew; }
            set { SetProperty(ref backupNew, value); }
        }

        public bool ReplaceChanged
        {
            get { return replaceChanged; }
            set { SetProperty(ref replaceChanged, value); }
        }

        public bool BackupChanged
        {
            get { return backupChanged; }
            set { SetProperty(ref backupChanged, value); }
        }

        public bool RemoveMissing
        {
            get { return removeMissing; }
            set { SetProperty(ref removeMissing, value); }
        }

        public bool BackupMissing
        {
            get { return backupMissing; }
            set { SetProperty(ref backupMissing, value); }
        }
    }
}

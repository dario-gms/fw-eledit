namespace FWEledit
{
    public sealed class ElementsJoinOptions
    {
        public string SourceFilePath { get; set; }
        public string LogDirectory { get; set; }
        public bool AddNew { get; set; }
        public bool ReplaceChanged { get; set; }
        public bool RemoveMissing { get; set; }
        public bool BackupNew { get; set; }
        public bool BackupChanged { get; set; }
        public bool BackupMissing { get; set; }
    }
}

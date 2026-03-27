namespace FWEledit
{
    public sealed class NavigationStateService
    {
        public string GetLastGameFolder()
        {
            return Properties.Settings.Default.LastGameFolder ?? string.Empty;
        }

        public string GetLastRunVersion()
        {
            return Properties.Settings.Default.LastRunVersion ?? string.Empty;
        }

        public bool ResetIfVersionChanged(string displayVersion)
        {
            string lastRun = GetLastRunVersion();
            if (!string.Equals(lastRun, displayVersion, System.StringComparison.OrdinalIgnoreCase))
            {
                Properties.Settings.Default.LastGameFolder = string.Empty;
                ResetOnStartup(displayVersion);
                return true;
            }
            return false;
        }

        public void PersistSelection(bool isRestoring, int listIndex, int? currentItemId)
        {
            if (isRestoring)
            {
                return;
            }
            if (listIndex > -1)
            {
                Properties.Settings.Default.LastListIndex = listIndex;
            }
            if (currentItemId.HasValue && currentItemId.Value > -1)
            {
                Properties.Settings.Default.LastItemId = currentItemId.Value;
            }
        }

        public void ResetOnStartup(string displayVersion)
        {
            Properties.Settings.Default.LastListIndex = 0;
            Properties.Settings.Default.LastItemId = -1;
            Properties.Settings.Default.LastRunVersion = displayVersion;
            Properties.Settings.Default.Save();
        }

        public void Flush()
        {
            Properties.Settings.Default.Save();
        }

        public NavigationSettingsSnapshot LoadSnapshot()
        {
            return new NavigationSettingsSnapshot
            {
                LastListIndex = Properties.Settings.Default.LastListIndex,
                LastItemId = Properties.Settings.Default.LastItemId
            };
        }

        public void SaveGameFolder(string gameFolderPath)
        {
            Properties.Settings.Default.LastGameFolder = gameFolderPath;
            Properties.Settings.Default.Save();
        }
    }
}

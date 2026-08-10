namespace FWEledit
{
    public sealed class NavigationStateService
    {
        private const int MaxRecentGameFolders = 5;
        private static readonly char[] RecentFolderSeparators = new[] { '\n' };

        public string GetLastGameFolder()
        {
            return Properties.Settings.Default.LastGameFolder ?? string.Empty;
        }

        public System.Collections.Generic.List<string> GetRecentGameFolders()
        {
            System.Collections.Generic.List<string> folders = new System.Collections.Generic.List<string>();
            string raw = Properties.Settings.Default.RecentGameFolders ?? string.Empty;
            string[] entries = raw.Split(RecentFolderSeparators, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length && folders.Count < MaxRecentGameFolders; i++)
            {
                string folder = entries[i].Trim();
                if (folder.Length == 0 || folders.Contains(folder))
                {
                    continue;
                }

                if (System.IO.Directory.Exists(folder))
                {
                    folders.Add(folder);
                }
            }

            return folders;
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
                Properties.Settings.Default.RecentGameFolders = string.Empty;
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
            if (string.IsNullOrWhiteSpace(gameFolderPath))
            {
                return;
            }

            Properties.Settings.Default.LastGameFolder = gameFolderPath;
            SaveRecentGameFolder(gameFolderPath);
            Properties.Settings.Default.Save();
        }

        public void ClearRecentGameFolders()
        {
            Properties.Settings.Default.LastGameFolder = string.Empty;
            Properties.Settings.Default.RecentGameFolders = string.Empty;
            Properties.Settings.Default.Save();
        }

        private void SaveRecentGameFolder(string gameFolderPath)
        {
            System.Collections.Generic.List<string> folders = GetRecentGameFolders();
            for (int i = folders.Count - 1; i >= 0; i--)
            {
                if (string.Equals(folders[i], gameFolderPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    folders.RemoveAt(i);
                }
            }

            folders.Insert(0, gameFolderPath);
            while (folders.Count > MaxRecentGameFolders)
            {
                folders.RemoveAt(folders.Count - 1);
            }

            Properties.Settings.Default.RecentGameFolders = string.Join("\n", folders.ToArray());
        }
    }
}

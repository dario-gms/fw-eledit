using System;
using System.IO;

namespace FWEledit
{
    public sealed class StartupSessionService
    {
        public void TryRestoreLastSession(NavigationStateService navigationStateService, Action<string> loadFolder)
        {
            if (navigationStateService == null || loadFolder == null)
            {
                return;
            }

            try
            {
                string savedFolder = navigationStateService.GetLastGameFolder();
                if (!string.IsNullOrWhiteSpace(savedFolder) && Directory.Exists(savedFolder))
                {
                    loadFolder(savedFolder);
                }
            }
            catch
            {
            }
        }
    }
}

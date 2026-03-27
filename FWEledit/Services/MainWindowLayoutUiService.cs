using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowLayoutUiService
    {
        public void EnsureMainSplitSizing(
            SplitContainer mainSplit,
            SplitContainerSizingService sizingService)
        {
            if (sizingService == null)
            {
                return;
            }

            sizingService.EnsureSizing(
                mainSplit,
                260,
                420,
                280,
                380,
                0.33);
        }

        public void HandleShown(
            SplitContainer mainSplit,
            SplitContainerSizingService sizingService,
            StartupSessionService startupSessionService,
            NavigationStateService navigationStateService,
            Action<string> loadGameFolder,
            ref bool startupSessionRestoreDone)
        {
            EnsureMainSplitSizing(mainSplit, sizingService);
            if (startupSessionRestoreDone)
            {
                return;
            }

            startupSessionRestoreDone = true;
            if (startupSessionService != null)
            {
                startupSessionService.TryRestoreLastSession(navigationStateService, loadGameFolder);
            }
        }

        public void HandleResize(
            SplitContainer mainSplit,
            SplitContainerSizingService sizingService)
        {
            EnsureMainSplitSizing(mainSplit, sizingService);
        }
    }
}

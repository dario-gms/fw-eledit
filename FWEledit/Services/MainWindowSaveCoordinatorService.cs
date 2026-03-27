using System;

namespace FWEledit
{
    public sealed class MainWindowSaveCoordinatorService
    {
        public bool SaveSession(
            MainWindowSaveUiService mainWindowSaveUiService,
            SaveSessionUiService saveSessionUiService,
            ElementsSessionService elementsSessionService,
            SaveContextBuilderService contextBuilderService,
            ISessionService sessionService,
            string elementsPathOverride,
            SaveProgressUiService progressUiService,
            SaveProgressService progressService,
            ColorProgressBar.ColorProgressBar progressBar,
            Func<bool> validateUniqueIds,
            Func<bool> flushPendingDescriptions,
            Action clearDirtyTracking,
            Action<string> showInfoMessage,
            Action<string> showErrorMessage,
            Action<string, Exception> logError,
            Func<NavigationSnapshot> captureSnapshot,
            Action<NavigationSnapshot> restoreSnapshot,
            Func<string> promptSavePath,
            Action<string> updateElementsPath,
            Action<string> updateStatus,
            Action<string> showConfirmation)
        {
            if (mainWindowSaveUiService == null || sessionService == null)
            {
                return false;
            }

            return mainWindowSaveUiService.SaveSession(
                saveSessionUiService,
                elementsSessionService,
                contextBuilderService,
                sessionService.ListCollection,
                sessionService.ConversationList,
                elementsPathOverride,
                sessionService.AssetManager,
                progressUiService,
                progressService,
                progressBar,
                validateUniqueIds,
                flushPendingDescriptions,
                clearDirtyTracking,
                showInfoMessage,
                showErrorMessage,
                logError,
                captureSnapshot,
                restoreSnapshot,
                promptSavePath,
                updateElementsPath,
                updateStatus,
                showConfirmation);
        }
    }
}

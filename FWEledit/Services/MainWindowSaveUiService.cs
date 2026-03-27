using System;

namespace FWEledit
{
    public sealed class MainWindowSaveUiService
    {
        public void BeginSaveProgress(
            SaveProgressUiService progressUiService,
            SaveProgressService progressService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressUiService == null)
            {
                return;
            }

            progressUiService.Begin(progressService, progressBar);
        }

        public void SetSaveProgress(
            SaveProgressUiService progressUiService,
            SaveProgressService progressService,
            ColorProgressBar.ColorProgressBar progressBar,
            int value)
        {
            if (progressUiService == null)
            {
                return;
            }

            progressUiService.Set(progressService, progressBar, value);
        }

        public void EndSaveProgress(
            SaveProgressUiService progressUiService,
            SaveProgressService progressService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (progressUiService == null)
            {
                return;
            }

            progressUiService.End(progressService, progressBar);
        }

        public void ShowSaveConfirmation(
            SaveConfirmationUiService confirmationUiService,
            SaveConfirmationService confirmationService,
            string details)
        {
            if (confirmationUiService == null)
            {
                return;
            }

            confirmationUiService.Show(confirmationService, details);
        }

        public bool ValidateUniqueIdsBeforeSave(
            UniqueIdValidationService uniqueIdValidationService,
            eListCollection listCollection,
            Func<int, int> getIdFieldIndex,
            ElementsValidationService elementsValidationService,
            Action<string> showMessage)
        {
            if (uniqueIdValidationService == null)
            {
                return true;
            }

            return uniqueIdValidationService.Validate(
                listCollection,
                getIdFieldIndex,
                elementsValidationService,
                showMessage);
        }

        public bool SaveSession(
            SaveSessionUiService saveSessionUiService,
            ElementsSessionService elementsSessionService,
            SaveContextBuilderService contextBuilderService,
            eListCollection listCollection,
            eListConversation conversationList,
            string elementsPathOverride,
            AssetManager assetManager,
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
            if (saveSessionUiService == null || contextBuilderService == null)
            {
                return false;
            }

            return saveSessionUiService.SaveSession(
                elementsSessionService,
                handler => contextBuilderService.Build(
                    listCollection,
                    conversationList,
                    elementsPathOverride,
                    assetManager,
                    () => BeginSaveProgress(progressUiService, progressService, progressBar),
                    value => SetSaveProgress(progressUiService, progressService, progressBar, value),
                    () => EndSaveProgress(progressUiService, progressService, progressBar),
                    validateUniqueIds,
                    flushPendingDescriptions,
                    clearDirtyTracking,
                    showInfoMessage,
                    showErrorMessage,
                    logError,
                    captureSnapshot,
                    restoreSnapshot,
                    promptSavePath,
                    handler),
                updateElementsPath,
                updateStatus,
                showConfirmation);
        }

        public bool SaveCurrentSessionNoDialog(
            eListCollection listCollection,
            SaveSessionUiService saveSessionUiService,
            ElementsSessionService elementsSessionService,
            SaveContextBuilderService contextBuilderService,
            eListConversation conversationList,
            string elementsPath,
            AssetManager assetManager,
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
            Action<string> updateStatus)
        {
            if (listCollection == null)
            {
                return true;
            }

            return SaveSession(
                saveSessionUiService,
                elementsSessionService,
                contextBuilderService,
                listCollection,
                conversationList,
                elementsPath,
                assetManager,
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
                null);
        }
    }
}

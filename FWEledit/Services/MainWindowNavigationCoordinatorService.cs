using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowNavigationCoordinatorService
    {
        public void BeginSaveProgress(
            MainWindowSaveUiService mainWindowSaveUiService,
            SaveProgressUiService saveProgressUiService,
            SaveProgressService saveProgressService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (mainWindowSaveUiService == null)
            {
                return;
            }
            mainWindowSaveUiService.BeginSaveProgress(saveProgressUiService, saveProgressService, progressBar);
        }

        public void SetSaveProgress(
            MainWindowSaveUiService mainWindowSaveUiService,
            SaveProgressUiService saveProgressUiService,
            SaveProgressService saveProgressService,
            ColorProgressBar.ColorProgressBar progressBar,
            int value)
        {
            if (mainWindowSaveUiService == null)
            {
                return;
            }
            mainWindowSaveUiService.SetSaveProgress(saveProgressUiService, saveProgressService, progressBar, value);
        }

        public void EndSaveProgress(
            MainWindowSaveUiService mainWindowSaveUiService,
            SaveProgressUiService saveProgressUiService,
            SaveProgressService saveProgressService,
            ColorProgressBar.ColorProgressBar progressBar)
        {
            if (mainWindowSaveUiService == null)
            {
                return;
            }
            mainWindowSaveUiService.EndSaveProgress(saveProgressUiService, saveProgressService, progressBar);
        }

        public void ShowSaveConfirmation(
            MainWindowSaveUiService mainWindowSaveUiService,
            SaveConfirmationUiService saveConfirmationUiService,
            SaveConfirmationService saveConfirmationService,
            string details)
        {
            if (mainWindowSaveUiService == null)
            {
                return;
            }
            mainWindowSaveUiService.ShowSaveConfirmation(saveConfirmationUiService, saveConfirmationService, details);
        }

        public NavigationSnapshot CaptureNavigationSnapshot(
            MainWindowNavigationUiService mainWindowNavigationUiService,
            NavigationSnapshotUiService navigationSnapshotUiService,
            NavigationSnapshotService navigationSnapshotService,
            ComboBox listCombo,
            DataGridView elementsGrid)
        {
            if (mainWindowNavigationUiService == null)
            {
                return null;
            }
            return mainWindowNavigationUiService.CaptureSnapshot(
                navigationSnapshotUiService,
                navigationSnapshotService,
                listCombo,
                elementsGrid);
        }

        public void RestoreNavigationSnapshot(
            MainWindowNavigationUiService mainWindowNavigationUiService,
            NavigationSnapshotUiService navigationSnapshotUiService,
            NavigationSnapshotService navigationSnapshotService,
            NavigationSnapshot snapshot,
            ComboBox listCombo,
            DataGridView elementsGrid,
            Func<bool> getIsRestoring,
            Action<bool> setIsRestoring)
        {
            if (mainWindowNavigationUiService == null)
            {
                return;
            }
            mainWindowNavigationUiService.RestoreSnapshot(
                navigationSnapshotUiService,
                navigationSnapshotService,
                snapshot,
                listCombo,
                elementsGrid,
                getIsRestoring,
                setIsRestoring);
        }

        public void ClearDirtyTrackingAfterSave(
            MainWindowDirtyTrackingService mainWindowDirtyTrackingService,
            DirtyTrackingUiService dirtyTrackingUiService,
            DirtyStateTracker dirtyStateTracker,
            DescriptionViewModel descriptionViewModel,
            ListDisplayService listDisplayService,
            int selectedListIndex,
            Action refreshList,
            ref bool hasUnsavedChanges)
        {
            if (mainWindowDirtyTrackingService == null)
            {
                return;
            }
            mainWindowDirtyTrackingService.ClearAfterSave(
                dirtyTrackingUiService,
                dirtyStateTracker,
                descriptionViewModel,
                listDisplayService,
                selectedListIndex,
                refreshList,
                ref hasUnsavedChanges);
        }

        public void PersistNavigationState(
            MainWindowNavigationUiService mainWindowNavigationUiService,
            NavigationPersistenceUiService navigationPersistenceUiService,
            NavigationPersistenceService navigationPersistenceService,
            SelectionStateService selectionStateService,
            MainWindowViewModel viewModel,
            ComboBox listCombo,
            DataGridView elementsGrid,
            NavigationStateService navigationStateService,
            Action restartTimer,
            Action<bool> setPendingWrite,
            Func<bool> isRestoring)
        {
            if (mainWindowNavigationUiService == null)
            {
                return;
            }
            mainWindowNavigationUiService.PersistNavigationState(
                navigationPersistenceUiService,
                navigationPersistenceService,
                selectionStateService,
                viewModel,
                listCombo,
                elementsGrid,
                navigationStateService,
                restartTimer,
                setPendingWrite,
                isRestoring);
        }

        public void FlushNavigationStateToDisk(
            MainWindowNavigationUiService mainWindowNavigationUiService,
            NavigationPersistenceUiService navigationPersistenceUiService,
            NavigationPersistenceService navigationPersistenceService,
            MainWindowViewModel viewModel,
            NavigationStateService navigationStateService,
            Action<bool> setPendingWrite)
        {
            if (mainWindowNavigationUiService == null)
            {
                return;
            }
            mainWindowNavigationUiService.FlushNavigationState(
                navigationPersistenceUiService,
                navigationPersistenceService,
                viewModel,
                navigationStateService,
                setPendingWrite);
        }

        public bool SaveCurrentSessionNoDialog(
            MainWindowSaveUiService mainWindowSaveUiService,
            eListCollection listCollection,
            SaveSessionUiService saveSessionUiService,
            ElementsSessionService elementsSessionService,
            SaveContextBuilderService saveContextBuilderService,
            eListConversation conversationList,
            string elementsPath,
            AssetManager assetManager,
            SaveProgressUiService saveProgressUiService,
            SaveProgressService saveProgressService,
            ColorProgressBar.ColorProgressBar progressBar,
            Func<bool> validateUniqueIdsBeforeSave,
            Func<bool> flushPendingDescriptions,
            Action clearDirtyTrackingAfterSave,
            Action<string> showMessage,
            Action<string> showWarning,
            Action<string, Exception> logError,
            Func<NavigationSnapshot> captureNavigationSnapshot,
            Action<NavigationSnapshot> restoreNavigationSnapshot,
            Func<string> promptSavePath,
            Action<string> updateElementsPath,
            Action<string> updateSummary)
        {
            if (mainWindowSaveUiService == null)
            {
                return false;
            }
            return mainWindowSaveUiService.SaveCurrentSessionNoDialog(
                listCollection,
                saveSessionUiService,
                elementsSessionService,
                saveContextBuilderService,
                conversationList,
                elementsPath,
                assetManager,
                saveProgressUiService,
                saveProgressService,
                progressBar,
                validateUniqueIdsBeforeSave,
                flushPendingDescriptions,
                clearDirtyTrackingAfterSave,
                showMessage,
                showWarning,
                logError,
                captureNavigationSnapshot,
                restoreNavigationSnapshot,
                promptSavePath,
                updateElementsPath,
                updateSummary);
        }

        public void HandleFormClosing(
            ClosePromptUiService closePromptUiService,
            bool suppressClosePrompt,
            bool hasUnsavedChanges,
            DescriptionViewModel descriptionViewModel,
            ClosePromptService closePromptService,
            Func<bool> saveCurrentSession,
            Action persistNavigationState,
            Action flushNavigationState,
            FormClosingEventArgs e)
        {
            if (closePromptUiService == null)
            {
                return;
            }
            closePromptUiService.HandleClosing(
                suppressClosePrompt,
                hasUnsavedChanges,
                descriptionViewModel,
                closePromptService,
                saveCurrentSession,
                persistNavigationState,
                flushNavigationState,
                e);
        }

        public void LogError(ErrorLoggingService errorLoggingService, string context, Exception ex)
        {
            if (errorLoggingService == null)
            {
                return;
            }
            errorLoggingService.Log(context, ex);
        }

        public bool ValidateUniqueIdsBeforeSave(
            MainWindowSaveUiService mainWindowSaveUiService,
            UniqueIdValidationService uniqueIdValidationService,
            eListCollection listCollection,
            Func<int, int> getIdFieldIndex,
            ElementsValidationService elementsValidationService,
            Action<string> showMessage)
        {
            if (mainWindowSaveUiService == null)
            {
                return false;
            }
            return mainWindowSaveUiService.ValidateUniqueIdsBeforeSave(
                uniqueIdValidationService,
                listCollection,
                getIdFieldIndex,
                elementsValidationService,
                showMessage);
        }
    }
}

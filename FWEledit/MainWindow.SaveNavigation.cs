using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void BeginSaveProgress()
        {
            mainWindowNavigationCoordinatorService.BeginSaveProgress(
                mainWindowSaveUiService,
                saveProgressUiService,
                saveProgressService,
                cpb2);
        }

        private void SetSaveProgress(int value)
        {
            mainWindowNavigationCoordinatorService.SetSaveProgress(
                mainWindowSaveUiService,
                saveProgressUiService,
                saveProgressService,
                cpb2,
                value);
        }

        private void EndSaveProgress()
        {
            mainWindowNavigationCoordinatorService.EndSaveProgress(
                mainWindowSaveUiService,
                saveProgressUiService,
                saveProgressService,
                cpb2);
        }

        private void ShowSaveConfirmation(string details)
        {
            mainWindowNavigationCoordinatorService.ShowSaveConfirmation(
                mainWindowSaveUiService,
                saveConfirmationUiService,
                saveConfirmationService,
                details);
        }

        private NavigationSnapshot CaptureNavigationSnapshot()
        {
            return mainWindowNavigationCoordinatorService.CaptureNavigationSnapshot(
                mainWindowNavigationUiService,
                navigationSnapshotUiService,
                navigationSnapshotService,
                comboBox_lists,
                dataGridView_elems);
        }

        private void RestoreNavigationSnapshot(NavigationSnapshot snapshot)
        {
            mainWindowNavigationCoordinatorService.RestoreNavigationSnapshot(
                mainWindowNavigationUiService,
                navigationSnapshotUiService,
                navigationSnapshotService,
                snapshot,
                comboBox_lists,
                dataGridView_elems,
                () => viewModel.IsRestoringSessionState,
                value => viewModel.IsRestoringSessionState = value);
        }

        private void ClearDirtyTrackingAfterSave()
        {
            mainWindowNavigationCoordinatorService.ClearDirtyTrackingAfterSave(
                mainWindowDirtyTrackingService,
                dirtyTrackingUiService,
                dirtyStateTracker,
                viewModel.DescriptionViewModel,
                listDisplayService,
                comboBox_lists.SelectedIndex,
                () => change_list(null, null),
                ref viewModel.HasUnsavedChanges);
        }

        private void PersistNavigationState()
        {
            mainWindowNavigationCoordinatorService.PersistNavigationState(
                mainWindowNavigationUiService,
                navigationPersistenceUiService,
                navigationPersistenceService,
                selectionStateService,
                viewModel,
                comboBox_lists,
                dataGridView_elems,
                navigationStateService,
                () => navigationPersistenceService.RestartTimer(navigationPersistTimer),
                value => viewModel.HasPendingNavigationStateWrite = value,
                () => viewModel.IsRestoringSessionState);
        }

        private void FlushNavigationStateToDisk()
        {
            mainWindowNavigationCoordinatorService.FlushNavigationStateToDisk(
                mainWindowNavigationUiService,
                navigationPersistenceUiService,
                navigationPersistenceService,
                viewModel,
                navigationStateService,
                value => viewModel.HasPendingNavigationStateWrite = value);
        }

        private bool SaveCurrentSessionNoDialog()
        {
            return mainWindowNavigationCoordinatorService.SaveCurrentSessionNoDialog(
                mainWindowSaveUiService,
                sessionService.ListCollection,
                saveSessionUiService,
                elementsSessionService,
                saveContextBuilderService,
                sessionService.ConversationList,
                viewModel.ElementsPath,
                sessionService.AssetManager,
                saveProgressUiService,
                saveProgressService,
                cpb2,
                ValidateUniqueIdsBeforeSave,
                FlushPendingDescriptionsToDisk,
                ClearDirtyTrackingAfterSave,
                message => MessageBox.Show(message),
                message => MessageBox.Show(message),
                LogError,
                CaptureNavigationSnapshot,
                RestoreNavigationSnapshot,
                () => savePathService.PromptElementsSavePath(Environment.CurrentDirectory, this),
                path => { viewModel.ElementsPath = path ?? viewModel.ElementsPath; },
                summary =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = summary;
                    }
                });
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            mainWindowNavigationCoordinatorService.HandleFormClosing(
                closePromptUiService,
                viewModel.SuppressClosePrompt,
                viewModel.HasUnsavedChanges,
                viewModel.DescriptionViewModel,
                closePromptService,
                SaveCurrentSessionNoDialog,
                PersistNavigationState,
                FlushNavigationStateToDisk,
                e);
        }

        private void LogError(string context, Exception ex)
        {
            mainWindowNavigationCoordinatorService.LogError(errorLoggingService, context, ex);
        }

        private bool ValidateUniqueIdsBeforeSave()
        {
            return mainWindowNavigationCoordinatorService.ValidateUniqueIdsBeforeSave(
                mainWindowSaveUiService,
                uniqueIdValidationService,
                sessionService.ListCollection,
                listIndex => idGenerationService.GetIdFieldIndex(sessionService.ListCollection, listIndex),
                elementsValidationService,
                message => MessageBox.Show(message));
        }

    }
}



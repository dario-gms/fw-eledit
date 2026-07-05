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
                dataGridView_elems,
                dataGridView_item);
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
                dataGridView_item,
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
                RefreshCurrentListAfterSave,
                ref viewModel.HasUnsavedChanges);
        }

        private void RefreshCurrentListAfterSave()
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_elems == null
                || listDisplayService == null)
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0 || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return;
            }

            int nameFieldIndex = fieldIndexLookupService.GetNameFieldIndex(sessionService.ListCollection, listIndex);

            dataGridView_elems.SuspendLayout();
            try
            {
                for (int gridRow = 0; gridRow < dataGridView_elems.Rows.Count; gridRow++)
                {
                    int elementIndex = elementIndexResolverService != null
                        ? elementIndexResolverService.ResolveElementIndexFromGridRow(
                            sessionService.ListCollection,
                            listIndex,
                            gridRow,
                            dataGridView_elems)
                        : gridRow;
                    if (elementIndex < 0
                        || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
                    {
                        continue;
                    }

                    dataGridView_elems.Rows[gridRow].Cells[2].Value = listDisplayService.ComposeListDisplayName(
                        sessionService,
                        sessionService.ListCollection,
                        listIndex,
                        elementIndex,
                        nameFieldIndex,
                        false);
                }
            }
            finally
            {
                dataGridView_elems.ResumeLayout();
            }

            dataGridView_elems.Refresh();

            RefreshCurrentItemGridAfterSave();
            UpdateDescriptionTabForSelection();
            UpdatePickIconButtonState();
            UpdateRawValueEditorFromCurrentCell();
            UpdateNpcSellServiceUiForSelection();
        }

        private void RefreshCurrentItemGridAfterSave()
        {
            if (dataGridView_item == null || dataGridView_item.Rows.Count == 0)
            {
                return;
            }

            System.Drawing.Color defaultForeColor = dataGridView_item.DefaultCellStyle.ForeColor;
            System.Drawing.Color defaultSelectionForeColor = dataGridView_item.DefaultCellStyle.SelectionForeColor;

            dataGridView_item.SuspendLayout();
            try
            {
                for (int rowIndex = 0; rowIndex < dataGridView_item.Rows.Count; rowIndex++)
                {
                    DataGridViewRow row = dataGridView_item.Rows[rowIndex];
                    if (row == null || row.Cells.Count < 3)
                    {
                        continue;
                    }

                    DataGridViewCell valueCell = row.Cells[2];
                    valueCell.Style.ForeColor = defaultForeColor;
                    valueCell.Style.SelectionForeColor = defaultSelectionForeColor;
                }
            }
            finally
            {
                dataGridView_item.ResumeLayout();
            }

            dataGridView_item.Refresh();
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



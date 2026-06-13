using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowNavigationUiService
    {
        public NavigationSnapshot CaptureSnapshot(
            NavigationSnapshotUiService snapshotUiService,
            NavigationSnapshotService snapshotService,
            ComboBox listCombo,
            DataGridView elementGrid,
            DataGridView itemGrid)
        {
            if (snapshotUiService == null)
            {
                return null;
            }

            int listIndex = listCombo != null ? listCombo.SelectedIndex : -1;
            return snapshotUiService.CaptureSnapshot(snapshotService, listIndex, elementGrid, itemGrid);
        }

        public void RestoreSnapshot(
            NavigationSnapshotUiService snapshotUiService,
            NavigationSnapshotService snapshotService,
            NavigationSnapshot snapshot,
            ComboBox listCombo,
            DataGridView elementGrid,
            DataGridView itemGrid,
            Func<bool> getIsRestoring,
            Action<bool> setIsRestoring)
        {
            if (snapshotUiService == null)
            {
                return;
            }

            snapshotUiService.RestoreSnapshot(
                snapshotService,
                snapshot,
                listCombo,
                elementGrid,
                itemGrid,
                getIsRestoring,
                setIsRestoring);
        }

        public void PersistNavigationState(
            NavigationPersistenceUiService persistenceUiService,
            NavigationPersistenceService persistenceService,
            SelectionStateService selectionStateService,
            MainWindowViewModel viewModel,
            ComboBox listCombo,
            DataGridView elementGrid,
            NavigationStateService navigationStateService,
            Action restartTimer,
            Action<bool> setPendingWrite,
            Func<bool> isRestoring)
        {
            if (persistenceUiService == null)
            {
                return;
            }

            persistenceUiService.Persist(
                persistenceService,
                selectionStateService,
                viewModel,
                listCombo,
                elementGrid,
                navigationStateService,
                restartTimer,
                setPendingWrite,
                isRestoring);
        }

        public void FlushNavigationState(
            NavigationPersistenceUiService persistenceUiService,
            NavigationPersistenceService persistenceService,
            MainWindowViewModel viewModel,
            NavigationStateService navigationStateService,
            Action<bool> setPendingWrite)
        {
            if (persistenceUiService == null)
            {
                return;
            }

            persistenceUiService.Flush(
                persistenceService,
                viewModel,
                navigationStateService,
                setPendingWrite);
        }
    }
}

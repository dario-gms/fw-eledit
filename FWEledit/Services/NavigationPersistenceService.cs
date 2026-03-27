using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NavigationPersistenceService
    {
        public void Persist(
            SelectionStateService selectionStateService,
            MainWindowViewModel viewModel,
            ComboBox listComboBox,
            DataGridView elementGrid,
            NavigationStateService navigationStateService,
            Action restartTimer)
        {
            if (selectionStateService != null && viewModel != null)
            {
                SelectionState state = selectionStateService.BuildSelectionState(listComboBox, elementGrid);
                viewModel.UpdateSelectionState(state.ListIndex, state.ItemId);
            }

            if (viewModel != null)
            {
                viewModel.PersistNavigationState(navigationStateService, restartTimer);
            }
        }

        public void Flush(MainWindowViewModel viewModel, NavigationStateService navigationStateService)
        {
            if (viewModel == null)
            {
                return;
            }
            viewModel.FlushNavigationStateToDisk(navigationStateService);
        }

        public void RestartTimer(Timer timer)
        {
            if (timer == null)
            {
                return;
            }
            timer.Stop();
            timer.Start();
        }
    }
}

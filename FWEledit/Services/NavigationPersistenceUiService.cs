namespace FWEledit
{
    public sealed class NavigationPersistenceUiService
    {
        public void Persist(
            NavigationPersistenceService persistenceService,
            SelectionStateService selectionStateService,
            MainWindowViewModel viewModel,
            System.Windows.Forms.ComboBox listComboBox,
            System.Windows.Forms.DataGridView elementGrid,
            NavigationStateService navigationStateService,
            System.Action restartTimer,
            System.Action<bool> setHasPending,
            System.Func<bool> getIsRestoring)
        {
            if (persistenceService == null || viewModel == null || navigationStateService == null)
            {
                return;
            }

            try
            {
                if (getIsRestoring != null && getIsRestoring())
                {
                    return;
                }

                persistenceService.Persist(
                    selectionStateService,
                    viewModel,
                    listComboBox,
                    elementGrid,
                    navigationStateService,
                    () =>
                    {
                        if (restartTimer != null)
                        {
                            restartTimer();
                        }
                    });

                if (setHasPending != null)
                {
                    setHasPending(viewModel.ShouldFlushNavigationState());
                }
            }
            catch
            {
            }
        }

        public void Flush(
            NavigationPersistenceService persistenceService,
            MainWindowViewModel viewModel,
            NavigationStateService navigationStateService,
            System.Action<bool> setHasPending)
        {
            if (persistenceService == null || viewModel == null || navigationStateService == null)
            {
                return;
            }

            try
            {
                persistenceService.Flush(viewModel, navigationStateService);
                if (setHasPending != null)
                {
                    setHasPending(viewModel.ShouldFlushNavigationState());
                }
            }
            catch
            {
            }
        }
    }
}

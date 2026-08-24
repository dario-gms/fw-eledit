using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class NavigationSelectionService
    {
        private const int RestoreSettleDelayMilliseconds = 3500;

        public void RestoreSelection(
            NavigationSettingsSnapshot navSettings,
            ComboBox listComboBox,
            DataGridView elementGrid,
            MainWindowViewModel viewModel,
            Action persistAction)
        {
            if (navSettings == null || listComboBox == null || listComboBox.Items.Count == 0 || viewModel == null)
            {
                return;
            }

            int savedList = navSettings.LastListIndex;
            int savedItemId = navSettings.LastItemId;
            int restoredRow = -1;
            bool keepRestoreGuard = false;

            viewModel.IsRestoringSessionState = true;
            try
            {
                if (savedList < 0 || savedList >= listComboBox.Items.Count)
                {
                    savedList = 0;
                }
                listComboBox.SelectedIndex = savedList;

                if (savedItemId > 0 && listComboBox.SelectedIndex > -1 && elementGrid != null)
                {
                    keepRestoreGuard = TryRestoreGridSelection(elementGrid, savedItemId, out restoredRow);
                }
            }
            finally
            {
                if (!keepRestoreGuard)
                {
                    viewModel.IsRestoringSessionState = false;
                }
            }

            if (keepRestoreGuard && restoredRow >= 0 && elementGrid != null && !elementGrid.IsDisposed)
            {
                Action finishRestore = () =>
                {
                    if (elementGrid.IsDisposed)
                    {
                        viewModel.IsRestoringSessionState = false;
                        return;
                    }

                    TryRestoreGridSelection(elementGrid, savedItemId, out restoredRow);

                    Timer settleTimer = new Timer();
                    settleTimer.Interval = RestoreSettleDelayMilliseconds;
                    settleTimer.Tick += (sender, args) =>
                    {
                        settleTimer.Stop();
                        settleTimer.Dispose();

                        bool restored = TryRestoreGridSelection(elementGrid, savedItemId, out restoredRow);
                        viewModel.IsRestoringSessionState = false;

                        if (restored && persistAction != null)
                        {
                            persistAction();
                        }
                    };
                    settleTimer.Start();
                };

                if (elementGrid.IsHandleCreated)
                {
                    elementGrid.BeginInvoke(finishRestore);
                }
                else
                {
                    finishRestore();
                }
            }
        }

        private static bool TryRestoreGridSelection(DataGridView elementGrid, int savedItemId, out int restoredRow)
        {
            restoredRow = -1;
            if (elementGrid == null || elementGrid.IsDisposed || savedItemId <= 0)
            {
                return false;
            }

            for (int row = 0; row < elementGrid.Rows.Count; row++)
            {
                int rowId;
                if (!int.TryParse(Convert.ToString(elementGrid.Rows[row].Cells[0].Value), out rowId) || rowId != savedItemId)
                {
                    continue;
                }

                elementGrid.ClearSelection();
                elementGrid.CurrentCell = elementGrid[0, row];
                elementGrid.Rows[row].Selected = true;
                try
                {
                    elementGrid.FirstDisplayedScrollingRowIndex = row;
                }
                catch
                {
                }

                restoredRow = row;
                return true;
            }

            return false;
        }
    }
}

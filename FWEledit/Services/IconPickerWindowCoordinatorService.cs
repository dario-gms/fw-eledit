using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class IconPickerWindowCoordinatorService
    {
        public void InitializeEntries(
            IconPickerViewModel viewModel,
            IconPickerWindow.AtlasGrid grid,
            Label statusLabel)
        {
            if (viewModel == null || grid == null || statusLabel == null)
            {
                return;
            }

            if (viewModel.Data.AtlasBitmap == null)
            {
                statusLabel.Text = "0 icons";
            }
            statusLabel.Text = viewModel.AllEntries.Count + " icons";
            grid.SetData(
                viewModel.Data.AtlasBitmap,
                viewModel.Data.IconWidth,
                viewModel.Data.IconHeight,
                viewModel.Data.AtlasCols,
                viewModel.AllEntries);
        }

        public void ApplyFilter(
            IconPickerViewModel viewModel,
            IconPickerWindow.AtlasGrid grid,
            Label statusLabel,
            string searchText)
        {
            if (viewModel == null || grid == null || statusLabel == null)
            {
                return;
            }

            viewModel.ApplyFilter(searchText);
            grid.SetEntries(viewModel.CurrentEntries);
            statusLabel.Text = viewModel.CurrentEntries.Count + " icons";
        }

        public void SelectPending(
            IconPickerViewModel viewModel,
            IconPickerWindow.AtlasGrid grid)
        {
            if (viewModel == null || grid == null)
            {
                return;
            }

            if (viewModel.PendingSelectPathId <= 0)
            {
                return;
            }

            for (int i = 0; i < viewModel.CurrentEntries.Count; i++)
            {
                if (viewModel.CurrentEntries[i].PathId == viewModel.PendingSelectPathId)
                {
                    grid.SelectedIndex = i;
                    grid.EnsureSelectionVisible();
                    break;
                }
            }
        }

        public void UpdateSelectionStatus(
            IconPickerViewModel viewModel,
            IconPickerWindow.AtlasGrid grid,
            Label statusLabel)
        {
            if (viewModel == null || grid == null || statusLabel == null)
            {
                return;
            }

            IconEntryModel entry = grid.SelectedEntry;
            if (entry == null)
            {
                statusLabel.Text = viewModel.AllEntries.Count + " icons";
                return;
            }

            int usedCount;
            viewModel.UsageByPathId.TryGetValue(entry.PathId, out usedCount);
            int keyCount;
            if (viewModel.UsageByIconKey.TryGetValue(entry.Key, out keyCount))
            {
                usedCount = Math.Max(usedCount, keyCount);
            }
            statusLabel.Text = "PathID: " + entry.PathId + " | Icon: " + entry.FileName + " | Used times: " + usedCount;
        }

        public void CommitSelection(
            IconPickerViewModel viewModel,
            IconPickerWindow.AtlasGrid grid,
            Action<int> setSelectedPathId,
            Action<DialogResult> setDialogResult,
            Action closeWindow)
        {
            if (viewModel == null || grid == null)
            {
                return;
            }

            IconEntryModel entry = grid.SelectedEntry;
            if (entry == null)
            {
                return;
            }

            setSelectedPathId?.Invoke(entry.PathId);
            setDialogResult?.Invoke(DialogResult.OK);
            closeWindow?.Invoke();
        }

        public void HandleKeyDown(KeyEventArgs e, Action cancel)
        {
            if (e == null)
            {
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                cancel?.Invoke();
            }
        }

        public string BuildTooltip(IconPickerViewModel viewModel, IconEntryModel entry)
        {
            if (viewModel == null || entry == null)
            {
                return string.Empty;
            }

            int usedCount;
            viewModel.UsageByPathId.TryGetValue(entry.PathId, out usedCount);
            int keyCount;
            if (viewModel.UsageByIconKey.TryGetValue(entry.Key, out keyCount))
            {
                usedCount = Math.Max(usedCount, keyCount);
            }
            return "PathID: " + entry.PathId
                + "\nIcon: " + entry.FileName
                + "\nUsed times: " + usedCount;
        }
    }
}

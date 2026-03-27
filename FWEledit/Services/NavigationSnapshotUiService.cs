namespace FWEledit
{
    public sealed class NavigationSnapshotUiService
    {
        public NavigationSnapshot CaptureSnapshot(
            NavigationSnapshotService snapshotService,
            int listIndex,
            System.Windows.Forms.DataGridView elementGrid)
        {
            if (snapshotService == null)
            {
                return null;
            }

            return snapshotService.CaptureSnapshot(listIndex, elementGrid);
        }

        public void RestoreSnapshot(
            NavigationSnapshotService snapshotService,
            NavigationSnapshot snapshot,
            System.Windows.Forms.ComboBox listComboBox,
            System.Windows.Forms.DataGridView elementGrid,
            System.Func<bool> getIsRestoring,
            System.Action<bool> setIsRestoring)
        {
            if (snapshotService == null)
            {
                return;
            }

            snapshotService.RestoreSnapshot(snapshot, listComboBox, elementGrid, getIsRestoring, setIsRestoring);
        }
    }
}

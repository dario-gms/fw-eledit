namespace FWEledit
{
    public sealed class NavigationSnapshotUiService
    {
        public NavigationSnapshot CaptureSnapshot(
            NavigationSnapshotService snapshotService,
            int listIndex,
            System.Windows.Forms.DataGridView elementGrid)
        {
            return CaptureSnapshot(snapshotService, listIndex, elementGrid, null);
        }

        public NavigationSnapshot CaptureSnapshot(
            NavigationSnapshotService snapshotService,
            int listIndex,
            System.Windows.Forms.DataGridView elementGrid,
            System.Windows.Forms.DataGridView itemGrid)
        {
            if (snapshotService == null)
            {
                return null;
            }

            return snapshotService.CaptureSnapshot(listIndex, elementGrid, itemGrid);
        }

        public void RestoreSnapshot(
            NavigationSnapshotService snapshotService,
            NavigationSnapshot snapshot,
            System.Windows.Forms.ComboBox listComboBox,
            System.Windows.Forms.DataGridView elementGrid,
            System.Func<bool> getIsRestoring,
            System.Action<bool> setIsRestoring)
        {
            RestoreSnapshot(
                snapshotService,
                snapshot,
                listComboBox,
                elementGrid,
                null,
                getIsRestoring,
                setIsRestoring);
        }

        public void RestoreSnapshot(
            NavigationSnapshotService snapshotService,
            NavigationSnapshot snapshot,
            System.Windows.Forms.ComboBox listComboBox,
            System.Windows.Forms.DataGridView elementGrid,
            System.Windows.Forms.DataGridView itemGrid,
            System.Func<bool> getIsRestoring,
            System.Action<bool> setIsRestoring)
        {
            if (snapshotService == null)
            {
                return;
            }

            snapshotService.RestoreSnapshot(snapshot, listComboBox, elementGrid, itemGrid, getIsRestoring, setIsRestoring);
        }
    }
}

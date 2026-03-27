namespace FWEledit
{
    public sealed class DirtyTrackingUiService
    {
        public void ClearAfterSave(
            DirtyStateTracker tracker,
            DescriptionViewModel descriptionViewModel,
            ListDisplayService listDisplayService,
            int selectedListIndex,
            System.Action refreshListAction,
            ref bool hasUnsavedChanges)
        {
            if (tracker == null || descriptionViewModel == null || listDisplayService == null)
            {
                return;
            }

            tracker.Clear();
            descriptionViewModel.ResetPendingChanges();
            hasUnsavedChanges = false;

            if (selectedListIndex > -1)
            {
                listDisplayService.ClearListDisplayCache();
                if (refreshListAction != null)
                {
                    refreshListAction();
                }
            }
        }
    }
}

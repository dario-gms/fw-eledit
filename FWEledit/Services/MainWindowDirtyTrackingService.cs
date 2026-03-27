using System;

namespace FWEledit
{
    public sealed class MainWindowDirtyTrackingService
    {
        public void MarkRowDirty(
            DirtyStateTracker tracker,
            ListDisplayService listDisplayService,
            ref bool hasUnsavedChanges,
            int listIndex,
            int rowIndex)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.MarkRowDirty(listIndex, rowIndex);
            if (listDisplayService != null)
            {
                listDisplayService.InvalidateListDisplayCache(listIndex);
            }
            hasUnsavedChanges = true;
        }

        public bool IsRowDirty(DirtyStateTracker tracker, int listIndex, int rowIndex)
        {
            return tracker != null && tracker.IsRowDirty(listIndex, rowIndex);
        }

        public void MarkFieldDirty(
            DirtyStateTracker tracker,
            ref bool hasUnsavedChanges,
            int listIndex,
            int rowIndex,
            int fieldIndex)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.MarkFieldDirty(listIndex, rowIndex, fieldIndex);
            hasUnsavedChanges = true;
        }

        public bool IsFieldDirty(DirtyStateTracker tracker, int listIndex, int rowIndex, int fieldIndex)
        {
            return tracker != null && tracker.IsFieldDirty(listIndex, rowIndex, fieldIndex);
        }

        public void MarkFieldInvalid(
            DirtyStateTracker tracker,
            int listIndex,
            int rowIndex,
            int fieldIndex)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.MarkFieldInvalid(listIndex, rowIndex, fieldIndex);
        }

        public void ClearFieldInvalid(
            DirtyStateTracker tracker,
            int listIndex,
            int rowIndex,
            int fieldIndex)
        {
            if (tracker == null)
            {
                return;
            }

            tracker.ClearFieldInvalid(listIndex, rowIndex, fieldIndex);
        }

        public bool IsFieldInvalid(DirtyStateTracker tracker, int listIndex, int rowIndex, int fieldIndex)
        {
            return tracker != null && tracker.IsFieldInvalid(listIndex, rowIndex, fieldIndex);
        }

        public void ClearAfterSave(
            DirtyTrackingUiService dirtyTrackingUiService,
            DirtyStateTracker tracker,
            DescriptionViewModel descriptionViewModel,
            ListDisplayService listDisplayService,
            int listIndex,
            Action refreshList,
            ref bool hasUnsavedChanges)
        {
            if (dirtyTrackingUiService == null)
            {
                return;
            }

            dirtyTrackingUiService.ClearAfterSave(
                tracker,
                descriptionViewModel,
                listDisplayService,
                listIndex,
                refreshList,
                ref hasUnsavedChanges);
        }
    }
}

namespace FWEledit
{
    public sealed class OffsetChangeUiService
    {
        public bool ApplyOffset(
            OffsetUpdateService offsetUpdateService,
            eListCollection listCollection,
            int listIndex,
            string offsetText)
        {
            if (offsetUpdateService == null)
            {
                return false;
            }

            return offsetUpdateService.UpdateOffset(listCollection, listIndex, offsetText);
        }
    }
}

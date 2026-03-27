namespace FWEledit
{
    public sealed class OffsetUpdateService
    {
        public bool UpdateOffset(eListCollection listCollection, int listIndex, string offsetText)
        {
            if (listCollection == null || listIndex < 0)
            {
                return false;
            }

            listCollection.SetOffset(listIndex, offsetText);
            return true;
        }
    }
}

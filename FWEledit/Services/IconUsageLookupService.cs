using System.Collections.Generic;

namespace FWEledit
{
    public sealed class IconUsageLookupService
    {
        public Dictionary<int, int> BuildIconUsageLookup(eListCollection listCollection, int listIndex, int iconFieldRowIndex)
        {
            Dictionary<int, int> usage = new Dictionary<int, int>();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return usage;
            }
            if (iconFieldRowIndex < 0 || iconFieldRowIndex >= listCollection.Lists[listIndex].elementFields.Length)
            {
                return usage;
            }

            for (int row = 0; row < listCollection.Lists[listIndex].elementValues.Length; row++)
            {
                int pathId;
                if (!int.TryParse(listCollection.GetValue(listIndex, row, iconFieldRowIndex), out pathId))
                {
                    continue;
                }
                if (pathId <= 0)
                {
                    continue;
                }

                int count;
                usage.TryGetValue(pathId, out count);
                usage[pathId] = count + 1;
            }

            return usage;
        }
    }
}

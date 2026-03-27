using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ListDisplayRowsService
    {
        public List<object[]> BuildRows(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            ListRowBuilderService rowBuilderService,
            ListDisplayService listDisplayService,
            Func<int, int, int, string> composeDisplayName)
        {
            if (rowBuilderService == null || listDisplayService == null)
            {
                return new List<object[]>();
            }

            return rowBuilderService.BuildRows(
                listCollection,
                conversationList,
                database,
                listIndex,
                composeDisplayName);
        }

        public void WarmupAll(
            eListCollection listCollection,
            ListDisplayService listDisplayService,
            Func<int, List<object[]>> buildRows)
        {
            if (listCollection == null || listDisplayService == null || buildRows == null)
            {
                return;
            }

            listDisplayService.ClearListDisplayCache();
            for (int l = 0; l < listCollection.Lists.Length; l++)
            {
                try
                {
                    listDisplayService.SetListDisplayRows(l, buildRows(l));
                }
                catch
                {
                    listDisplayService.SetListDisplayRows(l, new List<object[]>());
                }
            }
        }
    }
}

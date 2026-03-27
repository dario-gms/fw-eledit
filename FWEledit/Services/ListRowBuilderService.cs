using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class ListRowBuilderService
    {
        private readonly IconResolutionService iconResolutionService;

        public ListRowBuilderService(IconResolutionService iconResolutionService)
        {
            this.iconResolutionService = iconResolutionService;
        }

        public List<object[]> BuildRows(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            System.Func<int, int, int, string> composeDisplayName)
        {
            List<object[]> rows = new List<object[]>();
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return rows;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                if (conversationList != null)
                {
                    for (int e = 0; e < conversationList.talk_proc_count; e++)
                    {
                        rows.Add(new object[] { conversationList.talk_procs[e].id_talk, Properties.Resources.blank, conversationList.talk_procs[e].id_talk + " - Dialog" });
                    }
                }
                else
                {
                    rows.Add(new object[] { 0, Properties.Resources.blank, "Conversation parser unavailable for this data format" });
                }
                return rows;
            }

            int pos = -1;
            int pos2 = -1;
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "Name", System.StringComparison.OrdinalIgnoreCase))
                {
                    pos = i;
                }
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "file_icon", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[i], "file_icon1", System.StringComparison.OrdinalIgnoreCase))
                {
                    pos2 = i;
                }
                if (pos != -1 && pos2 != -1)
                {
                    break;
                }
            }
            if (pos < 0)
            {
                pos = 0;
            }

            for (int e = 0; e < listCollection.Lists[listIndex].elementValues.Length; e++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[0], "ID", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[0], "id", System.StringComparison.OrdinalIgnoreCase))
                {
                    Bitmap img = Properties.Resources.NoIcon;
                    if (pos2 > -1)
                    {
                        string path = iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, listCollection.GetValue(listIndex, e, pos2));
                        if (database != null && database.sourceBitmap != null && database.ContainsKey(path))
                        {
                            img = database.images(path);
                        }
                    }
                    rows.Add(new object[] { listCollection.GetValue(listIndex, e, 0), img, composeDisplayName(listIndex, e, pos) });
                }
                else
                {
                    rows.Add(new object[] { 0, Properties.Resources.NoIcon, composeDisplayName(listIndex, e, pos) });
                }
            }

            return rows;
        }
    }
}

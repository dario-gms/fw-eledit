using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class ListRowBuilderService
    {
        private struct ItemIconSource
        {
            public int ListIndex;
            public int ElementIndex;
            public int IconFieldIndex;
        }

        private readonly IconResolutionService iconResolutionService;
        private readonly CreaturePortraitIconService creaturePortraitIconService;
        private readonly NpcTradePortraitService npcTradePortraitService;
        private readonly NpcSellPortraitService npcSellPortraitService;
        private readonly MonsterDropPortraitService monsterDropPortraitService;
        private eListCollection cachedItemIconSourceCollection;
        private Dictionary<int, ItemIconSource> cachedItemIconSourcesById;

        public ListRowBuilderService(IconResolutionService iconResolutionService)
        {
            this.iconResolutionService = iconResolutionService;
            this.creaturePortraitIconService = new CreaturePortraitIconService();
            this.npcTradePortraitService = new NpcTradePortraitService();
            this.npcSellPortraitService = new NpcSellPortraitService();
            this.monsterDropPortraitService = new MonsterDropPortraitService();
        }

        public System.Func<int, int, int> ReferenceCountResolver { get; set; }

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
                    rows.Add(new object[] { conversationList.talk_procs[e].id_talk, Properties.Resources.blank, conversationList.talk_procs[e].id_talk + " - Dialog", string.Empty });
                    }
                }
                else
                {
                    rows.Add(new object[] { 0, Properties.Resources.blank, "Conversation parser unavailable for this data format", string.Empty });
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

            string normalizedListName = NormalizeListName(listCollection.Lists[listIndex].listName);
            bool isDropTableList = string.Equals(normalizedListName, "DROPTABLE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            bool isItemTradeList = string.Equals(normalizedListName, "ITEM_TRADE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            bool isItemTradePageList = string.Equals(normalizedListName, "ITEM_TRADE_PAGE_CONFIG", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcSellServiceList = string.Equals(normalizedListName, "NPC_SELL_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            int isCategoryFieldIndex = isDropTableList ? GetFieldIndex(listCollection.Lists[listIndex].elementFields, "is_category") : -1;
            List<int> dropFieldIndexes = isDropTableList ? GetDropFieldIndexes(listCollection.Lists[listIndex].elementFields) : null;
            List<int> tradePageGoodsFieldIndexes = isItemTradePageList ? GetTradePageGoodsFieldIndexes(listCollection.Lists[listIndex].elementFields) : null;
            Dictionary<int, int> dropTableRowById = isDropTableList ? BuildDropTableRowIndexMap(listCollection, listIndex) : null;
            bool requiresInheritedItemIcons = isDropTableList || isItemTradePageList;
            Dictionary<int, ItemIconSource> itemIconSourcesById = requiresInheritedItemIcons ? BuildItemIconSourceMap(listCollection) : null;
            Dictionary<int, Bitmap> itemIconById = requiresInheritedItemIcons ? new Dictionary<int, Bitmap>() : null;

            for (int e = 0; e < listCollection.Lists[listIndex].elementValues.Length; e++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[0], "ID", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[0], "id", System.StringComparison.OrdinalIgnoreCase))
                {
                    Bitmap img = ResolveRowIcon(listCollection, database, listIndex, e, pos2);
                    if (isDropTableList)
                    {
                        int dropTableId;
                        Bitmap monsterPortrait = null;
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out dropTableId))
                        {
                            monsterDropPortraitService.TryResolveDropPortrait(listCollection, database, dropTableId, out monsterPortrait);
                        }

                        if (monsterPortrait != null)
                        {
                            img = monsterPortrait;
                        }
                        else
                        {
                            Bitmap dropTableIcon = ResolveDropTableIcon(
                                listCollection,
                                database,
                                listIndex,
                                e,
                                isCategoryFieldIndex,
                                dropFieldIndexes,
                                dropTableRowById,
                                itemIconSourcesById,
                                itemIconById,
                                0);
                            if (dropTableIcon != null)
                            {
                                img = dropTableIcon;
                            }
                        }
                    }
                    else if (isItemTradeList)
                    {
                        int tradeServiceId;
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out tradeServiceId))
                        {
                            Bitmap tradeIcon;
                            if (npcTradePortraitService.TryResolveTradePortrait(listCollection, database, tradeServiceId, out tradeIcon) && tradeIcon != null)
                            {
                                img = tradeIcon;
                            }
                        }
                    }
                    else if (isItemTradePageList)
                    {
                        Bitmap tradePageIcon = ResolveTradePageIcon(
                            listCollection,
                            database,
                            listIndex,
                            e,
                            tradePageGoodsFieldIndexes,
                            itemIconSourcesById,
                            itemIconById);
                        if (tradePageIcon != null)
                        {
                            img = tradePageIcon;
                        }
                    }
                    else if (isNpcSellServiceList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int sellServiceId))
                        {
                            if (npcSellPortraitService.TryResolveSellPortrait(listCollection, database, sellServiceId, out Bitmap sellIcon)
                                && sellIcon != null)
                            {
                                img = sellIcon;
                            }
                        }
                    }
                    rows.Add(new object[] { listCollection.GetValue(listIndex, e, 0), img, composeDisplayName(listIndex, e, pos), string.Empty });
                }
                else
                {
                    rows.Add(new object[] { 0, Properties.Resources.NoIcon, composeDisplayName(listIndex, e, pos), string.Empty });
                }
            }

            return rows;
        }

        private Bitmap ResolveDropTableIcon(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int elementIndex,
            int isCategoryFieldIndex,
            List<int> dropFieldIndexes,
            Dictionary<int, int> dropTableRowById,
            Dictionary<int, ItemIconSource> itemIconSourcesById,
            Dictionary<int, Bitmap> itemIconById,
            int depth)
        {
            if (listCollection == null
                || dropFieldIndexes == null
                || dropFieldIndexes.Count == 0
                || depth > 6)
            {
                return null;
            }

            int firstDropId = GetFirstNonZeroDropId(listCollection, listIndex, elementIndex, dropFieldIndexes);
            if (firstDropId <= 0)
            {
                return null;
            }

            int isCategory = 0;
            if (isCategoryFieldIndex >= 0)
            {
                int.TryParse(listCollection.GetValue(listIndex, elementIndex, isCategoryFieldIndex), out isCategory);
            }

            if (isCategory == 1)
            {
                int childRowIndex;
                if (dropTableRowById != null && dropTableRowById.TryGetValue(firstDropId, out childRowIndex))
                {
                    return ResolveDropTableIcon(
                        listCollection,
                        database,
                        listIndex,
                        childRowIndex,
                        isCategoryFieldIndex,
                        dropFieldIndexes,
                        dropTableRowById,
                        itemIconSourcesById,
                        itemIconById,
                        depth + 1);
                }

                return null;
            }

            Bitmap icon;
            if (TryResolveItemIconById(listCollection, database, firstDropId, itemIconSourcesById, itemIconById, out icon))
            {
                return icon;
            }

            return null;
        }

        private Bitmap ResolveTradePageIcon(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int elementIndex,
            List<int> goodsFieldIndexes,
            Dictionary<int, ItemIconSource> itemIconSourcesById,
            Dictionary<int, Bitmap> itemIconById)
        {
            if (listCollection == null || goodsFieldIndexes == null || goodsFieldIndexes.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < goodsFieldIndexes.Count; i++)
            {
                int itemId;
                if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, goodsFieldIndexes[i]), out itemId) || itemId <= 0)
                {
                    continue;
                }

                Bitmap icon;
                if (TryResolveItemIconById(listCollection, database, itemId, itemIconSourcesById, itemIconById, out icon))
                {
                    return icon;
                }
            }

            return null;
        }

        private static int GetFirstNonZeroDropId(eListCollection listCollection, int listIndex, int elementIndex, List<int> dropFieldIndexes)
        {
            for (int i = 0; i < dropFieldIndexes.Count; i++)
            {
                int dropId;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, dropFieldIndexes[i]), out dropId) && dropId > 0)
                {
                    return dropId;
                }
            }

            return 0;
        }

        private Dictionary<int, ItemIconSource> BuildItemIconSourceMap(eListCollection listCollection)
        {
            if (object.ReferenceEquals(cachedItemIconSourceCollection, listCollection) && cachedItemIconSourcesById != null)
            {
                return cachedItemIconSourcesById;
            }

            Dictionary<int, ItemIconSource> map = new Dictionary<int, ItemIconSource>();
            if (listCollection == null || listCollection.Lists == null)
            {
                return map;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (listCollection.Lists[listIndex] == null
                    || listCollection.Lists[listIndex].elementFields == null
                    || listCollection.Lists[listIndex].elementValues == null
                    || !HasPrimaryIdField(listCollection.Lists[listIndex].elementFields))
                {
                    continue;
                }

                int iconFieldIndex = GetIconFieldIndex(listCollection.Lists[listIndex].elementFields);
                if (iconFieldIndex < 0)
                {
                    continue;
                }

                for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
                {
                    int id;
                    if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out id) || id <= 0 || map.ContainsKey(id))
                    {
                        continue;
                    }

                    map[id] = new ItemIconSource
                    {
                        ListIndex = listIndex,
                        ElementIndex = elementIndex,
                        IconFieldIndex = iconFieldIndex
                    };
                }
            }

            cachedItemIconSourceCollection = listCollection;
            cachedItemIconSourcesById = map;
            return map;
        }

        private bool TryResolveItemIconById(
            eListCollection listCollection,
            CacheSave database,
            int itemId,
            Dictionary<int, ItemIconSource> itemIconSourcesById,
            Dictionary<int, Bitmap> itemIconById,
            out Bitmap icon)
        {
            icon = null;
            if (itemId <= 0 || itemIconSourcesById == null || itemIconById == null)
            {
                return false;
            }

            if (itemIconById.TryGetValue(itemId, out icon))
            {
                return icon != null;
            }

            ItemIconSource source;
            if (!itemIconSourcesById.TryGetValue(itemId, out source))
            {
                itemIconById[itemId] = null;
                return false;
            }

            icon = ResolveRowIcon(listCollection, database, source.ListIndex, source.ElementIndex, source.IconFieldIndex);
            itemIconById[itemId] = icon;
            return icon != null;
        }

        private Bitmap ResolveRowIcon(eListCollection listCollection, CacheSave database, int listIndex, int elementIndex, int iconFieldIndex)
        {
            Bitmap img = Properties.Resources.NoIcon;
            if (iconFieldIndex < 0)
            {
                return img;
            }

            string rawIcon = listCollection.GetValue(listIndex, elementIndex, iconFieldIndex);
            Bitmap portrait;
            if (creaturePortraitIconService.TryResolvePortrait(database, listCollection, listIndex, rawIcon, out portrait))
            {
                return portrait;
            }

            string path = iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, rawIcon);
            if (database != null && database.sourceBitmap != null && database.ContainsKey(path))
            {
                img = database.images(path);
            }

            return img;
        }

        private static Dictionary<int, int> BuildDropTableRowIndexMap(eListCollection listCollection, int listIndex)
        {
            Dictionary<int, int> map = new Dictionary<int, int>();
            for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
            {
                int id;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out id) && id > 0 && !map.ContainsKey(id))
                {
                    map.Add(id, elementIndex);
                }
            }

            return map;
        }

        private static List<int> GetDropFieldIndexes(string[] fields)
        {
            List<int> indexes = new List<int>();
            if (fields == null)
            {
                return indexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (fieldName.StartsWith("drops_", System.StringComparison.OrdinalIgnoreCase)
                    && fieldName.EndsWith("_id_obj", System.StringComparison.OrdinalIgnoreCase))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private static List<int> GetTradePageGoodsFieldIndexes(string[] fields)
        {
            List<int> indexes = new List<int>();
            if (fields == null)
            {
                return indexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (fieldName.StartsWith("goods_", System.StringComparison.OrdinalIgnoreCase)
                    && fieldName.IndexOf("_1_id_goods", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private static int GetFieldIndex(string[] fields, string fieldName)
        {
            if (fields == null)
            {
                return -1;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], fieldName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetIconFieldIndex(string[] fields)
        {
            return GetFieldIndex(fields, "file_icon") >= 0
                ? GetFieldIndex(fields, "file_icon")
                : GetFieldIndex(fields, "file_icon1");
        }

        private static bool HasPrimaryIdField(string[] fields)
        {
            return fields != null
                && fields.Length > 0
                && (string.Equals(fields[0], "id", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fields[0], "ID", System.StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new string[] { " - " }, System.StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}

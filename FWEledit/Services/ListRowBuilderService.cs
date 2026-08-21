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

        private struct ProduceTypeIconSource
        {
            public int RecipeListIndex;
            public int RecipeElementIndex;
        }

        private readonly IconResolutionService iconResolutionService;
        private readonly CreaturePortraitIconService creaturePortraitIconService;
        private readonly NpcTradePortraitService npcTradePortraitService;
        private readonly NpcTalkPortraitService npcTalkPortraitService;
        private readonly NpcSellPortraitService npcSellPortraitService;
        private readonly NpcTransmitPortraitService npcTransmitPortraitService;
        private readonly NpcHotelPortraitService npcHotelPortraitService;
        private readonly NpcLearnProducePortraitService npcLearnProducePortraitService;
        private readonly MonsterDropPortraitService monsterDropPortraitService;
        private eListCollection cachedItemIconSourceCollection;
        private Dictionary<int, ItemIconSource> cachedItemIconSourcesById;

        public ListRowBuilderService(IconResolutionService iconResolutionService)
        {
            this.iconResolutionService = iconResolutionService;
            this.creaturePortraitIconService = new CreaturePortraitIconService();
            this.npcTradePortraitService = new NpcTradePortraitService();
            this.npcTalkPortraitService = new NpcTalkPortraitService();
            this.npcSellPortraitService = new NpcSellPortraitService();
            this.npcTransmitPortraitService = new NpcTransmitPortraitService();
            this.npcHotelPortraitService = new NpcHotelPortraitService();
            this.npcLearnProducePortraitService = new NpcLearnProducePortraitService();
            this.monsterDropPortraitService = new MonsterDropPortraitService();
        }

        public System.Func<int, int, int> ReferenceCountResolver { get; set; }

        public List<object[]> BuildRows(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            System.Func<int, int, int, string> composeDisplayName,
            bool includeIcons)
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

            if (eListCollection.IsRawTailList(listCollection.Lists[listIndex]))
            {
                int byteCount = 0;
                if (listCollection.Lists[listIndex].elementValues != null
                    && listCollection.Lists[listIndex].elementValues.Length > 0
                    && listCollection.Lists[listIndex].elementValues[0] != null
                    && listCollection.Lists[listIndex].elementValues[0].Length > 0)
                {
                    byte[] raw = listCollection.Lists[listIndex].elementValues[0][0] as byte[];
                    byteCount = raw != null ? raw.Length : 0;
                }

                rows.Add(new object[] { 0, Properties.Resources.blank, "Raw tail preserved (" + byteCount.ToString("N0") + " bytes)", string.Empty });
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
            bool isNpcTalkServiceList = string.Equals(normalizedListName, "NPC_TALK_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcSellServiceList = string.Equals(normalizedListName, "NPC_SELL_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcTransmitServiceList = string.Equals(normalizedListName, "NPC_TRANSMIT_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcHotelServiceList = string.Equals(normalizedListName, "NPC_HOTEL_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcLearnProduceServiceList = string.Equals(normalizedListName, "NPC_LEARN_PRODUCE_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isAddonPackageList = string.Equals(normalizedListName, "ADDON_PACKAGE_CONFIG", System.StringComparison.OrdinalIgnoreCase);
            bool isSuiteList = string.Equals(normalizedListName, "SUITE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            bool isMergeRecipeList = string.Equals(normalizedListName, "MERGE_RECIPE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            bool isProduceTypeList = string.Equals(normalizedListName, "PRODUCE_TYPE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            string inheritedTypeSourceListName;
            string inheritedTypeFieldName;
            bool isInheritedTypeList = TryGetInheritedTypeIconSource(normalizedListName, out inheritedTypeSourceListName, out inheritedTypeFieldName);
            int isCategoryFieldIndex = isDropTableList ? GetFieldIndex(listCollection.Lists[listIndex].elementFields, "is_category") : -1;
            List<int> dropFieldIndexes = isDropTableList ? GetDropFieldIndexes(listCollection.Lists[listIndex].elementFields) : null;
            List<int> tradePageGoodsFieldIndexes = isItemTradePageList ? GetTradePageGoodsFieldIndexes(listCollection.Lists[listIndex].elementFields) : null;
            List<int> suiteEquipmentFieldIndexes = isSuiteList ? GetSuiteEquipmentFieldIndexes(listCollection.Lists[listIndex].elementFields) : null;
            List<int> mergeRecipeItemFieldIndexes = isMergeRecipeList ? GetMergeRecipeItemFieldIndexes(listCollection.Lists[listIndex].elementFields) : null;
            Dictionary<int, ProduceTypeIconSource> produceTypeIconSourceById = includeIcons && isProduceTypeList
                ? BuildProduceTypeIconSourceMap(listCollection)
                : null;
            Dictionary<int, int> dropTableRowById = includeIcons && isDropTableList ? BuildDropTableRowIndexMap(listCollection, listIndex) : null;
            Dictionary<int, Bitmap> addonPackageIconById = includeIcons && isAddonPackageList ? BuildAddonPackageIconMap(listCollection, database) : null;
            Dictionary<int, Bitmap> inheritedTypeIconById = includeIcons && isInheritedTypeList
                ? BuildInheritedTypeIconMap(
                    listCollection,
                    database,
                    inheritedTypeSourceListName,
                    inheritedTypeFieldName)
                : null;
            bool requiresInheritedItemIcons = includeIcons && (isDropTableList || isItemTradePageList || isSuiteList || isMergeRecipeList || isProduceTypeList);
            Dictionary<int, ItemIconSource> itemIconSourcesById = requiresInheritedItemIcons ? BuildItemIconSourceMap(listCollection) : null;
            Dictionary<int, Bitmap> itemIconById = requiresInheritedItemIcons ? new Dictionary<int, Bitmap>() : null;

            for (int e = 0; e < listCollection.Lists[listIndex].elementValues.Length; e++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[0], "ID", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[0], "id", System.StringComparison.OrdinalIgnoreCase))
                {
                    Bitmap img = includeIcons
                        ? ResolveRowIcon(listCollection, database, listIndex, e, pos2)
                        : Properties.Resources.NoIcon;
                    if (includeIcons && isDropTableList)
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
                    else if (includeIcons && isItemTradeList)
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
                    else if (includeIcons && isItemTradePageList)
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
                    else if (includeIcons && isNpcTalkServiceList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int talkServiceId))
                        {
                            if (npcTalkPortraitService.TryResolveTalkPortrait(listCollection, database, talkServiceId, out Bitmap talkIcon)
                                && talkIcon != null)
                            {
                                img = talkIcon;
                            }
                        }
                    }
                    else if (includeIcons && isNpcSellServiceList)
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
                    else if (includeIcons && isNpcTransmitServiceList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int transmitServiceId))
                        {
                            if (npcTransmitPortraitService.TryResolveTransmitPortrait(listCollection, database, transmitServiceId, out Bitmap transmitIcon)
                                && transmitIcon != null)
                            {
                                img = transmitIcon;
                            }
                        }
                    }
                    else if (includeIcons && isNpcHotelServiceList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int hotelServiceId))
                        {
                            if (npcHotelPortraitService.TryResolveHotelPortrait(listCollection, database, hotelServiceId, out Bitmap hotelIcon)
                                && hotelIcon != null)
                            {
                                img = hotelIcon;
                            }
                        }
                    }
                    else if (includeIcons && isNpcLearnProduceServiceList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int learnProduceServiceId))
                        {
                            if (npcLearnProducePortraitService.TryResolveLearnProducePortrait(listCollection, database, learnProduceServiceId, out Bitmap learnProduceIcon)
                                && learnProduceIcon != null)
                            {
                                img = learnProduceIcon;
                            }
                        }
                    }
                    else if (includeIcons && isAddonPackageList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int addonPackageId)
                            && addonPackageIconById != null
                            && addonPackageIconById.TryGetValue(addonPackageId, out Bitmap addonPackageIcon)
                            && addonPackageIcon != null)
                        {
                            img = addonPackageIcon;
                        }
                    }
                    else if (includeIcons && isSuiteList)
                    {
                        Bitmap suiteIcon = ResolveSuiteIcon(
                            listCollection,
                            database,
                            listIndex,
                            e,
                            suiteEquipmentFieldIndexes,
                            itemIconSourcesById,
                            itemIconById);
                        if (suiteIcon != null)
                        {
                            img = suiteIcon;
                        }
                    }
                    else if (includeIcons && isMergeRecipeList)
                    {
                        Bitmap mergeRecipeIcon = ResolveMergeRecipeIcon(
                            listCollection,
                            database,
                            listIndex,
                            e,
                            mergeRecipeItemFieldIndexes,
                            itemIconSourcesById,
                            itemIconById);
                        if (mergeRecipeIcon != null)
                        {
                            img = mergeRecipeIcon;
                        }
                    }
                    else if (includeIcons && isProduceTypeList)
                    {
                        Bitmap produceTypeIcon = ResolveProduceTypeIcon(
                            listCollection,
                            database,
                            listIndex,
                            e,
                            produceTypeIconSourceById,
                            itemIconSourcesById,
                            itemIconById);
                        if (produceTypeIcon != null)
                        {
                            img = produceTypeIcon;
                        }
                    }
                    else if (includeIcons && isInheritedTypeList)
                    {
                        if (int.TryParse(listCollection.GetValue(listIndex, e, 0), out int inheritedTypeId)
                            && inheritedTypeIconById != null
                            && inheritedTypeIconById.TryGetValue(inheritedTypeId, out Bitmap inheritedTypeIcon)
                            && inheritedTypeIcon != null)
                        {
                            img = inheritedTypeIcon;
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

        public Bitmap BuildRowIcon(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int elementIndex)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return Properties.Resources.NoIcon;
            }

            int iconFieldIndex = -1;
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "file_icon", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[i], "file_icon1", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[i], "file_head_icon", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(listCollection.Lists[listIndex].elementFields[i], "file_self_head_icon", System.StringComparison.OrdinalIgnoreCase))
                {
                    iconFieldIndex = i;
                    break;
                }
            }

            string normalizedListName = NormalizeListName(listCollection.Lists[listIndex].listName);
            bool isDropTableList = string.Equals(normalizedListName, "DROPTABLE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            bool isItemTradeList = string.Equals(normalizedListName, "ITEM_TRADE_ESSENCE", System.StringComparison.OrdinalIgnoreCase);
            bool isItemTradePageList = string.Equals(normalizedListName, "ITEM_TRADE_PAGE_CONFIG", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcTalkServiceList = string.Equals(normalizedListName, "NPC_TALK_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcSellServiceList = string.Equals(normalizedListName, "NPC_SELL_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcTransmitServiceList = string.Equals(normalizedListName, "NPC_TRANSMIT_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcHotelServiceList = string.Equals(normalizedListName, "NPC_HOTEL_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isNpcLearnProduceServiceList = string.Equals(normalizedListName, "NPC_LEARN_PRODUCE_SERVICE", System.StringComparison.OrdinalIgnoreCase);
            bool isAddonPackageList = string.Equals(normalizedListName, "ADDON_PACKAGE_CONFIG", System.StringComparison.OrdinalIgnoreCase);

            if (isDropTableList)
            {
                int dropTableId;
                Bitmap monsterPortrait = null;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out dropTableId))
                {
                    monsterDropPortraitService.TryResolveDropPortrait(listCollection, database, dropTableId, out monsterPortrait);
                }

                if (monsterPortrait != null)
                {
                    return monsterPortrait;
                }

                Bitmap dropTableIcon = ResolveDropTableIcon(
                    listCollection,
                    database,
                    listIndex,
                    elementIndex,
                    GetFieldIndex(listCollection.Lists[listIndex].elementFields, "is_category"),
                    GetDropFieldIndexes(listCollection.Lists[listIndex].elementFields),
                    BuildDropTableRowIndexMap(listCollection, listIndex),
                    BuildItemIconSourceMap(listCollection),
                    new Dictionary<int, Bitmap>(),
                    0);
                if (dropTableIcon != null)
                {
                    return dropTableIcon;
                }
            }

            if (isItemTradeList)
            {
                int tradeServiceId;
                Bitmap tradeIcon;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out tradeServiceId)
                    && npcTradePortraitService.TryResolveTradePortrait(listCollection, database, tradeServiceId, out tradeIcon)
                    && tradeIcon != null)
                {
                    return tradeIcon;
                }
            }

            if (isItemTradePageList)
            {
                Bitmap tradePageIcon = ResolveTradePageIcon(
                    listCollection,
                    database,
                    listIndex,
                    elementIndex,
                    GetTradePageGoodsFieldIndexes(listCollection.Lists[listIndex].elementFields),
                    BuildItemIconSourceMap(listCollection),
                    new Dictionary<int, Bitmap>());
                if (tradePageIcon != null)
                {
                    return tradePageIcon;
                }
            }

            if (isNpcTalkServiceList)
            {
                int talkServiceId;
                Bitmap talkIcon;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out talkServiceId)
                    && npcTalkPortraitService.TryResolveTalkPortrait(listCollection, database, talkServiceId, out talkIcon)
                    && talkIcon != null)
                {
                    return talkIcon;
                }
            }

            if (isNpcSellServiceList)
            {
                int sellServiceId;
                Bitmap sellIcon;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out sellServiceId)
                    && npcSellPortraitService.TryResolveSellPortrait(listCollection, database, sellServiceId, out sellIcon)
                    && sellIcon != null)
                {
                    return sellIcon;
                }
            }

            if (isNpcTransmitServiceList)
            {
                int transmitServiceId;
                Bitmap transmitIcon;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out transmitServiceId)
                    && npcTransmitPortraitService.TryResolveTransmitPortrait(listCollection, database, transmitServiceId, out transmitIcon)
                    && transmitIcon != null)
                {
                    return transmitIcon;
                }
            }

            if (isNpcHotelServiceList)
            {
                int hotelServiceId;
                Bitmap hotelIcon;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out hotelServiceId)
                    && npcHotelPortraitService.TryResolveHotelPortrait(listCollection, database, hotelServiceId, out hotelIcon)
                    && hotelIcon != null)
                {
                    return hotelIcon;
                }
            }

            if (isNpcLearnProduceServiceList)
            {
                int learnProduceServiceId;
                Bitmap learnProduceIcon;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out learnProduceServiceId)
                    && npcLearnProducePortraitService.TryResolveLearnProducePortrait(listCollection, database, learnProduceServiceId, out learnProduceIcon)
                    && learnProduceIcon != null)
                {
                    return learnProduceIcon;
                }
            }

            if (isAddonPackageList)
            {
                int addonPackageId;
                Bitmap addonPackageIcon;
                Dictionary<int, Bitmap> addonPackageIconById = BuildAddonPackageIconMap(listCollection, database);
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out addonPackageId)
                    && addonPackageIconById.TryGetValue(addonPackageId, out addonPackageIcon)
                    && addonPackageIcon != null)
                {
                    return addonPackageIcon;
                }
            }

            if (string.Equals(normalizedListName, "SUITE_ESSENCE", System.StringComparison.OrdinalIgnoreCase))
            {
                Bitmap suiteIcon = ResolveSuiteIcon(
                    listCollection,
                    database,
                    listIndex,
                    elementIndex,
                    GetSuiteEquipmentFieldIndexes(listCollection.Lists[listIndex].elementFields),
                    BuildItemIconSourceMap(listCollection),
                    new Dictionary<int, Bitmap>());
                if (suiteIcon != null)
                {
                    return suiteIcon;
                }
            }

            if (string.Equals(normalizedListName, "MERGE_RECIPE_ESSENCE", System.StringComparison.OrdinalIgnoreCase))
            {
                Bitmap mergeRecipeIcon = ResolveMergeRecipeIcon(
                    listCollection,
                    database,
                    listIndex,
                    elementIndex,
                    GetMergeRecipeItemFieldIndexes(listCollection.Lists[listIndex].elementFields),
                    BuildItemIconSourceMap(listCollection),
                    new Dictionary<int, Bitmap>());
                if (mergeRecipeIcon != null)
                {
                    return mergeRecipeIcon;
                }
            }

            if (string.Equals(normalizedListName, "PRODUCE_TYPE_ESSENCE", System.StringComparison.OrdinalIgnoreCase))
            {
                Bitmap produceTypeIcon = ResolveProduceTypeIcon(
                    listCollection,
                    database,
                    listIndex,
                    elementIndex,
                    BuildProduceTypeIconSourceMap(listCollection),
                    BuildItemIconSourceMap(listCollection),
                    new Dictionary<int, Bitmap>());
                if (produceTypeIcon != null)
                {
                    return produceTypeIcon;
                }
            }

            string inheritedTypeSourceListName;
            string inheritedTypeFieldName;
            if (TryGetInheritedTypeIconSource(normalizedListName, out inheritedTypeSourceListName, out inheritedTypeFieldName))
            {
                int typeId;
                Dictionary<int, Bitmap> inheritedTypeIconById = BuildInheritedTypeIconMap(
                    listCollection,
                    database,
                    inheritedTypeSourceListName,
                    inheritedTypeFieldName);
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out typeId)
                    && inheritedTypeIconById != null
                    && inheritedTypeIconById.TryGetValue(typeId, out Bitmap inheritedIcon)
                    && inheritedIcon != null)
                {
                    return inheritedIcon;
                }
            }

            return ResolveRowIcon(listCollection, database, listIndex, elementIndex, iconFieldIndex);
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

        private Bitmap ResolveSuiteIcon(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int elementIndex,
            List<int> equipmentFieldIndexes,
            Dictionary<int, ItemIconSource> itemIconSourcesById,
            Dictionary<int, Bitmap> itemIconById)
        {
            if (listCollection == null || equipmentFieldIndexes == null || equipmentFieldIndexes.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < equipmentFieldIndexes.Count; i++)
            {
                int equipmentId;
                if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, equipmentFieldIndexes[i]), out equipmentId) || equipmentId <= 0)
                {
                    continue;
                }

                Bitmap icon;
                if (TryResolveItemIconById(listCollection, database, equipmentId, itemIconSourcesById, itemIconById, out icon))
                {
                    return icon;
                }
            }

            return null;
        }

        private Bitmap ResolveMergeRecipeIcon(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int elementIndex,
            List<int> itemFieldIndexes,
            Dictionary<int, ItemIconSource> itemIconSourcesById,
            Dictionary<int, Bitmap> itemIconById)
        {
            if (listCollection == null || itemFieldIndexes == null || itemFieldIndexes.Count == 0)
            {
                return null;
            }

            for (int i = 0; i < itemFieldIndexes.Count; i++)
            {
                int itemId;
                if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, itemFieldIndexes[i]), out itemId) || itemId <= 0)
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

        private Bitmap ResolveProduceTypeIcon(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            int elementIndex,
            Dictionary<int, ProduceTypeIconSource> produceTypeIconSourceById,
            Dictionary<int, ItemIconSource> itemIconSourcesById,
            Dictionary<int, Bitmap> itemIconById)
        {
            if (listCollection == null || produceTypeIconSourceById == null)
            {
                return null;
            }

            int produceTypeId;
            if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out produceTypeId) || produceTypeId <= 0)
            {
                return null;
            }

            ProduceTypeIconSource source;
            if (!produceTypeIconSourceById.TryGetValue(produceTypeId, out source)
                || source.RecipeListIndex < 0
                || source.RecipeListIndex >= listCollection.Lists.Length
                || source.RecipeElementIndex < 0
                || source.RecipeElementIndex >= listCollection.Lists[source.RecipeListIndex].elementValues.Length)
            {
                return null;
            }

            List<int> productFieldIndexes = GetRecipeProductItemFieldIndexes(listCollection.Lists[source.RecipeListIndex].elementFields);
            for (int i = 0; i < productFieldIndexes.Count; i++)
            {
                int itemId;
                if (!int.TryParse(listCollection.GetValue(source.RecipeListIndex, source.RecipeElementIndex, productFieldIndexes[i]), out itemId)
                    || itemId <= 0)
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

        private static Dictionary<int, ProduceTypeIconSource> BuildProduceTypeIconSourceMap(eListCollection listCollection)
        {
            Dictionary<int, ProduceTypeIconSource> map = new Dictionary<int, ProduceTypeIconSource>();
            int recipeListIndex = FindListIndexByNormalizedName(listCollection, "RECIPE_ESSENCE");
            if (recipeListIndex < 0
                || listCollection.Lists[recipeListIndex] == null
                || listCollection.Lists[recipeListIndex].elementFields == null
                || listCollection.Lists[recipeListIndex].elementValues == null)
            {
                return map;
            }

            string[] fields = listCollection.Lists[recipeListIndex].elementFields;
            int produceTypeFieldIndex = GetFieldIndex(fields, "produce_type");
            List<int> productFieldIndexes = GetRecipeProductItemFieldIndexes(fields);
            if (produceTypeFieldIndex < 0 || productFieldIndexes.Count == 0)
            {
                return map;
            }

            for (int elementIndex = 0; elementIndex < listCollection.Lists[recipeListIndex].elementValues.Length; elementIndex++)
            {
                int produceTypeId;
                if (!int.TryParse(listCollection.GetValue(recipeListIndex, elementIndex, produceTypeFieldIndex), out produceTypeId)
                    || produceTypeId <= 0
                    || map.ContainsKey(produceTypeId)
                    || !HasPositiveValueInAnyField(listCollection, recipeListIndex, elementIndex, productFieldIndexes))
                {
                    continue;
                }

                map[produceTypeId] = new ProduceTypeIconSource
                {
                    RecipeListIndex = recipeListIndex,
                    RecipeElementIndex = elementIndex
                };
            }

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
            if (database != null && database.ContainsKey(path))
            {
                img = database.images(path);
            }

            return img;
        }

        private Dictionary<int, Bitmap> BuildAddonPackageIconMap(eListCollection listCollection, CacheSave database)
        {
            Dictionary<int, Bitmap> map = new Dictionary<int, Bitmap>();
            if (listCollection == null || listCollection.Lists == null)
            {
                return map;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null || list.elementFields == null || list.elementValues == null)
                {
                    continue;
                }

                string[] fields = list.elementFields;
                List<int> packageFieldIndexes = new List<int>();
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    if (IsAddonPackageReferenceField(fields[fieldIndex]))
                    {
                        packageFieldIndexes.Add(fieldIndex);
                    }
                }

                if (packageFieldIndexes.Count == 0)
                {
                    continue;
                }

                int iconFieldIndex = GetIconFieldIndex(fields);
                if (iconFieldIndex < 0)
                {
                    continue;
                }

                for (int elementIndex = 0; elementIndex < list.elementValues.Length; elementIndex++)
                {
                    Bitmap sourceIcon = null;
                    foreach (int fieldIndex in packageFieldIndexes)
                    {
                        int packageId;
                        if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, fieldIndex), out packageId)
                            || packageId <= 0
                            || map.ContainsKey(packageId))
                        {
                            continue;
                        }

                        if (sourceIcon == null)
                        {
                            sourceIcon = ResolveRowIcon(listCollection, database, listIndex, elementIndex, iconFieldIndex);
                        }

                        if (sourceIcon != null)
                        {
                            map[packageId] = sourceIcon;
                        }
                    }
                }
            }

            return map;
        }

        private Dictionary<int, Bitmap> BuildInheritedTypeIconMap(eListCollection listCollection, CacheSave database, string sourceListName, string typeFieldName)
        {
            Dictionary<int, Bitmap> map = new Dictionary<int, Bitmap>();
            if (listCollection == null || listCollection.Lists == null)
            {
                return map;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null
                    || list.elementFields == null
                    || list.elementValues == null
                    || !string.Equals(NormalizeListName(list.listName), sourceListName, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int typeFieldIndex = GetFieldIndex(list.elementFields, typeFieldName);
                if (typeFieldIndex < 0 && string.Equals(typeFieldName, "id_major_type", System.StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = GetFieldIndex(list.elementFields, "major_type");
                }
                else if (typeFieldIndex < 0 && string.Equals(typeFieldName, "id_sub_type", System.StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = GetFieldIndex(list.elementFields, "sub_type");
                }

                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                if (typeFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int elementIndex = 0; elementIndex < list.elementValues.Length; elementIndex++)
                {
                    int typeId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, typeFieldIndex), out typeId)
                        || typeId <= 0
                        || map.ContainsKey(typeId))
                    {
                        continue;
                    }

                    Bitmap sourceIcon = ResolveRowIcon(listCollection, database, listIndex, elementIndex, iconFieldIndex);
                    if (sourceIcon != null)
                    {
                        map[typeId] = sourceIcon;
                    }
                }
            }

            return map;
        }

        private static bool IsAddonPackageReferenceField(string fieldName)
        {
            string normalized = fieldName ?? string.Empty;
            return string.Equals(normalized, "id_addon_package", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_prefix_addon_package", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_postfix_addon_package", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_special_addon_package", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_sign_addon_package", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "special_addon_package_id", System.StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("enhanced_prop_package_", System.StringComparison.OrdinalIgnoreCase);
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

        private static List<int> GetSuiteEquipmentFieldIndexes(string[] fields)
        {
            List<int> indexes = new List<int>();
            if (fields == null)
            {
                return indexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (fieldName.StartsWith("equipments_", System.StringComparison.OrdinalIgnoreCase)
                    && fieldName.EndsWith("_id", System.StringComparison.OrdinalIgnoreCase))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private static List<int> GetMergeRecipeItemFieldIndexes(string[] fields)
        {
            List<int> producedItemIndexes = new List<int>();
            List<int> inputItemIndexes = new List<int>();
            if (fields == null)
            {
                return producedItemIndexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (fieldName.StartsWith("makes_", System.StringComparison.OrdinalIgnoreCase)
                    && fieldName.IndexOf("_id", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    producedItemIndexes.Add(i);
                }
                else if ((fieldName.StartsWith("mains_", System.StringComparison.OrdinalIgnoreCase)
                        && fieldName.IndexOf("_id_main", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    || (fieldName.StartsWith("helpers_", System.StringComparison.OrdinalIgnoreCase)
                        && fieldName.EndsWith("_id", System.StringComparison.OrdinalIgnoreCase)))
                {
                    inputItemIndexes.Add(i);
                }
            }

            producedItemIndexes.AddRange(inputItemIndexes);
            return producedItemIndexes;
        }

        private static List<int> GetRecipeProductItemFieldIndexes(string[] fields)
        {
            List<int> productIndexes = new List<int>();
            List<int> acquiredIndexes = new List<int>();
            if (fields == null)
            {
                return productIndexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (fieldName.StartsWith("products_", System.StringComparison.OrdinalIgnoreCase)
                    && fieldName.IndexOf("id_to_make", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    productIndexes.Add(i);
                }
                else if (fieldName.StartsWith("acquired_", System.StringComparison.OrdinalIgnoreCase)
                    && (fieldName.EndsWith("_id", System.StringComparison.OrdinalIgnoreCase)
                        || fieldName.IndexOf("_id_", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    acquiredIndexes.Add(i);
                }
            }

            productIndexes.AddRange(acquiredIndexes);
            return productIndexes;
        }

        private static bool HasPositiveValueInAnyField(eListCollection listCollection, int listIndex, int elementIndex, List<int> fieldIndexes)
        {
            for (int i = 0; i < fieldIndexes.Count; i++)
            {
                int value;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, fieldIndexes[i]), out value) && value > 0)
                {
                    return true;
                }
            }

            return false;
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

        private static int FindListIndexByNormalizedName(eListCollection listCollection, string normalizedName)
        {
            if (listCollection == null || listCollection.Lists == null || string.IsNullOrWhiteSpace(normalizedName))
            {
                return -1;
            }

            for (int i = 0; i < listCollection.Lists.Length; i++)
            {
                if (listCollection.Lists[i] != null
                    && string.Equals(NormalizeListName(listCollection.Lists[i].listName), normalizedName, System.StringComparison.OrdinalIgnoreCase))
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

        private static bool TryGetInheritedTypeIconSource(string normalizedListName, out string sourceListName, out string typeFieldName)
        {
            sourceListName = string.Empty;
            typeFieldName = string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedListName))
            {
                return false;
            }

            const string majorSuffix = "_MAJOR_TYPE";
            const string subSuffix = "_SUB_TYPE";
            if (string.Equals(normalizedListName, "MONSTER_TYPE", System.StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = "MONSTER_ESSENCE";
                typeFieldName = "id_type";
                return true;
            }

            if (string.Equals(normalizedListName, "KM_PARAM_ADJUST_CONFIG", System.StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = "MONSTER_ESSENCE";
                typeFieldName = "id_adjust_config";
                return true;
            }

            if (normalizedListName.EndsWith(majorSuffix, System.StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = normalizedListName.Substring(0, normalizedListName.Length - majorSuffix.Length) + "_ESSENCE";
                typeFieldName = "id_major_type";
                return true;
            }

            if (normalizedListName.EndsWith(subSuffix, System.StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = normalizedListName.Substring(0, normalizedListName.Length - subSuffix.Length) + "_ESSENCE";
                typeFieldName = "id_sub_type";
                return true;
            }

            return false;
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

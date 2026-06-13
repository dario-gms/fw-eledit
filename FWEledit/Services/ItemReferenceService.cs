using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemReferenceService
    {
        private const int AllListsTargetIndex = -1;
        private const int ItemListsTargetIndex = -2;
        private const int TitleDefinitionsTargetIndex = TitleDefinitionCatalog.TargetListIndex;
        private readonly NpcTradePortraitService npcTradePortraitService = new NpcTradePortraitService();
        private readonly NpcSellPortraitService npcSellPortraitService = new NpcSellPortraitService();
        private readonly MonsterDropPortraitService monsterDropPortraitService = new MonsterDropPortraitService();

        private eListCollection cachedListCollection;
        private CacheSave cachedDatabase;
        private IconResolutionService cachedIconResolutionService;
        private readonly Dictionary<int, List<ItemReferenceOption>> optionsByListIndex = new Dictionary<int, List<ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<int, ItemReferenceOption>> optionsByIdByListIndex = new Dictionary<int, Dictionary<int, ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<string, ItemReferenceOption>> optionsByNameByListIndex = new Dictionary<int, Dictionary<string, ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<int, int>> elementIndexByIdByListIndex = new Dictionary<int, Dictionary<int, int>>();
        private List<ItemReferenceOption> searchableOptions;
        private Dictionary<int, ItemReferenceOption> searchableOptionsById;
        private Dictionary<string, ItemReferenceOption> searchableOptionsByName;
        private List<ItemReferenceOption> searchableItemOptions;
        private Dictionary<int, ItemReferenceOption> searchableItemOptionsById;
        private Dictionary<string, ItemReferenceOption> searchableItemOptionsByName;
        private readonly Dictionary<string, ItemReferenceOption> preferredOptionsByFieldAndId = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);

        public void ClearCache()
        {
            cachedListCollection = null;
            cachedDatabase = null;
            cachedIconResolutionService = null;
            optionsByListIndex.Clear();
            optionsByIdByListIndex.Clear();
            optionsByNameByListIndex.Clear();
            elementIndexByIdByListIndex.Clear();
            searchableOptions = null;
            searchableOptionsById = null;
            searchableOptionsByName = null;
            searchableItemOptions = null;
            searchableItemOptionsById = null;
            searchableItemOptionsByName = null;
        }

        public Dictionary<int, List<ItemReferenceOption>> ExportOptionsCache(
            eListCollection listCollection,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            EnsureCacheContext(listCollection, database, iconResolutionService);

            Dictionary<int, List<ItemReferenceOption>> clone = new Dictionary<int, List<ItemReferenceOption>>();
            foreach (KeyValuePair<int, List<ItemReferenceOption>> pair in optionsByListIndex)
            {
                clone[pair.Key] = CloneOptions(pair.Value);
            }

            return clone;
        }

        public void ImportOptionsCache(
            eListCollection listCollection,
            CacheSave database,
            IconResolutionService iconResolutionService,
            Dictionary<int, List<ItemReferenceOption>> cache)
        {
            EnsureCacheContext(listCollection, database, iconResolutionService);
            optionsByListIndex.Clear();
            optionsByIdByListIndex.Clear();
            optionsByNameByListIndex.Clear();
            elementIndexByIdByListIndex.Clear();
            searchableOptions = null;
            searchableOptionsById = null;
            searchableOptionsByName = null;
            searchableItemOptions = null;
            searchableItemOptionsById = null;
            searchableItemOptionsByName = null;

            if (cache == null)
            {
                return;
            }

            foreach (KeyValuePair<int, List<ItemReferenceOption>> pair in cache)
            {
                List<ItemReferenceOption> cloned = CloneOptions(pair.Value);
                optionsByListIndex[pair.Key] = cloned;
                IndexOptions(pair.Key, cloned);
            }
        }

        public bool IsItemListTargetIndex(int targetListIndex)
        {
            return targetListIndex == ItemListsTargetIndex;
        }

        public bool IsTitleDefinitionTargetIndex(int targetListIndex)
        {
            return targetListIndex == TitleDefinitionsTargetIndex;
        }

        public bool IsItemBearingList(eListCollection listCollection, int listIndex)
        {
            return IsItemList(listCollection, listIndex);
        }

        public bool IsReferenceField(eListCollection listCollection, int listIndex, string fieldName)
        {
            return IsReferenceField(listCollection, listIndex, -1, fieldName);
        }

        public bool IsReferenceField(eListCollection listCollection, int listIndex, int elementIndex, string fieldName)
        {
            int targetListIndex;
            return TryGetTargetListIndex(listCollection, listIndex, elementIndex, fieldName, out targetListIndex);
        }

        public bool TryGetTargetListIndex(eListCollection listCollection, int listIndex, string fieldName, out int targetListIndex)
        {
            return TryGetTargetListIndex(listCollection, listIndex, -1, fieldName, out targetListIndex);
        }

        public bool TryGetTargetListIndex(eListCollection listCollection, int listIndex, int elementIndex, string fieldName, out int targetListIndex)
        {
            targetListIndex = -1;
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string sourceListName = NormalizeListName(listCollection.Lists[listIndex].listName);
            string name = fieldName.Trim();
            string normalizedName = name.ToLowerInvariant();
            string targetListName = null;

            if (string.Equals(sourceListName, "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && TryGetNpcServiceTargetListName(name, out targetListName))
            {
            }
            else if (string.Equals(sourceListName, "ITEM_TRADE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && IsItemTradePageField(name))
            {
                targetListName = "ITEM_TRADE_PAGE_CONFIG";
            }
            else if (string.Equals(name, "id_title", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "title_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListIndex = TitleDefinitionsTargetIndex;
                return true;
            }
            else if (name.StartsWith("id_estone_", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "ESTONE_ESSENCE";
            }
            else if (string.Equals(name, "default_pet_egg_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PET_EGG_ESSENCE";
            }
            else if (string.Equals(name, "id_pet_bedge", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PET_BEDGE_ESSENCE";
            }
            else if (string.Equals(name, "id_level_exp", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PLAYER_SUB_PROF_LEVEL_EXP_CONFIG";
            }
            else if (string.Equals(name, "basic_show_level", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "TASKNORMALMATTER_ESSENCE";
            }
            else if (string.Equals(name, "id_uninstall", StringComparison.OrdinalIgnoreCase)
                && string.Equals(sourceListName, "PSTONE_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (name.StartsWith("id_pstone_", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PSTONE_ESSENCE";
            }
            else if (name.StartsWith("id_sstone_", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "SSTONE_ESSENCE";
            }
            else if (string.Equals(name, "id_addon_package", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(sourceListName, "PSTONE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sourceListName, "RUNE_CHAIN_ESSENCE", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sourceListName, "SOUL_TOOL_SKILL_RANDOM_TABLE_CONFIG", StringComparison.OrdinalIgnoreCase)))
            {
                targetListName = "ADDON_PACKAGE_CONFIG";
            }
            else if (name.StartsWith("enhanced_prop_package_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_sign_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_special_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "special_addon_package_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_prefix_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_postfix_addon_package", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "ADDON_PACKAGE_CONFIG";
            }
            else if (string.Equals(name, "id_special_status_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "special_status_package_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "SPECIAL_STATUS_PACKAGE_CONFIG";
            }
            else if (string.Equals(name, "id_quality", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "EQUIPMENT_QUALITY_CONFIG";
            }
            else if (string.Equals(name, "id_identify", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "IDENTIFY_SCROLL_ESSENCE";
            }
            else if (string.Equals(name, "id_equip_prop", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "EQUIPMENT_PROPERTY_RANDOM_CONFIG";
            }
            else if (string.Equals(sourceListName, "DROPTABLE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && name.StartsWith("drops_", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith("_id_obj", StringComparison.OrdinalIgnoreCase))
            {
                if (IsDropTableCategoryRow(listCollection, listIndex, elementIndex))
                {
                    return TryFindListIndexByName(listCollection, "DROPTABLE_ESSENCE", out targetListIndex);
                }

                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "ITEM_TRADE_PAGE_CONFIG", StringComparison.OrdinalIgnoreCase)
                && IsItemTradePageItemField(name))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (RandomGiftBagRewardTypeCatalog.IsRewardIdFieldName(listCollection, listIndex, name))
            {
                int rewardType;
                if (elementIndex >= 0
                    && TryGetRandomGiftBagRewardType(listCollection, listIndex, elementIndex, name, out rewardType)
                    && rewardType == RandomGiftBagRewardTypeCatalog.TitleValue)
                {
                    targetListIndex = TitleDefinitionsTargetIndex;
                    return true;
                }

                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "NPC_SELL_SERVICE", StringComparison.OrdinalIgnoreCase)
                && IsNpcSellGoodsField(name))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if ((name.StartsWith("extend_identify_attr_tool_", StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith("_tool_id", StringComparison.OrdinalIgnoreCase))
                || string.Equals(name, "reidentify_extend_identify_attr_tool_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (TryGetMappedTargetListName(sourceListName, normalizedName, out targetListName))
            {
            }
            else if (IsGenericItemReferenceField(sourceListName, normalizedName))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }

            if (string.IsNullOrWhiteSpace(targetListName))
            {
                return false;
            }

            return TryFindListIndexByName(listCollection, targetListName, out targetListIndex);
        }

        public string FormatReferenceValue(eListCollection listCollection, int listIndex, string fieldName, string rawValue)
        {
            return FormatReferenceValue(listCollection, listIndex, -1, fieldName, rawValue, null, null);
        }

        public string FormatReferenceValue(eListCollection listCollection, int listIndex, int elementIndex, string fieldName, string rawValue)
        {
            return FormatReferenceValue(listCollection, listIndex, elementIndex, fieldName, rawValue, null, null);
        }

        public string FormatReferenceValue(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            string fieldName,
            string rawValue,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            int id;
            if (!int.TryParse(rawValue, out id) || id <= 0)
            {
                return rawValue ?? string.Empty;
            }

            ItemReferenceOption preferredOption;
            if (TryGetPreferredReferenceOption(listIndex, elementIndex, fieldName, id, out preferredOption))
            {
                return string.IsNullOrWhiteSpace(preferredOption.Name) ? rawValue : preferredOption.Name;
            }

            int targetListIndex;
            if (!TryGetTargetListIndex(listCollection, listIndex, elementIndex, fieldName, out targetListIndex))
            {
                return rawValue ?? string.Empty;
            }

            EnsureCacheContext(listCollection, database, iconResolutionService);

            ItemReferenceOption option;
            if (targetListIndex == TitleDefinitionsTargetIndex && TitleDefinitionCatalog.TryGetOptionById(id, out option))
            {
                return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
            }

            if (targetListIndex >= 0 && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
            }

            if (targetListIndex == ItemListsTargetIndex)
            {
                if (!TryFindItemOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option))
                {
                    return rawValue ?? string.Empty;
                }
            }
            else if (!TryFindOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option))
            {
                return rawValue ?? string.Empty;
            }

            return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
        }

        public bool TryResolveReferenceOption(eListCollection listCollection, int listIndex, string fieldName, string rawValue, CacheSave database, IconResolutionService iconResolutionService, out ItemReferenceOption option)
        {
            return TryResolveReferenceOption(listCollection, listIndex, -1, fieldName, rawValue, database, iconResolutionService, out option);
        }

        public bool TryResolveReferenceOption(eListCollection listCollection, int listIndex, int elementIndex, string fieldName, string rawValue, CacheSave database, IconResolutionService iconResolutionService, out ItemReferenceOption option)
        {
            option = null;
            int id;
            if (!int.TryParse(rawValue, out id) || id <= 0)
            {
                return false;
            }

            int targetListIndex;
            if (!TryGetTargetListIndex(listCollection, listIndex, elementIndex, fieldName, out targetListIndex))
            {
                return false;
            }

            if (TryGetPreferredReferenceOption(listIndex, elementIndex, fieldName, id, out option))
            {
                return true;
            }

            if (targetListIndex == TitleDefinitionsTargetIndex && TitleDefinitionCatalog.TryGetOptionById(id, out option))
            {
                return true;
            }

            if (targetListIndex >= 0 && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return true;
            }

            if (targetListIndex == ItemListsTargetIndex)
            {
                return TryFindItemOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option);
            }

            return TryFindOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option);
        }

        public string NormalizeReferenceInput(eListCollection listCollection, int listIndex, string fieldName, string value)
        {
            return NormalizeReferenceInput(listCollection, listIndex, -1, fieldName, value);
        }

        public string NormalizeReferenceInput(eListCollection listCollection, int listIndex, int elementIndex, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            int id;
            if (int.TryParse(value.Trim(), out id))
            {
                return id.ToString();
            }

            int targetListIndex;
            if (!TryGetTargetListIndex(listCollection, listIndex, elementIndex, fieldName, out targetListIndex))
            {
                return value;
            }

            ItemReferenceOption option;
            if (targetListIndex == TitleDefinitionsTargetIndex && TitleDefinitionCatalog.TryGetOptionByName(value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            if (targetListIndex >= 0 && TryFindOptionByName(listCollection, targetListIndex, value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            if (targetListIndex == ItemListsTargetIndex && TryFindItemOptionByNameAcrossLists(listCollection, value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            if (TryFindOptionByNameAcrossLists(listCollection, value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            return value;
        }

        public void RememberReferenceOverride(int sourceListIndex, int sourceElementIndex, string fieldName, ItemReferenceOption option)
        {
            if (option == null || option.Id <= 0)
            {
                return;
            }

            string key = BuildPreferredReferenceKey(sourceListIndex, sourceElementIndex, fieldName, option.Id);
            preferredOptionsByFieldAndId[key] = option;
        }

        private bool TryGetPreferredReferenceOption(int sourceListIndex, int sourceElementIndex, string fieldName, int id, out ItemReferenceOption option)
        {
            option = null;
            if (id <= 0)
            {
                return false;
            }

            if (preferredOptionsByFieldAndId.TryGetValue(BuildPreferredReferenceKey(sourceListIndex, sourceElementIndex, fieldName, id), out option))
            {
                return true;
            }

            return preferredOptionsByFieldAndId.TryGetValue(BuildPreferredReferenceKey(sourceListIndex, -1, fieldName, id), out option);
        }

        private static string BuildPreferredReferenceKey(int sourceListIndex, int sourceElementIndex, string fieldName, int id)
        {
            return sourceListIndex.ToString()
                + "|"
                + sourceElementIndex.ToString()
                + "|"
                + (fieldName ?? string.Empty).Trim().ToLowerInvariant()
                + "|"
                + id.ToString();
        }

        public List<ItemReferenceOption> BuildOptions(eListCollection listCollection, int targetListIndex)
        {
            return BuildOptions(listCollection, targetListIndex, null, null);
        }

        public List<ItemReferenceOption> BuildOptions(eListCollection listCollection, int targetListIndex, CacheSave database, IconResolutionService iconResolutionService)
        {
            if (targetListIndex == TitleDefinitionsTargetIndex)
            {
                return BuildTitleDefinitionOptions();
            }

            if (listCollection == null || targetListIndex < 0 || targetListIndex >= listCollection.Lists.Length)
            {
                return new List<ItemReferenceOption>();
            }

            EnsureCacheContext(listCollection, database, iconResolutionService);

            List<ItemReferenceOption> options;
            if (!optionsByListIndex.TryGetValue(targetListIndex, out options))
            {
                options = BuildOptionsUncached(listCollection, targetListIndex, database, iconResolutionService);
                optionsByListIndex[targetListIndex] = options;
                IndexOptions(targetListIndex, options);
            }

            return options;
        }

        public List<ItemReferenceOption> BuildTitleDefinitionOptions()
        {
            return TitleDefinitionCatalog.BuildOptions();
        }

        private List<ItemReferenceOption> BuildOptionsUncached(eListCollection listCollection, int targetListIndex, CacheSave database, IconResolutionService iconResolutionService)
        {
            List<ItemReferenceOption> options = new List<ItemReferenceOption>();
            if (listCollection == null || targetListIndex < 0 || targetListIndex >= listCollection.Lists.Length)
            {
                return options;
            }

            int nameIndex = GetNameFieldIndex(listCollection, targetListIndex);
            int iconIndex = GetIconFieldIndex(listCollection, targetListIndex);
            int qualityIndex = GetQualityFieldIndex(listCollection, targetListIndex);
            string listName = listCollection.Lists[targetListIndex].listName ?? string.Empty;
            string normalizedListName = NormalizeListName(listName);
            Dictionary<int, ItemReferenceOption> addonPackageUsageMap = string.Equals(normalizedListName, "ADDON_PACKAGE_CONFIG", StringComparison.OrdinalIgnoreCase)
                ? BuildAddonPackageUsageMap(listCollection, targetListIndex, database, iconResolutionService)
                : null;
            for (int i = 0; i < listCollection.Lists[targetListIndex].elementValues.Length; i++)
            {
                int id;
                if (!int.TryParse(listCollection.GetValue(targetListIndex, i, 0), out id))
                {
                    continue;
                }

                string name = nameIndex >= 0 ? listCollection.GetValue(targetListIndex, i, nameIndex) : string.Empty;
                string iconKey = ResolveOptionIconKey(listCollection, database, iconResolutionService, targetListIndex, i, iconIndex);
                int quality = ResolveOptionQuality(listCollection, targetListIndex, i, qualityIndex);
                if (string.Equals(normalizedListName, "EQUIPMENT_ADDON", StringComparison.OrdinalIgnoreCase))
                {
                    string decodedName = DecodeEquipmentAddonName(listCollection, database, id);
                    if (!string.IsNullOrWhiteSpace(decodedName))
                    {
                        name = decodedName;
                    }
                }
                else if (string.Equals(normalizedListName, "ITEM_TRADE_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    string tradePortraitPath;
                    if (npcTradePortraitService.TryResolveTradePortraitPath(listCollection, database, id, out tradePortraitPath))
                    {
                        iconKey = tradePortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_SELL_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string sellPortraitPath;
                    if (npcSellPortraitService.TryResolveSellPortraitPath(listCollection, database, id, out sellPortraitPath))
                    {
                        iconKey = sellPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "DROPTABLE_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    string monsterPortraitPath;
                    if (monsterDropPortraitService.TryResolveDropPortraitPath(listCollection, database, id, out monsterPortraitPath))
                    {
                        iconKey = monsterPortraitPath;
                    }
                    else
                    {
                        ItemReferenceOption primaryDropOption;
                        if (TryResolvePrimaryDropTableItemOption(listCollection, targetListIndex, i, database, iconResolutionService, out primaryDropOption))
                        {
                            if (!string.IsNullOrWhiteSpace(primaryDropOption.IconKey))
                            {
                                iconKey = primaryDropOption.IconKey;
                            }
                            if (primaryDropOption.Quality >= 0)
                            {
                                quality = primaryDropOption.Quality;
                            }
                        }
                    }
                }
                else if (string.Equals(normalizedListName, "ITEM_TRADE_PAGE_CONFIG", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption primaryItemOption;
                    if (TryResolvePrimaryTradePageItemOption(listCollection, targetListIndex, i, database, iconResolutionService, out primaryItemOption))
                    {
                        if (!string.IsNullOrWhiteSpace(primaryItemOption.IconKey))
                        {
                            iconKey = primaryItemOption.IconKey;
                        }
                        if (primaryItemOption.Quality >= 0)
                        {
                            quality = primaryItemOption.Quality;
                        }
                    }
                }
                else if (string.Equals(normalizedListName, "ADDON_PACKAGE_CONFIG", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption sourceEquipmentOption;
                    if (addonPackageUsageMap != null && addonPackageUsageMap.TryGetValue(id, out sourceEquipmentOption) && sourceEquipmentOption != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sourceEquipmentOption.IconKey))
                        {
                            iconKey = sourceEquipmentOption.IconKey;
                        }

                        if (sourceEquipmentOption.Quality >= 0)
                        {
                            quality = sourceEquipmentOption.Quality;
                        }
                    }
                }

                options.Add(new ItemReferenceOption
                {
                    ListIndex = targetListIndex,
                    ElementIndex = i,
                    Id = id,
                    Name = name,
                    ListName = listName,
                    IconKey = iconKey,
                    Quality = quality
                });
            }

            return options;
        }

        private Dictionary<int, ItemReferenceOption> BuildAddonPackageUsageMap(
            eListCollection listCollection,
            int targetListIndex,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, ItemReferenceOption> map = new Dictionary<int, ItemReferenceOption>();
            if (listCollection == null || targetListIndex < 0 || targetListIndex >= listCollection.Lists.Length)
            {
                return map;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!IsItemList(listCollection, listIndex))
                {
                    continue;
                }

                string[] fields = listCollection.Lists[listIndex].elementFields;
                if (fields == null || fields.Length == 0)
                {
                    continue;
                }

                List<int> packageFieldIndexes = new List<int>();
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    int resolvedTargetListIndex;
                    if (TryGetTargetListIndex(listCollection, listIndex, -1, fields[fieldIndex], out resolvedTargetListIndex)
                        && resolvedTargetListIndex == targetListIndex)
                    {
                        packageFieldIndexes.Add(fieldIndex);
                    }
                }

                if (packageFieldIndexes.Count == 0)
                {
                    continue;
                }

                int nameIndex = GetNameFieldIndex(listCollection, listIndex);
                int iconIndex = GetIconFieldIndex(listCollection, listIndex);
                int qualityIndex = GetQualityFieldIndex(listCollection, listIndex);
                string sourceListName = listCollection.Lists[listIndex].listName ?? string.Empty;

                for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
                {
                    string sourceName = nameIndex >= 0 ? listCollection.GetValue(listIndex, elementIndex, nameIndex) : string.Empty;
                    string sourceIconKey = ResolveOptionIconKey(listCollection, database, iconResolutionService, listIndex, elementIndex, iconIndex);
                    int sourceQuality = ResolveOptionQuality(listCollection, listIndex, elementIndex, qualityIndex);

                    foreach (int fieldIndex in packageFieldIndexes)
                    {
                        int packageId;
                        if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, fieldIndex), out packageId) || packageId <= 0)
                        {
                            continue;
                        }

                        if (map.ContainsKey(packageId))
                        {
                            continue;
                        }

                        map[packageId] = new ItemReferenceOption
                        {
                            ListIndex = listIndex,
                            ElementIndex = elementIndex,
                            Id = packageId,
                            Name = sourceName,
                            ListName = sourceListName,
                            IconKey = sourceIconKey,
                            Quality = sourceQuality
                        };
                    }
                }
            }

            return map;
        }

        private static string DecodeEquipmentAddonName(
            eListCollection listCollection,
            CacheSave database,
            int addonId)
        {
            if (addonId <= 0 || listCollection == null)
            {
                return string.Empty;
            }

            try
            {
                SessionService session = new SessionService
                {
                    ListCollection = listCollection,
                    Database = database,
                    SkillStr = database != null ? database.skillstr : null,
                    BuffStr = database != null ? database.buff_str : null,
                    AddonsList = database != null ? database.addonslist : null,
                    InstanceList = database != null ? database.InstanceList : null,
                    LocalizationText = database != null ? database.LocalizationText : null
                };

                string decoded = EQUIPMENT_ADDON.GetAddon(session, addonId.ToString());
                if (string.IsNullOrWhiteSpace(decoded))
                {
                    return string.Empty;
                }

                return decoded.Replace("\r", " ").Replace("\n", " / ").Trim();
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool TryResolvePrimaryDropTableItemOption(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            CacheSave database,
            IconResolutionService iconResolutionService,
            out ItemReferenceOption option)
        {
            option = null;
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            int isCategoryFieldIndex = -1;
            List<int> dropFieldIndexes = new List<int>();
            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (string.Equals(fieldName, "is_category", StringComparison.OrdinalIgnoreCase))
                {
                    isCategoryFieldIndex = i;
                }
                else if (fieldName.StartsWith("drops_", StringComparison.OrdinalIgnoreCase)
                    && fieldName.EndsWith("_id_obj", StringComparison.OrdinalIgnoreCase))
                {
                    dropFieldIndexes.Add(i);
                }
            }

            return TryResolvePrimaryDropTableItemOptionInternal(
                listCollection,
                listIndex,
                elementIndex,
                isCategoryFieldIndex,
                dropFieldIndexes,
                database,
                iconResolutionService,
                out option,
                0);
        }

        private bool TryResolvePrimaryDropTableItemOptionInternal(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            int isCategoryFieldIndex,
            List<int> dropFieldIndexes,
            CacheSave database,
            IconResolutionService iconResolutionService,
            out ItemReferenceOption option,
            int depth)
        {
            option = null;
            if (depth > 6 || dropFieldIndexes == null || dropFieldIndexes.Count == 0)
            {
                return false;
            }

            int firstDropId = 0;
            for (int i = 0; i < dropFieldIndexes.Count; i++)
            {
                int candidateId;
                if (int.TryParse(listCollection.GetValue(listIndex, elementIndex, dropFieldIndexes[i]), out candidateId) && candidateId > 0)
                {
                    firstDropId = candidateId;
                    break;
                }
            }

            if (firstDropId <= 0)
            {
                return false;
            }

            int isCategory = 0;
            if (isCategoryFieldIndex >= 0)
            {
                int.TryParse(listCollection.GetValue(listIndex, elementIndex, isCategoryFieldIndex), out isCategory);
            }

            if (isCategory == 1)
            {
                int childRowIndex = FindElementIndexById(listCollection, listIndex, firstDropId);
                if (childRowIndex < 0)
                {
                    return false;
                }

                return TryResolvePrimaryDropTableItemOptionInternal(
                    listCollection,
                    listIndex,
                    childRowIndex,
                    isCategoryFieldIndex,
                    dropFieldIndexes,
                    database,
                    iconResolutionService,
                    out option,
                    depth + 1);
            }

            return TryFindItemOptionByIdAcrossLists(listCollection, firstDropId, database, iconResolutionService, out option);
        }

        private bool TryResolvePrimaryTradePageItemOption(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            CacheSave database,
            IconResolutionService iconResolutionService,
            out ItemReferenceOption option)
        {
            option = null;
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
            {
                if (!IsItemTradePagePrimaryGoodsField(fields[fieldIndex]))
                {
                    continue;
                }

                int itemId;
                if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, fieldIndex), out itemId) || itemId <= 0)
                {
                    continue;
                }

                return TryFindItemOptionByIdAcrossLists(listCollection, itemId, database, iconResolutionService, out option);
            }

            return false;
        }

        private int FindElementIndexById(eListCollection listCollection, int listIndex, int id)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementValues == null
                || id <= 0)
            {
                return -1;
            }

            EnsureElementIndexLookup(listCollection, listIndex);

            Dictionary<int, int> elementIndexesById;
            if (elementIndexByIdByListIndex.TryGetValue(listIndex, out elementIndexesById))
            {
                int elementIndex;
                if (elementIndexesById.TryGetValue(id, out elementIndex))
                {
                    return elementIndex;
                }
            }

            return -1;
        }

        private void EnsureElementIndexLookup(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndexByIdByListIndex.ContainsKey(listIndex))
            {
                return;
            }

            Dictionary<int, int> byId = new Dictionary<int, int>();
            for (int i = 0; i < listCollection.Lists[listIndex].elementValues.Length; i++)
            {
                int candidateId;
                if (int.TryParse(listCollection.GetValue(listIndex, i, 0), out candidateId)
                    && candidateId > 0
                    && !byId.ContainsKey(candidateId))
                {
                    byId[candidateId] = i;
                }
            }

            elementIndexByIdByListIndex[listIndex] = byId;
        }

        public List<ItemReferenceOption> BuildSearchableOptions(eListCollection listCollection, CacheSave database, IconResolutionService iconResolutionService)
        {
            if (listCollection == null || listCollection.Lists == null)
            {
                return new List<ItemReferenceOption>();
            }

            EnsureCacheContext(listCollection, database, iconResolutionService);

            if (searchableOptions != null)
            {
                return searchableOptions;
            }

            searchableOptions = new List<ItemReferenceOption>();
            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!HasPrimaryIdField(listCollection, listIndex))
                {
                    continue;
                }

                searchableOptions.AddRange(BuildOptions(listCollection, listIndex, database, iconResolutionService));
            }

            searchableOptionsById = new Dictionary<int, ItemReferenceOption>();
            searchableOptionsByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < searchableOptions.Count; i++)
            {
                ItemReferenceOption option = searchableOptions[i];
                if (!searchableOptionsById.ContainsKey(option.Id))
                {
                    searchableOptionsById.Add(option.Id, option);
                }
                if (!string.IsNullOrWhiteSpace(option.Name) && !searchableOptionsByName.ContainsKey(option.Name))
                {
                    searchableOptionsByName.Add(option.Name, option);
                }
            }

            return searchableOptions;
        }

        public List<ItemReferenceOption> BuildSearchableItemOptions(eListCollection listCollection, CacheSave database, IconResolutionService iconResolutionService)
        {
            if (listCollection == null || listCollection.Lists == null)
            {
                return new List<ItemReferenceOption>();
            }

            EnsureCacheContext(listCollection, database, iconResolutionService);

            if (searchableItemOptions != null)
            {
                return searchableItemOptions;
            }

            searchableItemOptions = new List<ItemReferenceOption>();
            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!HasPrimaryIdField(listCollection, listIndex) || !IsItemList(listCollection, listIndex))
                {
                    continue;
                }

                searchableItemOptions.AddRange(BuildOptions(listCollection, listIndex, database, iconResolutionService));
            }

            searchableItemOptionsById = new Dictionary<int, ItemReferenceOption>();
            searchableItemOptionsByName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < searchableItemOptions.Count; i++)
            {
                ItemReferenceOption option = searchableItemOptions[i];
                if (!searchableItemOptionsById.ContainsKey(option.Id))
                {
                    searchableItemOptionsById.Add(option.Id, option);
                }
                if (!string.IsNullOrWhiteSpace(option.Name) && !searchableItemOptionsByName.ContainsKey(option.Name))
                {
                    searchableItemOptionsByName.Add(option.Name, option);
                }
            }

            return searchableItemOptions;
        }

        public bool TryFindOptionById(eListCollection listCollection, int targetListIndex, int id, out ItemReferenceOption option)
        {
            return TryFindOptionById(listCollection, targetListIndex, id, null, null, out option);
        }

        public bool TryFindOptionById(eListCollection listCollection, int targetListIndex, int id, CacheSave database, IconResolutionService iconResolutionService, out ItemReferenceOption option)
        {
            option = null;
            if (targetListIndex == TitleDefinitionsTargetIndex)
            {
                return TitleDefinitionCatalog.TryGetOptionById(id, out option);
            }

            BuildOptions(listCollection, targetListIndex, database, iconResolutionService);

            Dictionary<int, ItemReferenceOption> byId;
            if (optionsByIdByListIndex.TryGetValue(targetListIndex, out byId)
                && byId.TryGetValue(id, out option))
            {
                return true;
            }
            return false;
        }

        private bool TryFindOptionByName(eListCollection listCollection, int targetListIndex, string name, out ItemReferenceOption option)
        {
            option = null;
            if (targetListIndex == TitleDefinitionsTargetIndex)
            {
                return TitleDefinitionCatalog.TryGetOptionByName(name, out option);
            }

            BuildOptions(listCollection, targetListIndex);

            Dictionary<string, ItemReferenceOption> byName;
            if (optionsByNameByListIndex.TryGetValue(targetListIndex, out byName)
                && byName.TryGetValue(name, out option))
            {
                return true;
            }
            return false;
        }

        private bool TryFindOptionByIdAcrossLists(eListCollection listCollection, int id, CacheSave database, IconResolutionService iconResolutionService, out ItemReferenceOption option)
        {
            option = null;
            BuildSearchableOptions(listCollection, database, iconResolutionService);
            if (searchableOptionsById != null && searchableOptionsById.TryGetValue(id, out option))
            {
                return true;
            }
            return false;
        }

        private bool TryFindItemOptionByIdAcrossLists(eListCollection listCollection, int id, CacheSave database, IconResolutionService iconResolutionService, out ItemReferenceOption option)
        {
            option = null;
            BuildSearchableItemOptions(listCollection, database, iconResolutionService);
            if (searchableItemOptionsById != null && searchableItemOptionsById.TryGetValue(id, out option))
            {
                return true;
            }
            return false;
        }

        private bool TryFindOptionByNameAcrossLists(eListCollection listCollection, string name, out ItemReferenceOption option)
        {
            option = null;
            BuildSearchableOptions(listCollection, null, null);
            if (searchableOptionsByName != null && searchableOptionsByName.TryGetValue(name, out option))
            {
                return true;
            }
            return false;
        }

        private bool TryFindItemOptionByNameAcrossLists(eListCollection listCollection, string name, out ItemReferenceOption option)
        {
            option = null;
            BuildSearchableItemOptions(listCollection, null, null);
            if (searchableItemOptionsByName != null && searchableItemOptionsByName.TryGetValue(name, out option))
            {
                return true;
            }
            return false;
        }

        private void EnsureCacheContext(eListCollection listCollection, CacheSave database, IconResolutionService iconResolutionService)
        {
            if (!object.ReferenceEquals(cachedListCollection, listCollection)
                || !object.ReferenceEquals(cachedDatabase, database)
                || !object.ReferenceEquals(cachedIconResolutionService, iconResolutionService))
            {
                ClearCache();
                cachedListCollection = listCollection;
                cachedDatabase = database;
                cachedIconResolutionService = iconResolutionService;
            }
        }

        private void IndexOptions(int listIndex, List<ItemReferenceOption> options)
        {
            Dictionary<int, ItemReferenceOption> byId = new Dictionary<int, ItemReferenceOption>();
            Dictionary<string, ItemReferenceOption> byName = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < options.Count; i++)
            {
                ItemReferenceOption option = options[i];
                if (!byId.ContainsKey(option.Id))
                {
                    byId.Add(option.Id, option);
                }
                if (!string.IsNullOrWhiteSpace(option.Name) && !byName.ContainsKey(option.Name))
                {
                    byName.Add(option.Name, option);
                }
            }

            optionsByIdByListIndex[listIndex] = byId;
            optionsByNameByListIndex[listIndex] = byName;
        }

        private bool TryGetRandomGiftBagRewardType(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            string rewardIdFieldName,
            out int rewardType)
        {
            rewardType = 0;
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            string typeFieldName;
            if (!RandomGiftBagRewardTypeCatalog.TryGetRewardTypeFieldNameForIdField(rewardIdFieldName, out typeFieldName))
            {
                return false;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            if (fields == null)
            {
                return false;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.Equals(fields[i], typeFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return int.TryParse(
                    listCollection.GetValue(listIndex, elementIndex, i),
                    out rewardType);
            }

            return false;
        }

        private static bool TryFindListIndexByName(eListCollection listCollection, string targetListName, out int targetListIndex)
        {
            targetListIndex = -1;
            for (int i = 0; i < listCollection.Lists.Length; i++)
            {
                string listName = NormalizeListName(listCollection.Lists[i].listName);
                if (string.Equals(listName, targetListName, StringComparison.OrdinalIgnoreCase))
                {
                    targetListIndex = i;
                    return true;
                }
            }
            return false;
        }

        private static bool TryGetMappedTargetListName(string sourceListName, string fieldName, out string targetListName)
        {
            targetListName = null;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (fieldName.StartsWith("id_addon_prop_", StringComparison.OrdinalIgnoreCase)
                || fieldName.StartsWith("addons_", StringComparison.OrdinalIgnoreCase)
                || fieldName.StartsWith("addon_props_", StringComparison.OrdinalIgnoreCase)
                || fieldName.StartsWith("addon_id_", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("_id_addons_"))
            {
                targetListName = "EQUIPMENT_ADDON";
                return true;
            }

            if (fieldName.Contains("rune_addon_package"))
            {
                targetListName = "RUNE_ADDON_PACKAGE_CONFIG";
                return true;
            }

            if (fieldName.Contains("rune_package"))
            {
                targetListName = "RUNE_PACKAGE_CONFIG";
                return true;
            }

            if (fieldName.Contains("special_status_package"))
            {
                targetListName = "SPECIAL_STATUS_PACKAGE_CONFIG";
                return true;
            }

            if (fieldName.Contains("status_package"))
            {
                targetListName = "STATUS_PACKAGE_EXPRESSIONS_CONFIG";
                return true;
            }

            if (fieldName.Contains("equip_transform_cfg") || fieldName.Contains("equip_transform_config"))
            {
                targetListName = "EQUIP_TRANSFORM_CONFIG";
                return true;
            }

            if ((fieldName.Contains("equip") || fieldName.Contains("equipment"))
                && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "Equipment";
                return true;
            }

            if (fieldName.Contains("fashion_dye_cfg") || fieldName.Contains("fashion_dye_config"))
            {
                targetListName = "FASHION_DYE_CONFIG";
                return true;
            }

            if (fieldName.Contains("color_plan"))
            {
                targetListName = "COLOR_PLAN_CONFIG";
                return true;
            }

            if (fieldName.Contains("quality_config"))
            {
                targetListName = "EQUIPMENT_QUALITY_CONFIG";
                return true;
            }

            if (fieldName.Contains("property_random") || fieldName.Contains("equip_prop"))
            {
                targetListName = "EQUIPMENT_PROPERTY_RANDOM_CONFIG";
                return true;
            }

            if (fieldName.Contains("dynamic_instance"))
            {
                targetListName = "DYNAMIC_INSTANCE_CONFIG";
                return true;
            }

            if (fieldName.Contains("monster") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "MONSTER_ESSENCE";
                return true;
            }

            if ((fieldName.Contains("npc") || fieldName.StartsWith("id_npc_", StringComparison.OrdinalIgnoreCase))
                && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "NPC_ESSENCE";
                return true;
            }

            if (fieldName.Contains("mine") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "MINE_ESSENCE";
                return true;
            }

            if (fieldName.Contains("recipe") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "RECIPE_ESSENCE";
                return true;
            }

            if (!string.Equals(fieldName, "catch_pet_skill_id", StringComparison.OrdinalIgnoreCase)
                && fieldName.Contains("pet_skill") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PET_SKILL_ESSENCE";
                return true;
            }

            if (fieldName.Contains("skillmatter") || fieldName.Contains("skill_matter"))
            {
                targetListName = "SKILLMATTER_ESSENCE";
                return true;
            }

            if (fieldName.Contains("drop") && (fieldName.Contains("table") || fieldName.Contains("droptable")))
            {
                targetListName = "DROPTABLE_ESSENCE";
                return true;
            }

            if (fieldName.Contains("producing_area"))
            {
                targetListName = "TRADE_PORT_DISTANCE_CONFIG";
                return true;
            }

            if (fieldName.Contains("port_service"))
            {
                targetListName = "NPC_TRADE_PORT_SERVICE";
                return true;
            }

            if (fieldName.Contains("title") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "TITLE_PROP_CONFIG";
                return true;
            }

            if (fieldName.Contains("identify") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "idENTIFY_SCROLL_ESSENCE";
                return true;
            }

            if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (IsNumberedIdField(fieldName, "materials_") || IsNumberedIdField(fieldName, "acquired_")))
            {
                targetListName = null;
                return false;
            }

            return false;
        }

        private static bool TryGetNpcServiceTargetListName(string fieldName, out string targetListName)
        {
            targetListName = null;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            switch (fieldName.Trim().ToLowerInvariant())
            {
                case "id_kingdom_activity_service":
                    targetListName = "KINGDOM_ACTIVITY_SERVICE";
                    return true;
                case "id_talk_service":
                    targetListName = "NPC_TALK_SERVICE";
                    return true;
                case "id_sell_service":
                    targetListName = "NPC_SELL_SERVICE";
                    return true;
                case "id_learn_produce":
                    targetListName = "NPC_LEARN_PRODUCE_SERVICE";
                    return true;
                case "id_hotel_service":
                    targetListName = "NPC_HOTEL_SERVICE";
                    return true;
                case "id_buy_service":
                    targetListName = "NPC_BUY_SERVICE";
                    return true;
                case "id_task_out_service":
                    targetListName = "NPC_TASK_OUT_SERVICE";
                    return true;
                case "id_task_in_service":
                    targetListName = "NPC_TASK_IN_SERVICE";
                    return true;
                case "id_task_matter_service":
                    targetListName = "NPC_TASK_MATTER_SERVICE";
                    return true;
                case "id_heal_service":
                    targetListName = "NPC_HEAL_SERVICE";
                    return true;
                case "id_transmit_service":
                    targetListName = "NPC_TRANSMIT_SERVICE";
                    return true;
                case "id_proxy_service":
                    targetListName = "NPC_PROXY_SERVICE";
                    return true;
                case "id_storage_service":
                    targetListName = "NPC_STORAGE_SERVICE";
                    return true;
                case "id_war_towerbuild_service":
                    targetListName = "NPC_WAR_TOWERBUILD_SERVICE";
                    return true;
                case "id_resetprop_service":
                    targetListName = "NPC_RESETPROP_SERVICE";
                    return true;
                case "id_equipbind_service":
                    targetListName = "NPC_EQUIPBIND_SERVICE";
                    return true;
                case "id_equipdestroy_service":
                    targetListName = "NPC_EQUIPDESTROY_SERVICE";
                    return true;
                case "id_equipundestroy_service":
                    targetListName = "NPC_EQUIPUNDESTROY_SERVICE";
                    return true;
                case "id_item_trade_service":
                    targetListName = "ITEM_TRADE_ESSENCE";
                    return true;
                case "id_skill_learn_service":
                    targetListName = "NPC_LEARN_SKILL_SERVICE";
                    return true;
                case "id_news_service":
                    targetListName = "NPC_NEWS_SERVICE";
                    return true;
                case "id_port_service1":
                case "id_port_service2":
                case "id_port_service3":
                    targetListName = "NPC_TRADE_PORT_SERVICE";
                    return true;
                case "instance_service":
                    targetListName = "INSTANCE_CONFIG";
                    return true;
                case "id_wedding_parade_service":
                    targetListName = "WEDDING_PARADE_SERVICE";
                    return true;
                case "id_pet_unbind_service":
                    targetListName = "PET_UNBIND_SERVICE";
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsGenericItemReferenceField(string sourceListName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (fieldName == "id" || fieldName.EndsWith("_num", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_count", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_time", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_level", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_probability", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if ((fieldName.Contains("item") && HasIdToken(fieldName))
                || fieldName.EndsWith("_id_obj", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_tool_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_ticket_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_book_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_scroll_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_stone_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_matter_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_result_id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsNumberedIdField(fieldName, "gift_")
                || IsNumberedIdField(fieldName, "reward_")
                || IsNumberedIdField(fieldName, "materials_")
                || IsNumberedIdField(fieldName, "acquired_")
                || IsNumberedIdField(fieldName, "decompose_main_result_")
                || IsNumberedIdField(fieldName, "decompose_sub_result_")
                || IsNumberedIdField(fieldName, "stone_")
                || IsNumberedIdField(fieldName, "tools_")
                || IsNumberedIdField(fieldName, "tool_"))
            {
                return true;
            }

            if (string.Equals(sourceListName, "DROPTABLE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && fieldName.StartsWith("drops_", StringComparison.OrdinalIgnoreCase)
                && fieldName.EndsWith("_id_obj", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (IsNumberedIdField(fieldName, "materials_") || IsNumberedIdField(fieldName, "acquired_")))
            {
                return true;
            }

            return false;
        }

        private static bool IsItemTradePageField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.StartsWith("pages_", StringComparison.OrdinalIgnoreCase)
                && fieldName.IndexOf("id_page", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsItemTradePageItemField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return IsItemTradePagePrimaryGoodsField(normalized)
                || normalized.IndexOf("_2_item_required_1_value_1", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.IndexOf("_4_item_required_2_value_1", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsNpcSellGoodsField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.StartsWith("pages_", StringComparison.OrdinalIgnoreCase)
                && fieldName.IndexOf("_id_goods_", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsItemTradePagePrimaryGoodsField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.StartsWith("goods_", StringComparison.OrdinalIgnoreCase)
                && fieldName.IndexOf("_1_id_goods", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsDropTableCategoryRow(eListCollection listCollection, int listIndex, int elementIndex)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || elementIndex < 0
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (!string.Equals(listCollection.Lists[listIndex].elementFields[i], "is_category", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int isCategory;
                return int.TryParse(listCollection.GetValue(listIndex, elementIndex, i), out isCategory) && isCategory == 1;
            }

            return false;
        }

        private static bool HasIdToken(string fieldName)
        {
            return fieldName.StartsWith("id_", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.Contains("_id_");
        }

        private static bool IsNumberedIdField(string fieldName, string prefix)
        {
            return fieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase) || fieldName.Contains("_id_"));
        }

        private static int GetNameFieldIndex(eListCollection listCollection, int listIndex)
        {
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int GetIconFieldIndex(eListCollection listCollection, int listIndex)
        {
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                string fieldName = listCollection.Lists[listIndex].elementFields[i];
                if (string.Equals(fieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "file_icon1", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int GetQualityFieldIndex(eListCollection listCollection, int listIndex)
        {
            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                if (string.Equals(listCollection.Lists[listIndex].elementFields[i], "item_quality", StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool HasPrimaryIdField(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }
            if (listCollection.Lists[listIndex].elementFields == null || listCollection.Lists[listIndex].elementFields.Length == 0)
            {
                return false;
            }

            return string.Equals(listCollection.Lists[listIndex].elementFields[0], "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listCollection.Lists[listIndex].elementFields[0], "ID", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsItemList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            string listName = NormalizeListName(listCollection.Lists[listIndex].listName);
            if (string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            if (fields == null)
            {
                return false;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], "item_quality", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ResolveOptionIconKey(eListCollection listCollection, CacheSave database, IconResolutionService iconResolutionService, int listIndex, int elementIndex, int iconIndex)
        {
            if (iconIndex < 0 || iconResolutionService == null)
            {
                return string.Empty;
            }

            string rawIcon = listCollection.GetValue(listIndex, elementIndex, iconIndex);
            return iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, rawIcon);
        }

        private static int ResolveOptionQuality(eListCollection listCollection, int listIndex, int elementIndex, int qualityIndex)
        {
            int quality;
            if (qualityIndex >= 0 && int.TryParse(listCollection.GetValue(listIndex, elementIndex, qualityIndex), out quality))
            {
                return quality;
            }
            return -1;
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new string[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }

        private static List<ItemReferenceOption> CloneOptions(List<ItemReferenceOption> source)
        {
            List<ItemReferenceOption> clone = new List<ItemReferenceOption>();
            if (source == null)
            {
                return clone;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ItemReferenceOption option = source[i];
                if (option == null)
                {
                    continue;
                }

                clone.Add(new ItemReferenceOption
                {
                    ListIndex = option.ListIndex,
                    ElementIndex = option.ElementIndex,
                    Id = option.Id,
                    Name = option.Name,
                    ListName = option.ListName,
                    IconKey = option.IconKey,
                    Quality = option.Quality,
                    Description = option.Description,
                    SecondaryText = option.SecondaryText,
                    AccentHex = option.AccentHex,
                    Kind = option.Kind
                });
            }

            return clone;
        }
    }
}

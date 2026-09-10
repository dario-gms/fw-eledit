using System;
using System.Collections.Generic;
using System.Drawing;
using eELedit;

namespace FWEledit
{
    public sealed class ItemReferenceService
    {
        private const int AllListsTargetIndex = -1;
        private const int ItemListsTargetIndex = -2;
        private const int TasksTargetIndex = -4;
        private const int ConversationTargetIndex = -5;
        private const int TitleDefinitionsTargetIndex = TitleDefinitionCatalog.TargetListIndex;
        private readonly NpcTradePortraitService npcTradePortraitService = new NpcTradePortraitService();
        private readonly NpcTalkPortraitService npcTalkPortraitService = new NpcTalkPortraitService();
        private readonly NpcSellPortraitService npcSellPortraitService = new NpcSellPortraitService();
        private readonly NpcTransmitPortraitService npcTransmitPortraitService = new NpcTransmitPortraitService();
        private readonly NpcHotelPortraitService npcHotelPortraitService = new NpcHotelPortraitService();
        private readonly NpcLearnProducePortraitService npcLearnProducePortraitService = new NpcLearnProducePortraitService();
        private readonly MonsterDropPortraitService monsterDropPortraitService = new MonsterDropPortraitService();
        private readonly CreaturePortraitIconService creaturePortraitIconService = new CreaturePortraitIconService();

        private eListCollection cachedListCollection;
        private CacheSave cachedDatabase;
        private IconResolutionService cachedIconResolutionService;
        private readonly Dictionary<int, List<ItemReferenceOption>> optionsByListIndex = new Dictionary<int, List<ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<int, ItemReferenceOption>> optionsByIdByListIndex = new Dictionary<int, Dictionary<int, ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<string, ItemReferenceOption>> optionsByNameByListIndex = new Dictionary<int, Dictionary<string, ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<int, int>> elementIndexByIdByListIndex = new Dictionary<int, Dictionary<int, int>>();
        private readonly Dictionary<string, Dictionary<int, string>> inheritedTypeIconKeyCache = new Dictionary<string, Dictionary<int, string>>(StringComparer.OrdinalIgnoreCase);
        private List<ItemReferenceOption> searchableOptions;
        private Dictionary<int, ItemReferenceOption> searchableOptionsById;
        private Dictionary<string, ItemReferenceOption> searchableOptionsByName;
        private List<ItemReferenceOption> searchableItemOptions;
        private Dictionary<int, ItemReferenceOption> searchableItemOptionsById;
        private Dictionary<string, ItemReferenceOption> searchableItemOptionsByName;
        private readonly Dictionary<string, ItemReferenceOption> preferredOptionsByFieldAndId = new Dictionary<string, ItemReferenceOption>(StringComparer.OrdinalIgnoreCase);
        private int cachedTaskItemsRevision = -1;

        public void ClearCache()
        {
            cachedListCollection = null;
            cachedDatabase = null;
            cachedIconResolutionService = null;
            optionsByListIndex.Clear();
            optionsByIdByListIndex.Clear();
            optionsByNameByListIndex.Clear();
            elementIndexByIdByListIndex.Clear();
            inheritedTypeIconKeyCache.Clear();
            searchableOptions = null;
            searchableOptionsById = null;
            searchableOptionsByName = null;
            searchableItemOptions = null;
            searchableItemOptionsById = null;
            searchableItemOptionsByName = null;
            cachedTaskItemsRevision = -1;
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
                if (pair.Key == TasksTargetIndex)
                {
                    continue;
                }

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

        public bool IsTaskTargetIndex(int targetListIndex)
        {
            return targetListIndex == TasksTargetIndex;
        }

        public bool IsConversationTargetIndex(int targetListIndex)
        {
            return targetListIndex == ConversationTargetIndex;
        }

        public bool IsItemBearingList(eListCollection listCollection, int listIndex)
        {
            return ItemListCatalog.IsItemList(listCollection, listIndex);
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

            if (SkillReferenceCatalog.IsSkillMatterTypeField(listCollection, listIndex, name))
            {
                return false;
            }

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
            else if (string.Equals(name, "id_recipe", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "RECIPE_ESSENCE";
            }
            else if (string.Equals(name, "id_level_exp", StringComparison.OrdinalIgnoreCase)
                && !IsPetBedgeEssenceListName(sourceListName))
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
            else if (string.Equals(sourceListName, "MINE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && IsMineToolItemReferenceField(name))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "GM_GENERATOR_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(name, "id_object", StringComparison.OrdinalIgnoreCase))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (IsTaskReferenceField(sourceListName, name))
            {
                targetListIndex = TasksTargetIndex;
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
            else if (IsMergeRecipeItemReferenceField(sourceListName, name))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && IsNumberedIdField(name, "materials_"))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && IsNumberedIdField(name, "acquired_"))
            {
                targetListName = "PRODUCE_TYPE_ESSENCE";
            }
            else if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(name, "produce_type", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PRODUCE_TYPE_ESSENCE";
            }
            else if (string.Equals(sourceListName, "NPC_TALK_SERVICE", StringComparison.OrdinalIgnoreCase)
                && FieldNameEquals(name, "id_dialog"))
            {
                targetListIndex = ConversationTargetIndex;
                return true;
            }
            else if (IsNpcLearnProduceSkillReferenceField(sourceListName, name))
            {
                targetListName = "PRODUCE_TYPE_ESSENCE";
            }
            else if (string.Equals(sourceListName, "NPC_SELL_SERVICE", StringComparison.OrdinalIgnoreCase)
                && IsNpcSellGoodsField(name))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "NPC_RESETPROP_SERVICE", StringComparison.OrdinalIgnoreCase)
                && IsNpcResetpropRequiredItemField(name))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (string.Equals(sourceListName, "NPC_TRANSMIT_SERVICE", StringComparison.OrdinalIgnoreCase)
                && IsNpcTransmitTargetField(name))
            {
                targetListName = "NPC_TRANSMIT_SERVICE";
            }
            else if ((name.StartsWith("extend_identify_attr_tool_", StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith("_tool_id", StringComparison.OrdinalIgnoreCase))
                || string.Equals(name, "reidentify_extend_identify_attr_tool_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListIndex = ItemListsTargetIndex;
                return true;
            }
            else if (TryGetConventionalTypeTargetListIndex(listCollection, sourceListName, name, out targetListIndex))
            {
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

            if (targetListIndex == TasksTargetIndex && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
            }
            if (targetListIndex == TasksTargetIndex)
            {
                return rawValue ?? string.Empty;
            }
            if (targetListIndex == ConversationTargetIndex && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
            }
            if (targetListIndex == ConversationTargetIndex)
            {
                return rawValue ?? string.Empty;
            }

            if (targetListIndex >= 0 && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
            }
            if (IsStrictTargetReferenceField(listCollection, listIndex, fieldName))
            {
                return rawValue ?? string.Empty;
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
                option = ApplySourceMonsterIcon(listCollection, listIndex, elementIndex, fieldName, option, database, iconResolutionService);
                return true;
            }

            if (targetListIndex == TitleDefinitionsTargetIndex && TitleDefinitionCatalog.TryGetOptionById(id, out option))
            {
                return true;
            }

            if (targetListIndex == TasksTargetIndex && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return true;
            }
            if (targetListIndex == TasksTargetIndex)
            {
                return false;
            }
            if (targetListIndex == ConversationTargetIndex && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return true;
            }
            if (targetListIndex == ConversationTargetIndex)
            {
                return false;
            }

            if (targetListIndex >= 0 && TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                option = ApplySourceMonsterIcon(listCollection, listIndex, elementIndex, fieldName, option, database, iconResolutionService);
                return true;
            }
            if (IsStrictTargetReferenceField(listCollection, listIndex, fieldName))
            {
                return false;
            }

            if (targetListIndex == ItemListsTargetIndex)
            {
                bool foundItem = TryFindItemOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option);
                if (foundItem)
                {
                    option = ApplySourceMonsterIcon(listCollection, listIndex, elementIndex, fieldName, option, database, iconResolutionService);
                }
                return foundItem;
            }

            bool found = TryFindOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option);
            if (found)
            {
                option = ApplySourceMonsterIcon(listCollection, listIndex, elementIndex, fieldName, option, database, iconResolutionService);
            }
            return found;
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

            if (targetListIndex == TasksTargetIndex && TryFindOptionByName(listCollection, targetListIndex, value.Trim(), out option))
            {
                return option.Id.ToString();
            }
            if (targetListIndex == ConversationTargetIndex && TryFindOptionByName(listCollection, targetListIndex, value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            if (targetListIndex >= 0 && TryFindOptionByName(listCollection, targetListIndex, value.Trim(), out option))
            {
                return option.Id.ToString();
            }
            if (IsStrictTargetReferenceField(listCollection, listIndex, fieldName))
            {
                return value;
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
            EnsureCacheContext(listCollection, database, iconResolutionService);

            if (targetListIndex == TasksTargetIndex)
            {
                return BuildTaskOptions(database);
            }
            if (targetListIndex == ConversationTargetIndex)
            {
                return BuildConversationOptions(listCollection);
            }

            if (targetListIndex == TitleDefinitionsTargetIndex)
            {
                return BuildTitleDefinitionOptions();
            }

            if (listCollection == null || targetListIndex < 0 || targetListIndex >= listCollection.Lists.Length)
            {
                return new List<ItemReferenceOption>();
            }

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

        public List<ItemReferenceOption> BuildTaskOptions(CacheSave database)
        {
            EnsureCacheContext(cachedListCollection, database, cachedIconResolutionService);

            List<ItemReferenceOption> options;
            if (optionsByListIndex.TryGetValue(TasksTargetIndex, out options))
            {
                return options;
            }

            options = BuildTaskOptionsUncached(database);
            optionsByListIndex[TasksTargetIndex] = options;
            IndexOptions(TasksTargetIndex, options);
            return options;
        }

        public List<ItemReferenceOption> BuildConversationOptions(eListCollection listCollection)
        {
            EnsureCacheContext(listCollection, cachedDatabase, cachedIconResolutionService);

            List<ItemReferenceOption> options;
            if (optionsByListIndex.TryGetValue(ConversationTargetIndex, out options))
            {
                return options;
            }

            options = BuildConversationOptionsUncached(listCollection);
            optionsByListIndex[ConversationTargetIndex] = options;
            IndexOptions(ConversationTargetIndex, options);
            return options;
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
            Dictionary<int, ItemReferenceOption> suiteEquipmentUsageMap = string.Equals(normalizedListName, "SUITE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                ? BuildSuiteEquipmentUsageMap(listCollection, targetListIndex, database, iconResolutionService)
                : null;
            Dictionary<int, ItemReferenceOption> produceTypeUsageMap = string.Equals(normalizedListName, "PRODUCE_TYPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                ? BuildProduceTypeUsageMap(listCollection, database, iconResolutionService)
                : null;
            Dictionary<int, ItemReferenceOption> recipeUsageMap = string.Equals(normalizedListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                ? BuildRecipeUsageMap(listCollection, targetListIndex, database, iconResolutionService)
                : null;
            Dictionary<int, ItemReferenceOption> recipeTypeUsageMap =
                string.Equals(normalizedListName, "RECIPE_MAJOR_TYPE", StringComparison.OrdinalIgnoreCase)
                    ? BuildRecipeTypeUsageMap(listCollection, "id_major_type", database, iconResolutionService)
                    : string.Equals(normalizedListName, "RECIPE_SUB_TYPE", StringComparison.OrdinalIgnoreCase)
                        ? BuildRecipeTypeUsageMap(listCollection, "id_sub_type", database, iconResolutionService)
                        : null;
            Dictionary<int, ItemReferenceOption> npcResetpropUsageMap = string.Equals(normalizedListName, "NPC_RESETPROP_SERVICE", StringComparison.OrdinalIgnoreCase)
                ? BuildNpcResetpropUsageMap(listCollection, targetListIndex, database, iconResolutionService)
                : null;
            string inheritedTypeSourceListName;
            string inheritedTypeFieldName;
            Dictionary<int, string> inheritedTypeIconKeyById = !string.Equals(normalizedListName, "RECIPE_MAJOR_TYPE", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(normalizedListName, "RECIPE_SUB_TYPE", StringComparison.OrdinalIgnoreCase)
                && TryGetInheritedTypeIconSource(normalizedListName, out inheritedTypeSourceListName, out inheritedTypeFieldName)
                ? BuildInheritedTypeIconKeyMap(listCollection, database, iconResolutionService, inheritedTypeSourceListName, inheritedTypeFieldName)
                : null;
            for (int i = 0; i < listCollection.Lists[targetListIndex].elementValues.Length; i++)
            {
                int id;
                if (!int.TryParse(listCollection.GetValue(targetListIndex, i, 0), out id))
                {
                    continue;
                }

                string rawName = ResolveOptionName(listCollection, targetListIndex, i, nameIndex, normalizedListName);
                Color? nameForeColor;
                string name = NormalizeOptionDisplayName(rawName, out nameForeColor);
                string iconKey = ResolveSourceElementIconKey(listCollection, database, iconResolutionService, targetListIndex, i, iconIndex);
                if (string.IsNullOrWhiteSpace(iconKey)
                    && inheritedTypeIconKeyById != null
                    && inheritedTypeIconKeyById.TryGetValue(id, out string inheritedIconKey))
                {
                    iconKey = inheritedIconKey;
                }
                int quality = ResolveOptionQuality(listCollection, targetListIndex, i, qualityIndex);
                if (string.Equals(normalizedListName, "EQUIPMENT_ADDON", StringComparison.OrdinalIgnoreCase))
                {
                    string decodedName = DecodeEquipmentAddonName(listCollection, database, id);
                    if (!string.IsNullOrWhiteSpace(decodedName))
                    {
                        rawName = decodedName;
                        name = NormalizeOptionDisplayName(rawName, out nameForeColor);
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
                else if (string.Equals(normalizedListName, "NPC_TALK_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string talkPortraitPath;
                    if (npcTalkPortraitService.TryResolveTalkPortraitPath(listCollection, database, id, out talkPortraitPath))
                    {
                        iconKey = talkPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_TRANSMIT_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string transmitPortraitPath;
                    if (npcTransmitPortraitService.TryResolveTransmitPortraitPath(listCollection, database, id, out transmitPortraitPath))
                    {
                        iconKey = transmitPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_HOTEL_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string hotelPortraitPath;
                    if (npcHotelPortraitService.TryResolveHotelPortraitPath(listCollection, database, id, out hotelPortraitPath))
                    {
                        iconKey = hotelPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_LEARN_PRODUCE_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string learnProducePortraitPath;
                    if (npcLearnProducePortraitService.TryResolveLearnProducePortraitPath(listCollection, database, id, out learnProducePortraitPath))
                    {
                        iconKey = learnProducePortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_TASK_IN_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string taskInPortraitPath;
                    if (TryResolveNpcServicePortraitPathByField(listCollection, database, id, out taskInPortraitPath, "id_task_in_service", "Task In"))
                    {
                        iconKey = taskInPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_TASK_OUT_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string taskOutPortraitPath;
                    if (TryResolveNpcServicePortraitPathByField(listCollection, database, id, out taskOutPortraitPath, "id_task_out_service", "Task Out"))
                    {
                        iconKey = taskOutPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_TASK_MATTER_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string taskMatterPortraitPath;
                    if (TryResolveNpcServicePortraitPathByField(listCollection, database, id, out taskMatterPortraitPath, "id_task_matter_service", "Task Matter"))
                    {
                        iconKey = taskMatterPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_HEAL_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string healPortraitPath;
                    if (TryResolveNpcServicePortraitPathByField(listCollection, database, id, out healPortraitPath, "id_heal_service", "Heal Service"))
                    {
                        iconKey = healPortraitPath;
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_STORAGE_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    string storagePortraitPath;
                    if (TryResolveNpcServicePortraitPathByField(listCollection, database, id, out storagePortraitPath, "id_storage_service", "Storage Service"))
                    {
                        iconKey = storagePortraitPath;
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
                else if (string.Equals(normalizedListName, "SUITE_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption sourceEquipmentOption;
                    if (suiteEquipmentUsageMap != null && suiteEquipmentUsageMap.TryGetValue(id, out sourceEquipmentOption) && sourceEquipmentOption != null)
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
                else if (string.Equals(normalizedListName, "PRODUCE_TYPE_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption sourceItemOption;
                    if (produceTypeUsageMap != null && produceTypeUsageMap.TryGetValue(id, out sourceItemOption) && sourceItemOption != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sourceItemOption.IconKey))
                        {
                            iconKey = sourceItemOption.IconKey;
                        }

                        if (sourceItemOption.Quality >= 0)
                        {
                            quality = sourceItemOption.Quality;
                        }
                    }
                }
                else if (string.Equals(normalizedListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption sourceItemOption;
                    if (recipeUsageMap != null && recipeUsageMap.TryGetValue(id, out sourceItemOption) && sourceItemOption != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sourceItemOption.IconKey))
                        {
                            iconKey = sourceItemOption.IconKey;
                        }

                        if (sourceItemOption.Quality >= 0)
                        {
                            quality = sourceItemOption.Quality;
                        }
                    }
                }
                else if (string.Equals(normalizedListName, "RECIPE_MAJOR_TYPE", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalizedListName, "RECIPE_SUB_TYPE", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption sourceItemOption;
                    if (recipeTypeUsageMap != null && recipeTypeUsageMap.TryGetValue(id, out sourceItemOption) && sourceItemOption != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sourceItemOption.IconKey))
                        {
                            iconKey = sourceItemOption.IconKey;
                        }

                        if (sourceItemOption.Quality >= 0)
                        {
                            quality = sourceItemOption.Quality;
                        }
                    }
                }
                else if (string.Equals(normalizedListName, "NPC_RESETPROP_SERVICE", StringComparison.OrdinalIgnoreCase))
                {
                    ItemReferenceOption sourceItemOption;
                    if (npcResetpropUsageMap != null && npcResetpropUsageMap.TryGetValue(id, out sourceItemOption) && sourceItemOption != null)
                    {
                        if (!string.IsNullOrWhiteSpace(sourceItemOption.IconKey))
                        {
                            iconKey = sourceItemOption.IconKey;
                        }

                        if (sourceItemOption.Quality >= 0)
                        {
                            quality = sourceItemOption.Quality;
                        }
                    }
                }

                string accentHex = string.Empty;
                string secondaryText = string.Empty;
                if (string.Equals(normalizedListName, "COLOR_PLAN_CONFIG", StringComparison.OrdinalIgnoreCase))
                {
                    accentHex = BuildColorPlanAccentHex(listCollection, targetListIndex, i);
                    if (!string.IsNullOrWhiteSpace(accentHex))
                    {
                        secondaryText = "#" + accentHex;
                    }
                }

                options.Add(new ItemReferenceOption
                {
                    ListIndex = targetListIndex,
                    ElementIndex = i,
                    Id = id,
                    Name = name,
                    RawName = rawName,
                    NameForeColor = nameForeColor,
                    ListName = listName,
                    IconKey = iconKey,
                    Quality = quality,
                    AccentHex = accentHex,
                    SecondaryText = secondaryText
                });
            }

            return options;
        }

        private static string BuildColorPlanAccentHex(eListCollection listCollection, int listIndex, int elementIndex)
        {
            int redMinField = FindFieldIndex(listCollection, listIndex, "red_min");
            int redMaxField = FindFieldIndex(listCollection, listIndex, "red_max");
            int greenMinField = FindFieldIndex(listCollection, listIndex, "green_min");
            int greenMaxField = FindFieldIndex(listCollection, listIndex, "green_max");
            int blueMinField = FindFieldIndex(listCollection, listIndex, "blue_min");
            int blueMaxField = FindFieldIndex(listCollection, listIndex, "blue_max");
            if (redMinField < 0
                || redMaxField < 0
                || greenMinField < 0
                || greenMaxField < 0
                || blueMinField < 0
                || blueMaxField < 0)
            {
                return string.Empty;
            }

            int redMin = GetColorPlanChannelValue(listCollection, listIndex, elementIndex, redMinField);
            int redMax = GetColorPlanChannelValue(listCollection, listIndex, elementIndex, redMaxField);
            int greenMin = GetColorPlanChannelValue(listCollection, listIndex, elementIndex, greenMinField);
            int greenMax = GetColorPlanChannelValue(listCollection, listIndex, elementIndex, greenMaxField);
            int blueMin = GetColorPlanChannelValue(listCollection, listIndex, elementIndex, blueMinField);
            int blueMax = GetColorPlanChannelValue(listCollection, listIndex, elementIndex, blueMaxField);

            int red = (redMin + redMax) / 2;
            int green = (greenMin + greenMax) / 2;
            int blue = (blueMin + blueMax) / 2;
            return red.ToString("X2") + green.ToString("X2") + blue.ToString("X2");
        }

        private static int GetColorPlanChannelValue(eListCollection listCollection, int listIndex, int elementIndex, int fieldIndex)
        {
            if (fieldIndex < 0)
            {
                return 0;
            }

            int value;
            if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, fieldIndex), out value))
            {
                return 0;
            }

            if (value < 0)
            {
                return 0;
            }
            if (value > 255)
            {
                return 255;
            }
            return value;
        }

        private static string NormalizeOptionDisplayName(string rawName, out Color? nameForeColor)
        {
            nameForeColor = null;
            Color parsedColor;
            string visibleName;
            if (FwTextColorService.TryParseLeadingColor(rawName, out parsedColor, out visibleName))
            {
                nameForeColor = parsedColor;
                return visibleName;
            }

            return rawName ?? string.Empty;
        }

        private static int FindFieldIndex(eListCollection listCollection, int listIndex, string fieldName)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || string.IsNullOrWhiteSpace(fieldName))
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static List<ItemReferenceOption> BuildTaskOptionsUncached(CacheSave database)
        {
            List<ItemReferenceOption> options = new List<ItemReferenceOption>();
            if (database == null || database.task_items == null || database.task_items.Count == 0)
            {
                return options;
            }

            foreach (KeyValuePair<int, ItemDupe> pair in database.task_items)
            {
                ItemDupe task = pair.Value;
                if (task == null || pair.Key <= 0)
                {
                    continue;
                }

                string secondaryText = BuildTaskSecondaryText(database, task);
                string description = BuildTaskDescription(database, task);

                options.Add(new ItemReferenceOption
                {
                    ListIndex = TasksTargetIndex,
                    ElementIndex = task.index,
                    Id = pair.Key,
                    Name = string.IsNullOrWhiteSpace(task.name) ? "Task " + pair.Key.ToString() : task.name,
                    ListName = "Tasks",
                    IconKey = string.Empty,
                    Quality = -1,
                    SecondaryText = secondaryText,
                    Description = description,
                    Kind = task.depth > 0 ? "Subtask" : "Task"
                });
            }

            return options;
        }

        private static List<ItemReferenceOption> BuildConversationOptionsUncached(eListCollection listCollection)
        {
            List<ItemReferenceOption> options = new List<ItemReferenceOption>();
            eListConversation conversation = TryLoadConversation(listCollection);
            if (conversation == null || conversation.talk_procs == null)
            {
                return options;
            }

            for (int i = 0; i < conversation.talk_proc_count && i < conversation.talk_procs.Length; i++)
            {
                talk_proc talk = conversation.talk_procs[i];
                if (talk == null || talk.id_talk <= 0)
                {
                    continue;
                }

                string name = CleanConversationText(talk.GetText());
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = "Dialog " + talk.id_talk.ToString();
                }

                options.Add(new ItemReferenceOption
                {
                    ListIndex = ConversationTargetIndex,
                    ElementIndex = i,
                    Id = talk.id_talk,
                    Name = name,
                    ListName = "Dialogs",
                    IconKey = string.Empty,
                    Quality = -1,
                    Kind = "Dialog"
                });
            }

            return options;
        }

        private static eListConversation TryLoadConversation(eListCollection listCollection)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listCollection.ConversationListIndex < 0
                || listCollection.ConversationListIndex >= listCollection.Lists.Length
                || listCollection.Lists[listCollection.ConversationListIndex] == null
                || listCollection.Lists[listCollection.ConversationListIndex].elementValues == null
                || listCollection.Lists[listCollection.ConversationListIndex].elementValues.Length == 0
                || listCollection.Lists[listCollection.ConversationListIndex].elementValues[0] == null
                || listCollection.Lists[listCollection.ConversationListIndex].elementValues[0].Length == 0)
            {
                return null;
            }

            byte[] raw = listCollection.Lists[listCollection.ConversationListIndex].elementValues[0][0] as byte[];
            if (raw == null || raw.Length == 0)
            {
                return null;
            }

            try
            {
                return new eListConversation(raw);
            }
            catch
            {
                return null;
            }
        }

        private static string CleanConversationText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Replace("\0", string.Empty).Trim();
        }

        private static bool FieldNameEquals(string fieldName, string expected)
        {
            return string.Equals(NormalizeFieldKey(fieldName), NormalizeFieldKey(expected), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeFieldKey(string fieldName)
        {
            return (fieldName ?? string.Empty).Trim().Replace(" ", "_");
        }

        private static string BuildTaskSecondaryText(CacheSave database, ItemDupe task)
        {
            if (task == null)
            {
                return "Task";
            }

            if (task.depth <= 0)
            {
                return task.childCount > 0 ? "Task group" : "Task";
            }

            ItemDupe parentTask;
            string parentName = TryGetTaskById(database, task.parentId, out parentTask) && parentTask != null && !string.IsNullOrWhiteSpace(parentTask.name)
                ? parentTask.name
                : "Task " + task.parentId.ToString();
            return "Subtask of " + parentName + " [" + task.parentId.ToString() + "]";
        }

        private static string BuildTaskDescription(CacheSave database, ItemDupe task)
        {
            if (task == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            if (task.depth > 0)
            {
                ItemDupe parentTask;
                string parentName = TryGetTaskById(database, task.parentId, out parentTask) && parentTask != null && !string.IsNullOrWhiteSpace(parentTask.name)
                    ? parentTask.name
                    : "Task " + task.parentId.ToString();
                parts.Add("Parent: " + parentName + " [" + task.parentId.ToString() + "]");
            }

            if (task.rootId > 0 && task.rootId != task.itemId)
            {
                ItemDupe rootTask;
                string rootName = TryGetTaskById(database, task.rootId, out rootTask) && rootTask != null && !string.IsNullOrWhiteSpace(rootTask.name)
                    ? rootTask.name
                    : "Task " + task.rootId.ToString();
                parts.Add("Root: " + rootName + " [" + task.rootId.ToString() + "]");
            }

            if (task.childCount > 0)
            {
                parts.Add("Contains " + task.childCount.ToString() + " subtasks");
            }

            return string.Join(Environment.NewLine, parts.ToArray());
        }

        private static bool TryGetTaskById(CacheSave database, int id, out ItemDupe task)
        {
            task = null;
            return database != null
                && database.task_items != null
                && id > 0
                && database.task_items.ContainsKey(id)
                && (task = database.task_items[id]) != null;
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
                if (!ItemListCatalog.IsItemList(listCollection, listIndex))
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

                        Color? sourceNameForeColor;
                        string sourceDisplayName = NormalizeOptionDisplayName(sourceName, out sourceNameForeColor);
                        map[packageId] = new ItemReferenceOption
                        {
                            ListIndex = listIndex,
                            ElementIndex = elementIndex,
                            Id = packageId,
                            Name = sourceDisplayName,
                            RawName = sourceName,
                            NameForeColor = sourceNameForeColor,
                            ListName = sourceListName,
                            IconKey = sourceIconKey,
                            Quality = sourceQuality
                        };
                    }
                }
            }

            return map;
        }

        private Dictionary<int, ItemReferenceOption> BuildSuiteEquipmentUsageMap(
            eListCollection listCollection,
            int targetListIndex,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, ItemReferenceOption> map = new Dictionary<int, ItemReferenceOption>();
            if (listCollection == null
                || targetListIndex < 0
                || targetListIndex >= listCollection.Lists.Length
                || listCollection.Lists[targetListIndex] == null
                || listCollection.Lists[targetListIndex].elementFields == null
                || listCollection.Lists[targetListIndex].elementValues == null)
            {
                return map;
            }

            string[] fields = listCollection.Lists[targetListIndex].elementFields;
            List<int> equipmentFieldIndexes = GetSuiteEquipmentFieldIndexes(fields);
            if (equipmentFieldIndexes.Count == 0)
            {
                return map;
            }

            for (int elementIndex = 0; elementIndex < listCollection.Lists[targetListIndex].elementValues.Length; elementIndex++)
            {
                int suiteId;
                if (!int.TryParse(listCollection.GetValue(targetListIndex, elementIndex, 0), out suiteId) || suiteId <= 0 || map.ContainsKey(suiteId))
                {
                    continue;
                }

                for (int i = 0; i < equipmentFieldIndexes.Count; i++)
                {
                    int equipmentId;
                    if (!int.TryParse(listCollection.GetValue(targetListIndex, elementIndex, equipmentFieldIndexes[i]), out equipmentId) || equipmentId <= 0)
                    {
                        continue;
                    }

                    ItemReferenceOption equipmentOption;
                    if (TryBuildItemOptionByIdUncached(listCollection, equipmentId, database, iconResolutionService, out equipmentOption))
                    {
                        map[suiteId] = equipmentOption;
                        break;
                    }
                }
            }

            return map;
        }

        private Dictionary<int, ItemReferenceOption> BuildProduceTypeUsageMap(
            eListCollection listCollection,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, ItemReferenceOption> map = new Dictionary<int, ItemReferenceOption>();
            int recipeListIndex;
            if (!TryFindListIndexByName(listCollection, "RECIPE_ESSENCE", out recipeListIndex)
                || listCollection.Lists[recipeListIndex] == null
                || listCollection.Lists[recipeListIndex].elementFields == null
                || listCollection.Lists[recipeListIndex].elementValues == null)
            {
                return map;
            }

            string[] fields = listCollection.Lists[recipeListIndex].elementFields;
            int produceTypeFieldIndex = GetFieldIndex(fields, "produce_type");
            List<int> productFieldIndexes = GetRecipeProducedItemFieldIndexes(fields);
            if (produceTypeFieldIndex < 0 || productFieldIndexes.Count == 0)
            {
                return map;
            }

            for (int elementIndex = 0; elementIndex < listCollection.Lists[recipeListIndex].elementValues.Length; elementIndex++)
            {
                int produceTypeId;
                if (!int.TryParse(listCollection.GetValue(recipeListIndex, elementIndex, produceTypeFieldIndex), out produceTypeId)
                    || produceTypeId <= 0
                    || map.ContainsKey(produceTypeId))
                {
                    continue;
                }

                for (int i = 0; i < productFieldIndexes.Count; i++)
                {
                    int itemId;
                    if (!int.TryParse(listCollection.GetValue(recipeListIndex, elementIndex, productFieldIndexes[i]), out itemId)
                        || itemId <= 0)
                    {
                        continue;
                    }

                    ItemReferenceOption itemOption;
                    if (TryBuildItemOptionByIdUncached(listCollection, itemId, database, iconResolutionService, out itemOption))
                    {
                        map[produceTypeId] = itemOption;
                        break;
                    }
                }
            }

            return map;
        }

        private Dictionary<int, ItemReferenceOption> BuildRecipeUsageMap(
            eListCollection listCollection,
            int recipeListIndex,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, ItemReferenceOption> map = new Dictionary<int, ItemReferenceOption>();
            if (listCollection == null
                || recipeListIndex < 0
                || recipeListIndex >= listCollection.Lists.Length
                || listCollection.Lists[recipeListIndex] == null
                || listCollection.Lists[recipeListIndex].elementFields == null
                || listCollection.Lists[recipeListIndex].elementValues == null)
            {
                return map;
            }

            string[] fields = listCollection.Lists[recipeListIndex].elementFields;
            List<int> productFieldIndexes = GetRecipeProducedItemFieldIndexes(fields);
            if (productFieldIndexes.Count == 0)
            {
                return map;
            }

            for (int elementIndex = 0; elementIndex < listCollection.Lists[recipeListIndex].elementValues.Length; elementIndex++)
            {
                int recipeId;
                if (!int.TryParse(listCollection.GetValue(recipeListIndex, elementIndex, 0), out recipeId)
                    || recipeId <= 0
                    || map.ContainsKey(recipeId))
                {
                    continue;
                }

                ItemReferenceOption itemOption;
                if (TryResolveRecipeProductOption(
                    listCollection,
                    recipeListIndex,
                    elementIndex,
                    productFieldIndexes,
                    database,
                    iconResolutionService,
                    out itemOption))
                {
                    map[recipeId] = itemOption;
                }
            }

            return map;
        }

        private Dictionary<int, ItemReferenceOption> BuildRecipeTypeUsageMap(
            eListCollection listCollection,
            string typeFieldName,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, ItemReferenceOption> map = new Dictionary<int, ItemReferenceOption>();
            int recipeListIndex;
            if (!TryFindListIndexByName(listCollection, "RECIPE_ESSENCE", out recipeListIndex)
                || listCollection.Lists[recipeListIndex] == null
                || listCollection.Lists[recipeListIndex].elementFields == null
                || listCollection.Lists[recipeListIndex].elementValues == null)
            {
                return map;
            }

            string[] fields = listCollection.Lists[recipeListIndex].elementFields;
            int typeFieldIndex = GetFieldIndex(fields, typeFieldName);
            List<int> productFieldIndexes = GetRecipeProducedItemFieldIndexes(fields);
            if (typeFieldIndex < 0 || productFieldIndexes.Count == 0)
            {
                return map;
            }

            for (int elementIndex = 0; elementIndex < listCollection.Lists[recipeListIndex].elementValues.Length; elementIndex++)
            {
                int typeId;
                if (!int.TryParse(listCollection.GetValue(recipeListIndex, elementIndex, typeFieldIndex), out typeId)
                    || typeId <= 0
                    || map.ContainsKey(typeId))
                {
                    continue;
                }

                ItemReferenceOption itemOption;
                if (TryResolveRecipeProductOption(
                    listCollection,
                    recipeListIndex,
                    elementIndex,
                    productFieldIndexes,
                    database,
                    iconResolutionService,
                    out itemOption))
                {
                    map[typeId] = itemOption;
                }
            }

            return map;
        }

        private bool TryResolveRecipeProductOption(
            eListCollection listCollection,
            int recipeListIndex,
            int recipeElementIndex,
            List<int> productFieldIndexes,
            CacheSave database,
            IconResolutionService iconResolutionService,
            out ItemReferenceOption itemOption)
        {
            itemOption = null;
            if (productFieldIndexes == null)
            {
                return false;
            }

            for (int i = 0; i < productFieldIndexes.Count; i++)
            {
                int itemId;
                if (!int.TryParse(listCollection.GetValue(recipeListIndex, recipeElementIndex, productFieldIndexes[i]), out itemId)
                    || itemId <= 0)
                {
                    continue;
                }

                if (TryBuildItemOptionByIdUncached(listCollection, itemId, database, iconResolutionService, out itemOption))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<int> GetRecipeProducedItemFieldIndexes(string[] fields)
        {
            List<int> productFields = new List<int>();
            if (fields == null)
            {
                return productFields;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string field = NormalizeFieldKey(fields[i]);
                if (field.IndexOf("products_", StringComparison.OrdinalIgnoreCase) >= 0
                    && field.EndsWith("_id_to_make", StringComparison.OrdinalIgnoreCase))
                {
                    productFields.Add(i);
                }
            }

            return productFields;
        }

        private bool TryBuildItemOptionByIdUncached(
            eListCollection listCollection,
            int itemId,
            CacheSave database,
            IconResolutionService iconResolutionService,
            out ItemReferenceOption option)
        {
            option = null;
            if (listCollection == null || itemId <= 0)
            {
                return false;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!ItemListCatalog.IsItemList(listCollection, listIndex)
                    || listCollection.Lists[listIndex] == null
                    || listCollection.Lists[listIndex].elementFields == null
                    || listCollection.Lists[listIndex].elementValues == null)
                {
                    continue;
                }

                int nameIndex = GetNameFieldIndex(listCollection, listIndex);
                int iconIndex = GetIconFieldIndex(listCollection, listIndex);
                int qualityIndex = GetQualityFieldIndex(listCollection, listIndex);
                for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
                {
                    int id;
                    if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out id) || id != itemId)
                    {
                        continue;
                    }

                    string rawName = nameIndex >= 0 ? listCollection.GetValue(listIndex, elementIndex, nameIndex) : string.Empty;
                    Color? nameForeColor;
                    string displayName = NormalizeOptionDisplayName(rawName, out nameForeColor);
                    option = new ItemReferenceOption
                    {
                        ListIndex = listIndex,
                        ElementIndex = elementIndex,
                        Id = id,
                        Name = displayName,
                        RawName = rawName,
                        NameForeColor = nameForeColor,
                        ListName = listCollection.Lists[listIndex].listName ?? string.Empty,
                        IconKey = ResolveSourceElementIconKey(listCollection, database, iconResolutionService, listIndex, elementIndex, iconIndex),
                        Quality = ResolveOptionQuality(listCollection, listIndex, elementIndex, qualityIndex)
                    };
                    return true;
                }
            }

            return false;
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

        private Dictionary<int, ItemReferenceOption> BuildNpcResetpropUsageMap(
            eListCollection listCollection,
            int resetpropListIndex,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, ItemReferenceOption> map = new Dictionary<int, ItemReferenceOption>();
            if (listCollection == null
                || resetpropListIndex < 0
                || resetpropListIndex >= listCollection.Lists.Length
                || listCollection.Lists[resetpropListIndex] == null
                || listCollection.Lists[resetpropListIndex].elementFields == null
                || listCollection.Lists[resetpropListIndex].elementValues == null)
            {
                return map;
            }

            string[] fields = listCollection.Lists[resetpropListIndex].elementFields;
            List<int> requiredItemFieldIndexes = GetNpcResetpropRequiredItemFieldIndexes(fields);
            if (requiredItemFieldIndexes.Count == 0)
            {
                return map;
            }

            for (int elementIndex = 0; elementIndex < listCollection.Lists[resetpropListIndex].elementValues.Length; elementIndex++)
            {
                int serviceId;
                if (!int.TryParse(listCollection.GetValue(resetpropListIndex, elementIndex, 0), out serviceId)
                    || serviceId <= 0
                    || map.ContainsKey(serviceId))
                {
                    continue;
                }

                for (int i = 0; i < requiredItemFieldIndexes.Count; i++)
                {
                    int itemId;
                    if (!int.TryParse(listCollection.GetValue(resetpropListIndex, elementIndex, requiredItemFieldIndexes[i]), out itemId)
                        || itemId <= 0)
                    {
                        continue;
                    }

                    ItemReferenceOption itemOption;
                    if (TryFindItemOptionByIdAcrossLists(listCollection, itemId, database, iconResolutionService, out itemOption)
                        && itemOption != null)
                    {
                        map[serviceId] = itemOption;
                        break;
                    }
                }
            }

            return map;
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
                if (!HasPrimaryIdField(listCollection, listIndex) || !ItemListCatalog.IsItemList(listCollection, listIndex))
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
            if (targetListIndex == TasksTargetIndex)
            {
                BuildTaskOptions(database);
                Dictionary<int, ItemReferenceOption> taskById;
                return optionsByIdByListIndex.TryGetValue(TasksTargetIndex, out taskById)
                    && taskById.TryGetValue(id, out option);
            }

            BuildOptions(listCollection, targetListIndex, database, iconResolutionService);

            Dictionary<int, ItemReferenceOption> byId;
            if (optionsByIdByListIndex.TryGetValue(targetListIndex, out byId)
                && byId.TryGetValue(id, out option))
            {
                EnsureInheritedOptionIcon(listCollection, targetListIndex, option, database, iconResolutionService);
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
            if (targetListIndex == TasksTargetIndex)
            {
                BuildTaskOptions(cachedDatabase);
                Dictionary<string, ItemReferenceOption> taskByName;
                return optionsByNameByListIndex.TryGetValue(TasksTargetIndex, out taskByName)
                    && taskByName.TryGetValue(name, out option);
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
            int currentTaskItemsRevision = database != null ? database.task_items_revision : 0;
            if (!object.ReferenceEquals(cachedListCollection, listCollection)
                || !object.ReferenceEquals(cachedDatabase, database)
                || !object.ReferenceEquals(cachedIconResolutionService, iconResolutionService)
                || cachedTaskItemsRevision != currentTaskItemsRevision)
            {
                ClearCache();
                cachedListCollection = listCollection;
                cachedDatabase = database;
                cachedIconResolutionService = iconResolutionService;
                cachedTaskItemsRevision = currentTaskItemsRevision;
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

        private static bool IsTaskReferenceField(string sourceListName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            string lowered = normalized.ToLowerInvariant();
            if (IsTaskServiceField(lowered) || lowered == "id_task_set")
            {
                return false;
            }

            if (lowered == "task_in" || lowered == "task_out")
            {
                return true;
            }

            if (lowered.StartsWith("id_tasks_", StringComparison.OrdinalIgnoreCase)
                || lowered.EndsWith("_task_id", StringComparison.OrdinalIgnoreCase)
                || lowered.EndsWith("_id_task", StringComparison.OrdinalIgnoreCase)
                || lowered.Contains("_task_id_")
                || lowered.Contains("_id_task_")
                || (lowered.StartsWith("tasks_", StringComparison.OrdinalIgnoreCase) && lowered.EndsWith("_id_task", StringComparison.OrdinalIgnoreCase))
                || (lowered.StartsWith("task_", StringComparison.OrdinalIgnoreCase) && lowered.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
                || (lowered.StartsWith("id_task_", StringComparison.OrdinalIgnoreCase) && !lowered.EndsWith("_service", StringComparison.OrdinalIgnoreCase))
                || HasIndexedTaskSuffix(lowered)
                || (lowered.StartsWith("task_") && int.TryParse(lowered.Substring(5), out _)))
            {
                return true;
            }

            if (!lowered.Contains("task"))
            {
                return false;
            }

            return string.Equals(sourceListName, "NPC_TASK_IN_SERVICE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sourceListName, "NPC_TASK_OUT_SERVICE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(sourceListName, "NPC_TASK_MATTER_SERVICE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTaskServiceField(string fieldName)
        {
            return string.Equals(fieldName, "id_task_out_service", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_task_in_service", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_task_matter_service", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasIndexedTaskSuffix(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            int markerIndex = fieldName.LastIndexOf("_task_", StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return false;
            }

            string suffix = fieldName.Substring(markerIndex + 6);
            return int.TryParse(suffix, out _);
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
                if (!FieldNameEquals(fields[i], typeFieldName))
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
            string normalizedTarget = NormalizeListName(targetListName);
            for (int i = 0; i < listCollection.Lists.Length; i++)
            {
                string listName = NormalizeListName(listCollection.Lists[i].listName);
                if (IsEquivalentListName(listName, normalizedTarget))
                {
                    targetListIndex = i;
                    return true;
                }
            }
            return false;
        }

        private static bool IsEquivalentListName(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (IsEquipmentEssenceAlias(left) && IsEquipmentEssenceAlias(right))
            {
                return true;
            }

            return false;
        }

        private static bool IsEquipmentEssenceAlias(string listName)
        {
            return string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPetBedgeEssenceListName(string listName)
        {
            return !string.IsNullOrWhiteSpace(listName)
                && (listName.IndexOf("PET_BEDGE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0
                    || listName.IndexOf("PET_BADGE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsMergeRecipeEssenceListName(string listName)
        {
            return !string.IsNullOrWhiteSpace(listName)
                && listName.IndexOf("MERGE_RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMergeRecipeItemReferenceField(string sourceListName, string fieldName)
        {
            if (!IsMergeRecipeEssenceListName(sourceListName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return (normalized.StartsWith("makes_", StringComparison.OrdinalIgnoreCase)
                    && normalized.IndexOf("_id", StringComparison.OrdinalIgnoreCase) >= 0)
                || (normalized.StartsWith("mains_", StringComparison.OrdinalIgnoreCase)
                    && normalized.IndexOf("_id_main", StringComparison.OrdinalIgnoreCase) >= 0)
                || (normalized.StartsWith("helpers_", StringComparison.OrdinalIgnoreCase)
                    && normalized.EndsWith("_id", StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryGetMappedTargetListName(string sourceListName, string fieldName, out string targetListName)
        {
            targetListName = null;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (string.Equals(sourceListName, "MEDICINE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(fieldName, "major_type", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "id_major_type", StringComparison.OrdinalIgnoreCase)))
            {
                targetListName = "MEDICINE_MAJOR_TYPE";
                return true;
            }

            if (string.Equals(sourceListName, "MEDICINE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(fieldName, "sub_type", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "id_sub_type", StringComparison.OrdinalIgnoreCase)))
            {
                targetListName = "MEDICINE_SUB_TYPE";
                return true;
            }

            if (string.Equals(sourceListName, "MATERIAL_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(fieldName, "major_type", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "id_major_type", StringComparison.OrdinalIgnoreCase)))
            {
                targetListName = "MATERIAL_MAJOR_TYPE";
                return true;
            }

            if (string.Equals(sourceListName, "MATERIAL_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(fieldName, "sub_type", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "id_sub_type", StringComparison.OrdinalIgnoreCase)))
            {
                targetListName = "MATERIAL_SUB_TYPE";
                return true;
            }

            if (string.Equals(sourceListName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(fieldName, "id_type", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "MONSTER_TYPE";
                return true;
            }

            if (string.Equals(sourceListName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(fieldName, "id_adjust_config", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "KM_PARAM_ADJUST_CONFIG";
                return true;
            }

            if (string.Equals(sourceListName, "CHARRACTER_CLASS_CONFIG", StringComparison.OrdinalIgnoreCase)
                && string.Equals(fieldName, "player_status_point_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PLAYER_STATUS_POINT_CONFIG";
                return true;
            }

            if (string.Equals(sourceListName, "MINE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(fieldName, "id_type", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "MINE_TYPE";
                return true;
            }

            if (string.Equals(sourceListName, "MINE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(fieldName, "id_relative_monster", StringComparison.OrdinalIgnoreCase)
                    || fieldName.EndsWith("_id_monster", StringComparison.OrdinalIgnoreCase)))
            {
                targetListName = "MONSTER_ESSENCE";
                return true;
            }

            if (string.Equals(sourceListName, "GM_GENERATOR_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(fieldName, "id_type", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "GM_GENERATOR_TYPE";
                return true;
            }

            if (IsNpcSourceMonsterField(sourceListName, fieldName))
            {
                targetListName = "MONSTER_ESSENCE";
                return true;
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

            if (IsSuiteReferenceField(fieldName))
            {
                targetListName = "SUITE_ESSENCE";
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

            if (fieldName.StartsWith("color_plan_id", StringComparison.OrdinalIgnoreCase)
                || fieldName.EndsWith("_color_plan_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "color_plan_id", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "COLOR_PLAN_CONFIG";
                return true;
            }

            if (fieldName.Contains("quench_config"))
            {
                targetListName = "QUENCH_CONFIG";
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
                && string.Equals(fieldName, "produce_type", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PRODUCE_TYPE_ESSENCE";
                return true;
            }

            if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && IsNumberedIdField(fieldName, "acquired_"))
            {
                targetListName = "PRODUCE_TYPE_ESSENCE";
                return true;
            }

            if (string.Equals(sourceListName, "RECIPE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && IsNumberedIdField(fieldName, "materials_"))
            {
                targetListName = null;
                return false;
            }

            return false;
        }

        private static bool IsStrictTargetReferenceField(eListCollection listCollection, int sourceListIndex, string fieldName)
        {
            string sourceListName = GetNormalizedListName(listCollection, sourceListIndex);
            if (IsNpcSourceMonsterField(sourceListName, fieldName))
            {
                return true;
            }

            if (!string.Equals(sourceListName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            return string.Equals(fieldName, "id_type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_adjust_config", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNpcSourceMonsterField(string sourceListName, string fieldName)
        {
            return string.Equals(sourceListName, "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(fieldName, "id_src_monster", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNpcLearnProduceSkillReferenceField(string sourceListName, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(sourceListName) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            return string.Equals(sourceListName, "NPC_LEARN_PRODUCE_SERVICE", StringComparison.OrdinalIgnoreCase)
                && fieldName.IndexOf("produce_skill", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSuiteReferenceField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            return fieldName.IndexOf("suite", StringComparison.OrdinalIgnoreCase) >= 0
                && (fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                    || fieldName.IndexOf("_id_", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool TryGetConventionalTypeTargetListIndex(
            eListCollection listCollection,
            string sourceListName,
            string fieldName,
            out int targetListIndex)
        {
            targetListIndex = -1;
            if (listCollection == null
                || string.IsNullOrWhiteSpace(sourceListName)
                || string.IsNullOrWhiteSpace(fieldName)
                || !sourceListName.EndsWith("_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string suffix = null;
            if (IsMajorTypeField(fieldName))
            {
                suffix = "_MAJOR_TYPE";
            }
            else if (IsSubTypeField(fieldName))
            {
                suffix = "_SUB_TYPE";
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
                return false;
            }

            string prefix = sourceListName.Substring(0, sourceListName.Length - "_ESSENCE".Length);
            return TryFindListIndexByName(listCollection, prefix + suffix, out targetListIndex);
        }

        private static bool IsMajorTypeField(string fieldName)
        {
            return string.Equals(fieldName, "major_type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_major_type", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSubTypeField(string fieldName)
        {
            return string.Equals(fieldName, "sub_type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_sub_type", StringComparison.OrdinalIgnoreCase);
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
                || IsProductIdToMakeField(fieldName)
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
                && IsNumberedIdField(fieldName, "materials_"))
            {
                return true;
            }

            return false;
        }

        private static bool IsProductIdToMakeField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.StartsWith("products_", StringComparison.OrdinalIgnoreCase)
                && fieldName.IndexOf("id_to_make", StringComparison.OrdinalIgnoreCase) >= 0;
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

        private static bool IsNpcResetpropRequiredItemField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.StartsWith("prop_entry_", StringComparison.OrdinalIgnoreCase)
                && fieldName.EndsWith("_id_object_need", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMineToolItemReferenceField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.StartsWith("tools_", StringComparison.OrdinalIgnoreCase)
                && fieldName.EndsWith("_tid", StringComparison.OrdinalIgnoreCase);
        }

        private static List<int> GetNpcResetpropRequiredItemFieldIndexes(string[] fields)
        {
            List<int> indexes = new List<int>();
            if (fields == null)
            {
                return indexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (IsNpcResetpropRequiredItemField(fields[i]))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private static bool IsNpcTransmitTargetField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.Trim().StartsWith("targets_", StringComparison.OrdinalIgnoreCase);
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
            string normalizedFieldName = NormalizeFieldKey(fieldName);
            return normalizedFieldName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && (normalizedFieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase) || normalizedFieldName.Contains("_id_"));
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

        private static string ResolveOptionName(eListCollection listCollection, int listIndex, int elementIndex, int nameIndex, string normalizedListName)
        {
            string name = nameIndex >= 0 ? listCollection.GetValue(listIndex, elementIndex, nameIndex) : string.Empty;
            if (!string.Equals(normalizedListName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || !LooksLikeNumericDisplayName(name))
            {
                return name;
            }

            int exactNameIndex = GetExactTextNameFieldIndex(listCollection, listIndex);
            if (exactNameIndex >= 0 && exactNameIndex != nameIndex)
            {
                string exactName = listCollection.GetValue(listIndex, elementIndex, exactNameIndex);
                if (!LooksLikeNumericDisplayName(exactName))
                {
                    return exactName;
                }
            }

            int fallbackIndex = FindFirstStringFieldIndex(listCollection, listIndex, new string[] { "name", "prop", "desc" });
            if (fallbackIndex >= 0 && fallbackIndex != nameIndex)
            {
                string fallbackName = listCollection.GetValue(listIndex, elementIndex, fallbackIndex);
                if (!LooksLikeNumericDisplayName(fallbackName))
                {
                    return fallbackName;
                }
            }

            return name;
        }

        private static int GetExactTextNameFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementTypes == null)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            string[] types = listCollection.Lists[listIndex].elementTypes;
            for (int i = 0; i < fields.Length && i < types.Length; i++)
            {
                if (string.Equals(fields[i], "name", StringComparison.OrdinalIgnoreCase)
                    && IsTextFieldType(types[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindFirstStringFieldIndex(eListCollection listCollection, int listIndex, string[] preferredFields)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementTypes == null
                || preferredFields == null)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            string[] types = listCollection.Lists[listIndex].elementTypes;
            for (int p = 0; p < preferredFields.Length; p++)
            {
                for (int i = 0; i < fields.Length && i < types.Length; i++)
                {
                    if (string.Equals(fields[i], preferredFields[p], StringComparison.OrdinalIgnoreCase)
                        && IsTextFieldType(types[i]))
                    {
                        return i;
                    }
                }
            }

            return -1;
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
                if (fieldName.StartsWith("equipments_", StringComparison.OrdinalIgnoreCase)
                    && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
                {
                    indexes.Add(i);
                }
            }

            return indexes;
        }

        private static bool IsTextFieldType(string fieldType)
        {
            return !string.IsNullOrWhiteSpace(fieldType)
                && (fieldType.IndexOf("string:", StringComparison.OrdinalIgnoreCase) >= 0
                    || fieldType.IndexOf("wstring:", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool LooksLikeNumericDisplayName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            int numeric;
            return int.TryParse(value.Trim(), out numeric);
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

        private static string ResolveOptionIconKey(eListCollection listCollection, CacheSave database, IconResolutionService iconResolutionService, int listIndex, int elementIndex, int iconIndex)
        {
            if (iconIndex < 0 || iconResolutionService == null)
            {
                return string.Empty;
            }

            string rawIcon = listCollection.GetValue(listIndex, elementIndex, iconIndex);
            return iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, rawIcon);
        }

        private ItemReferenceOption ApplySourceMonsterIcon(
            eListCollection listCollection,
            int sourceListIndex,
            int sourceElementIndex,
            string fieldName,
            ItemReferenceOption option,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            if (option == null
                || listCollection == null
                || sourceElementIndex < 0
                || !IsMonsterInheritedIconField(listCollection, sourceListIndex, fieldName))
            {
                return option;
            }

            int iconIndex = GetIconFieldIndex(listCollection, sourceListIndex);
            if (iconIndex < 0)
            {
                return option;
            }

            string iconKey = ResolveSourceElementIconKey(listCollection, database, iconResolutionService, sourceListIndex, sourceElementIndex, iconIndex);
            if (string.IsNullOrWhiteSpace(iconKey))
            {
                return option;
            }

            return new ItemReferenceOption
            {
                ListIndex = option.ListIndex,
                ElementIndex = option.ElementIndex,
                Id = option.Id,
                Name = option.Name,
                RawName = option.RawName,
                NameForeColor = option.NameForeColor,
                ListName = option.ListName,
                IconKey = iconKey,
                Quality = option.Quality,
                Description = option.Description,
                SecondaryText = option.SecondaryText,
                AccentHex = option.AccentHex,
                Kind = option.Kind
            };
        }

        private static bool IsMonsterInheritedIconField(eListCollection listCollection, int sourceListIndex, string fieldName)
        {
            if (!string.Equals(GetNormalizedListName(listCollection, sourceListIndex), "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            return string.Equals(fieldName, "id_type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "id_adjust_config", StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureInheritedOptionIcon(
            eListCollection listCollection,
            int targetListIndex,
            ItemReferenceOption option,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            if (option == null || !string.IsNullOrWhiteSpace(option.IconKey))
            {
                return;
            }

            string normalizedListName = GetNormalizedListName(listCollection, targetListIndex);
            string inheritedTypeSourceListName;
            string inheritedTypeFieldName;
            if (!TryGetInheritedTypeIconSource(normalizedListName, out inheritedTypeSourceListName, out inheritedTypeFieldName))
            {
                return;
            }

            Dictionary<int, string> iconKeyById = BuildInheritedTypeIconKeyMap(
                listCollection,
                database,
                iconResolutionService,
                inheritedTypeSourceListName,
                inheritedTypeFieldName);
            string iconKey;
            if (iconKeyById.TryGetValue(option.Id, out iconKey) && !string.IsNullOrWhiteSpace(iconKey))
            {
                option.IconKey = iconKey;
            }
        }

        private bool TryResolveNpcServicePortraitPathByField(
            eListCollection listCollection,
            CacheSave database,
            int serviceId,
            out string mappedPath,
            params string[] serviceFieldNames)
        {
            mappedPath = string.Empty;
            if (serviceId <= 0
                || listCollection == null
                || listCollection.Lists == null
                || database == null
                || serviceFieldNames == null
                || serviceFieldNames.Length == 0)
            {
                return false;
            }

            for (int npcListIndex = 0; npcListIndex < listCollection.Lists.Length; npcListIndex++)
            {
                eList list = listCollection.Lists[npcListIndex];
                if (list == null
                    || list.elementFields == null
                    || list.elementValues == null
                    || !string.Equals(GetNormalizedListName(listCollection, npcListIndex), "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int serviceFieldIndex = GetFirstFieldIndex(list.elementFields, serviceFieldNames);
                int iconFieldIndex = GetIconFieldIndex(listCollection, npcListIndex);
                if (serviceFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    int currentServiceId;
                    if (!int.TryParse(listCollection.GetValue(npcListIndex, rowIndex, serviceFieldIndex), out currentServiceId)
                        || currentServiceId != serviceId)
                    {
                        continue;
                    }

                    string rawIconValue = listCollection.GetValue(npcListIndex, rowIndex, iconFieldIndex);
                    int pathId;
                    if (!string.IsNullOrWhiteSpace(rawIconValue)
                        && creaturePortraitIconService.TryResolvePortraitPath(database, rawIconValue, out pathId, out mappedPath)
                        && !string.IsNullOrWhiteSpace(mappedPath))
                    {
                        return true;
                    }
                }
            }

            mappedPath = string.Empty;
            return false;
        }

        private Dictionary<int, string> BuildInheritedTypeIconKeyMap(
            eListCollection listCollection,
            CacheSave database,
            IconResolutionService iconResolutionService,
            string sourceListName,
            string typeFieldName)
        {
            string cacheKey = (sourceListName ?? string.Empty) + "|" + (typeFieldName ?? string.Empty);
            Dictionary<int, string> cached;
            if (inheritedTypeIconKeyCache.TryGetValue(cacheKey, out cached))
            {
                return cached;
            }

            Dictionary<int, string> map = new Dictionary<int, string>();
            inheritedTypeIconKeyCache[cacheKey] = map;
            if (listCollection == null || listCollection.Lists == null || iconResolutionService == null)
            {
                return map;
            }

            bool useSourceMaterialIcon = string.Equals(sourceListName, "MINE_ESSENCE", StringComparison.OrdinalIgnoreCase)
                && string.Equals(typeFieldName, "id_type", StringComparison.OrdinalIgnoreCase);
            Dictionary<int, string> itemIconKeyById = useSourceMaterialIcon
                ? BuildItemIconKeyMap(listCollection, database, iconResolutionService)
                : null;

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!string.Equals(GetNormalizedListName(listCollection, listIndex), sourceListName, StringComparison.OrdinalIgnoreCase)
                    || listCollection.Lists[listIndex] == null
                    || listCollection.Lists[listIndex].elementFields == null
                    || listCollection.Lists[listIndex].elementValues == null)
                {
                    continue;
                }

                int typeFieldIndex = GetFieldIndex(listCollection.Lists[listIndex].elementFields, typeFieldName);
                if (typeFieldIndex < 0 && string.Equals(typeFieldName, "id_major_type", StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = GetFieldIndex(listCollection.Lists[listIndex].elementFields, "major_type");
                }
                else if (typeFieldIndex < 0 && string.Equals(typeFieldName, "id_sub_type", StringComparison.OrdinalIgnoreCase))
                {
                    typeFieldIndex = GetFieldIndex(listCollection.Lists[listIndex].elementFields, "sub_type");
                }

                int iconFieldIndex = GetIconFieldIndex(listCollection, listIndex);
                List<int> sourceItemFieldIndexes = useSourceMaterialIcon
                    ? GetMineMaterialItemFieldIndexes(listCollection.Lists[listIndex].elementFields)
                    : null;
                if (typeFieldIndex < 0
                    || (!useSourceMaterialIcon && iconFieldIndex < 0)
                    || (useSourceMaterialIcon && (sourceItemFieldIndexes == null || sourceItemFieldIndexes.Count == 0)))
                {
                    continue;
                }

                for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
                {
                    int typeId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, typeFieldIndex), out typeId)
                        || typeId <= 0
                        || map.ContainsKey(typeId))
                    {
                        continue;
                    }

                    string iconKey = useSourceMaterialIcon
                        ? ResolveFirstItemFieldIconKey(listCollection, listIndex, elementIndex, sourceItemFieldIndexes, itemIconKeyById)
                        : ResolveSourceElementIconKey(listCollection, database, iconResolutionService, listIndex, elementIndex, iconFieldIndex);
                    if (!string.IsNullOrWhiteSpace(iconKey))
                    {
                        map[typeId] = iconKey;
                    }
                }
            }

            return map;
        }

        private Dictionary<int, string> BuildItemIconKeyMap(
            eListCollection listCollection,
            CacheSave database,
            IconResolutionService iconResolutionService)
        {
            Dictionary<int, string> map = new Dictionary<int, string>();
            if (listCollection == null || listCollection.Lists == null || iconResolutionService == null)
            {
                return map;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!HasPrimaryIdField(listCollection, listIndex)
                    || !ItemListCatalog.IsItemList(listCollection, listIndex)
                    || listCollection.Lists[listIndex] == null
                    || listCollection.Lists[listIndex].elementValues == null)
                {
                    continue;
                }

                int iconIndex = GetIconFieldIndex(listCollection, listIndex);
                if (iconIndex < 0)
                {
                    continue;
                }

                for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
                {
                    int itemId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out itemId)
                        || itemId <= 0
                        || map.ContainsKey(itemId))
                    {
                        continue;
                    }

                    string iconKey = ResolveSourceElementIconKey(
                        listCollection,
                        database,
                        iconResolutionService,
                        listIndex,
                        elementIndex,
                        iconIndex);
                    if (!string.IsNullOrWhiteSpace(iconKey))
                    {
                        map[itemId] = iconKey;
                    }
                }
            }

            return map;
        }

        private string ResolveFirstItemFieldIconKey(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            List<int> itemFieldIndexes,
            Dictionary<int, string> itemIconKeyById)
        {
            if (listCollection == null
                || itemFieldIndexes == null
                || itemIconKeyById == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < itemFieldIndexes.Count; i++)
            {
                int itemId;
                if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, itemFieldIndexes[i]), out itemId)
                    || itemId <= 0)
                {
                    continue;
                }

                string iconKey;
                if (itemIconKeyById.TryGetValue(itemId, out iconKey)
                    && !string.IsNullOrWhiteSpace(iconKey))
                {
                    return iconKey;
                }
            }

            return string.Empty;
        }

        private string ResolveSourceElementIconKey(
            eListCollection listCollection,
            CacheSave database,
            IconResolutionService iconResolutionService,
            int listIndex,
            int elementIndex,
            int iconIndex)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || iconIndex < 0
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return string.Empty;
            }

            string rawIcon = listCollection.GetValue(listIndex, elementIndex, iconIndex);
            string iconFieldName = listCollection.Lists[listIndex].elementFields != null
                && iconIndex < listCollection.Lists[listIndex].elementFields.Length
                ? listCollection.Lists[listIndex].elementFields[iconIndex]
                : string.Empty;

            if (creaturePortraitIconService.IsCreaturePortraitField(listCollection, listIndex, iconFieldName))
            {
                int pathId;
                string mappedPath;
                if (creaturePortraitIconService.TryResolvePortraitPath(database, rawIcon, out pathId, out mappedPath))
                {
                    return mappedPath;
                }
            }

            return ResolveOptionIconKey(listCollection, database, iconResolutionService, listIndex, elementIndex, iconIndex);
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
            if (string.Equals(normalizedListName, "MONSTER_TYPE", StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = "MONSTER_ESSENCE";
                typeFieldName = "id_type";
                return true;
            }

            if (string.Equals(normalizedListName, "KM_PARAM_ADJUST_CONFIG", StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = "MONSTER_ESSENCE";
                typeFieldName = "id_adjust_config";
                return true;
            }

            if (string.Equals(normalizedListName, "MINE_TYPE", StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = "MINE_ESSENCE";
                typeFieldName = "id_type";
                return true;
            }

            if (string.Equals(normalizedListName, "GM_GENERATOR_TYPE", StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = "GM_GENERATOR_ESSENCE";
                typeFieldName = "id_type";
                return true;
            }

            if (normalizedListName.EndsWith(majorSuffix, StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = normalizedListName.Substring(0, normalizedListName.Length - majorSuffix.Length) + "_ESSENCE";
                typeFieldName = "id_major_type";
                return true;
            }

            if (normalizedListName.EndsWith(subSuffix, StringComparison.OrdinalIgnoreCase))
            {
                sourceListName = normalizedListName.Substring(0, normalizedListName.Length - subSuffix.Length) + "_ESSENCE";
                typeFieldName = "id_sub_type";
                return true;
            }

            return false;
        }

        private static string GetNormalizedListName(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null)
            {
                return string.Empty;
            }

            return NormalizeListName(listCollection.Lists[listIndex].listName);
        }

        private static List<int> GetMineMaterialItemFieldIndexes(string[] fields)
        {
            List<int> primaryIndexes = new List<int>();
            List<int> fallbackIndexes = new List<int>();
            if (fields == null)
            {
                return primaryIndexes;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string fieldName = fields[i] ?? string.Empty;
                if (!fieldName.StartsWith("materials_", StringComparison.OrdinalIgnoreCase)
                    || !(fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                        || fieldName.IndexOf("_id_", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    continue;
                }

                if (fieldName.StartsWith("materials_1_1_", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "materials_0_id", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fieldName, "materials_1_id", StringComparison.OrdinalIgnoreCase))
                {
                    primaryIndexes.Add(i);
                }
                else
                {
                    fallbackIndexes.Add(i);
                }
            }

            primaryIndexes.AddRange(fallbackIndexes);
            return primaryIndexes;
        }

        private static int GetFieldIndex(string[] fields, string fieldName)
        {
            if (fields == null)
            {
                return -1;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetFirstFieldIndex(string[] fields, params string[] fieldNames)
        {
            if (fields == null || fieldNames == null)
            {
                return -1;
            }

            for (int i = 0; i < fieldNames.Length; i++)
            {
                int fieldIndex = GetFieldIndex(fields, fieldNames[i]);
                if (fieldIndex >= 0)
                {
                    return fieldIndex;
                }
            }

            return -1;
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

                string rawName = !string.IsNullOrEmpty(option.RawName) ? option.RawName : option.Name;
                Color? nameForeColor = option.NameForeColor;
                string displayName = option.Name ?? string.Empty;
                if (!nameForeColor.HasValue || FwTextColorService.HasLeadingColor(displayName))
                {
                    displayName = NormalizeOptionDisplayName(rawName, out nameForeColor);
                }

                clone.Add(new ItemReferenceOption
                {
                    ListIndex = option.ListIndex,
                    ElementIndex = option.ElementIndex,
                    Id = option.Id,
                    Name = displayName,
                    RawName = rawName,
                    NameForeColor = nameForeColor,
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

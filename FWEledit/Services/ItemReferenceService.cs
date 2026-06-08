using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemReferenceService
    {
        private const int AllListsTargetIndex = -1;
        private const int ItemListsTargetIndex = -2;

        private eListCollection cachedListCollection;
        private CacheSave cachedDatabase;
        private IconResolutionService cachedIconResolutionService;
        private readonly Dictionary<int, List<ItemReferenceOption>> optionsByListIndex = new Dictionary<int, List<ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<int, ItemReferenceOption>> optionsByIdByListIndex = new Dictionary<int, Dictionary<int, ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<string, ItemReferenceOption>> optionsByNameByListIndex = new Dictionary<int, Dictionary<string, ItemReferenceOption>>();
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

            if (name.StartsWith("id_estone_", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "ESTONE_ESSENCE";
            }
            else if (string.Equals(name, "basic_show_level", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "TASKNORMALMATTER_ESSENCE";
            }
            else if (name.StartsWith("id_pstone_", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "PSTONE_ESSENCE";
            }
            else if (name.StartsWith("id_sstone_", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "SSTONE_ESSENCE";
            }
            else if (name.StartsWith("enhanced_prop_package_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_sign_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_special_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_prefix_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_postfix_addon_package", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "ADDON_PACKAGE_CONFIG";
            }
            else if (string.Equals(name, "id_quality", StringComparison.OrdinalIgnoreCase))
            {
                targetListName = "EQUIPMENT_QUALITY_CONFIG";
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
            return FormatReferenceValue(listCollection, listIndex, -1, fieldName, rawValue);
        }

        public string FormatReferenceValue(eListCollection listCollection, int listIndex, int elementIndex, string fieldName, string rawValue)
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

            ItemReferenceOption option;
            if (targetListIndex >= 0 && TryFindOptionById(listCollection, targetListIndex, id, out option))
            {
                return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
            }

            if (targetListIndex == ItemListsTargetIndex)
            {
                if (!TryFindItemOptionByIdAcrossLists(listCollection, id, null, null, out option))
                {
                    return rawValue ?? string.Empty;
                }
            }
            else if (!TryFindOptionByIdAcrossLists(listCollection, id, null, null, out option))
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
            for (int i = 0; i < listCollection.Lists[targetListIndex].elementValues.Length; i++)
            {
                int id;
                if (!int.TryParse(listCollection.GetValue(targetListIndex, i, 0), out id))
                {
                    continue;
                }

                string name = nameIndex >= 0 ? listCollection.GetValue(targetListIndex, i, nameIndex) : string.Empty;
                options.Add(new ItemReferenceOption
                {
                    ListIndex = targetListIndex,
                    ElementIndex = i,
                    Id = id,
                    Name = name,
                    ListName = listName,
                    IconKey = ResolveOptionIconKey(listCollection, database, iconResolutionService, targetListIndex, i, iconIndex),
                    Quality = ResolveOptionQuality(listCollection, targetListIndex, i, qualityIndex)
                });
            }

            return options;
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

            if (fieldName.Contains("pet_skill") && fieldName.EndsWith("_id", StringComparison.OrdinalIgnoreCase))
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
                    Quality = option.Quality
                });
            }

            return clone;
        }
    }
}

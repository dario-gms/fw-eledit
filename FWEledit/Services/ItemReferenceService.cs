using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemReferenceService
    {
        private eListCollection cachedListCollection;
        private CacheSave cachedDatabase;
        private IconResolutionService cachedIconResolutionService;
        private readonly Dictionary<int, List<ItemReferenceOption>> optionsByListIndex = new Dictionary<int, List<ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<int, ItemReferenceOption>> optionsByIdByListIndex = new Dictionary<int, Dictionary<int, ItemReferenceOption>>();
        private readonly Dictionary<int, Dictionary<string, ItemReferenceOption>> optionsByNameByListIndex = new Dictionary<int, Dictionary<string, ItemReferenceOption>>();
        private List<ItemReferenceOption> searchableOptions;
        private Dictionary<int, ItemReferenceOption> searchableOptionsById;
        private Dictionary<string, ItemReferenceOption> searchableOptionsByName;

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
        }

        public bool IsReferenceField(eListCollection listCollection, int listIndex, string fieldName)
        {
            int targetListIndex;
            return TryGetTargetListIndex(listCollection, listIndex, fieldName, out targetListIndex);
        }

        public bool TryGetTargetListIndex(eListCollection listCollection, int listIndex, string fieldName, out int targetListIndex)
        {
            targetListIndex = -1;
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string name = fieldName.Trim();
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

            if (string.IsNullOrWhiteSpace(targetListName))
            {
                return false;
            }

            return TryFindListIndexByName(listCollection, targetListName, out targetListIndex);
        }

        public string FormatReferenceValue(eListCollection listCollection, int listIndex, string fieldName, string rawValue)
        {
            int id;
            if (!int.TryParse(rawValue, out id) || id <= 0)
            {
                return rawValue ?? string.Empty;
            }

            int targetListIndex;
            if (!TryGetTargetListIndex(listCollection, listIndex, fieldName, out targetListIndex))
            {
                return rawValue ?? string.Empty;
            }

            ItemReferenceOption option;
            if (!TryFindOptionById(listCollection, targetListIndex, id, out option))
            {
                if (!TryFindOptionByIdAcrossLists(listCollection, id, null, null, out option))
                {
                    return rawValue ?? string.Empty;
                }
            }

            return string.IsNullOrWhiteSpace(option.Name) ? rawValue : option.Name;
        }

        public bool TryResolveReferenceOption(eListCollection listCollection, int listIndex, string fieldName, string rawValue, CacheSave database, IconResolutionService iconResolutionService, out ItemReferenceOption option)
        {
            option = null;
            int id;
            if (!int.TryParse(rawValue, out id) || id <= 0)
            {
                return false;
            }

            int targetListIndex;
            if (!TryGetTargetListIndex(listCollection, listIndex, fieldName, out targetListIndex))
            {
                return false;
            }

            if (TryFindOptionById(listCollection, targetListIndex, id, database, iconResolutionService, out option))
            {
                return true;
            }

            return TryFindOptionByIdAcrossLists(listCollection, id, database, iconResolutionService, out option);
        }

        public string NormalizeReferenceInput(eListCollection listCollection, int listIndex, string fieldName, string value)
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
            if (!TryGetTargetListIndex(listCollection, listIndex, fieldName, out targetListIndex))
            {
                return value;
            }

            ItemReferenceOption option;
            if (TryFindOptionByName(listCollection, targetListIndex, value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            if (TryFindOptionByNameAcrossLists(listCollection, value.Trim(), out option))
            {
                return option.Id.ToString();
            }

            return value;
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
    }
}

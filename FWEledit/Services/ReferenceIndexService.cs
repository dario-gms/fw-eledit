using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ReferenceIndexService
    {
        private readonly object syncRoot = new object();
        private readonly GameShopDataService gameShopDataService = new GameShopDataService();
        private eListCollection cachedListCollection;
        private ItemReferenceService cachedReferenceService;
        private string cachedElementsPath = string.Empty;
        private readonly Dictionary<string, List<ReferenceUsage>> referencesByTarget = new Dictionary<string, List<ReferenceUsage>>();
        private Dictionary<int, List<int>> itemListIndexesById = new Dictionary<int, List<int>>();
        private readonly Dictionary<int, int[]> referenceFieldIndexesBySourceList = new Dictionary<int, int[]>();
        private bool indexBuilt;

        public string ElementsPath { get; set; } = string.Empty;

        public void Clear()
        {
            lock (syncRoot)
            {
                cachedListCollection = null;
                cachedReferenceService = null;
                cachedElementsPath = string.Empty;
                referencesByTarget.Clear();
                itemListIndexesById.Clear();
                referenceFieldIndexesBySourceList.Clear();
                indexBuilt = false;
            }
        }

        public void EnsureBuilt(eListCollection listCollection, ItemReferenceService referenceService)
        {
            lock (syncRoot)
            {
                EnsureIndex(listCollection, referenceService);
            }
        }

        public Dictionary<string, List<ReferenceUsage>> ExportCache(
            eListCollection listCollection,
            ItemReferenceService referenceService)
        {
            lock (syncRoot)
            {
                EnsureIndex(listCollection, referenceService);
                return CloneIndex(referencesByTarget);
            }
        }

        public void ImportCache(
            eListCollection listCollection,
            ItemReferenceService referenceService,
            Dictionary<string, List<ReferenceUsage>> cache)
        {
            lock (syncRoot)
            {
                cachedListCollection = listCollection;
                cachedReferenceService = referenceService;
                cachedElementsPath = ElementsPath ?? string.Empty;
                referencesByTarget.Clear();

                if (cache != null)
                {
                    Dictionary<string, List<ReferenceUsage>> cloned = CloneIndex(cache);
                    foreach (KeyValuePair<string, List<ReferenceUsage>> pair in cloned)
                    {
                        referencesByTarget[pair.Key] = pair.Value;
                    }
                }

                itemListIndexesById = BuildItemListIndexMap(listCollection, referenceService);
                BuildReferenceFieldIndexMap(listCollection, referenceService);
                indexBuilt = true;
            }
        }

        public void RebuildSourceElement(
            eListCollection listCollection,
            ItemReferenceService referenceService,
            int sourceListIndex,
            int sourceElementIndex)
        {
            lock (syncRoot)
            {
                EnsureIndex(listCollection, referenceService);
                if (!indexBuilt)
                {
                    return;
                }

                if (listCollection == null
                    || listCollection.Lists == null
                    || sourceListIndex < 0
                    || sourceListIndex >= listCollection.Lists.Length
                    || sourceListIndex == listCollection.ConversationListIndex
                    || listCollection.Lists[sourceListIndex] == null
                    || listCollection.Lists[sourceListIndex].elementFields == null
                    || listCollection.Lists[sourceListIndex].elementValues == null
                    || sourceElementIndex < 0
                    || sourceElementIndex >= listCollection.Lists[sourceListIndex].elementValues.Length)
                {
                    return;
                }

                RemoveUsagesForSourceElement(sourceListIndex, sourceElementIndex);
                AddUsagesForSourceElement(listCollection, referenceService, sourceListIndex, sourceElementIndex);
            }
        }

        public int GetReferenceCount(
            eListCollection listCollection,
            ItemReferenceService referenceService,
            int targetListIndex,
            int targetId)
        {
            lock (syncRoot)
            {
                return GetReferences(listCollection, referenceService, targetListIndex, targetId).Count;
            }
        }

        public List<ReferenceUsage> GetReferences(
            eListCollection listCollection,
            ItemReferenceService referenceService,
            int targetListIndex,
            int targetId)
        {
            lock (syncRoot)
            {
                EnsureIndex(listCollection, referenceService);

                List<ReferenceUsage> merged = new List<ReferenceUsage>();
                List<ReferenceUsage> usages;
                if (referencesByTarget.TryGetValue(BuildKey(targetListIndex, targetId), out usages))
                {
                    AddUniqueUsages(merged, usages);
                }

                if (referenceService.IsItemBearingList(listCollection, targetListIndex)
                    && referencesByTarget.TryGetValue(BuildKey(-1, targetId), out usages))
                {
                    AddUniqueUsages(merged, usages);
                }

                int titleDefinitionId;
                if (TryGetTitleDefinitionIdForTitlePropElement(listCollection, targetListIndex, targetId, out titleDefinitionId)
                    && referencesByTarget.TryGetValue(BuildKey(TitleDefinitionCatalog.TargetListIndex, titleDefinitionId), out usages))
                {
                    AddUniqueTitleDefinitionUsagesForTitleProp(merged, usages, targetListIndex, targetId);
                }

                return merged;
            }
        }

        private void EnsureIndex(eListCollection listCollection, ItemReferenceService referenceService)
        {
            if (listCollection == null || referenceService == null)
            {
                Clear();
                return;
            }

            if (object.ReferenceEquals(cachedListCollection, listCollection)
                && object.ReferenceEquals(cachedReferenceService, referenceService)
                && string.Equals(cachedElementsPath, ElementsPath ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && indexBuilt)
            {
                return;
            }

            cachedListCollection = listCollection;
            cachedReferenceService = referenceService;
            cachedElementsPath = ElementsPath ?? string.Empty;
            referencesByTarget.Clear();
            itemListIndexesById = BuildItemListIndexMap(listCollection, referenceService);
            BuildReferenceFieldIndexMap(listCollection, referenceService);
            BuildIndex(listCollection, referenceService);
            AddGameShopUsages(itemListIndexesById);
            indexBuilt = true;
        }

        private void BuildIndex(eListCollection listCollection, ItemReferenceService referenceService)
        {
            if (listCollection == null || listCollection.Lists == null || referenceService == null)
            {
                return;
            }

            for (int sourceListIndex = 0; sourceListIndex < listCollection.Lists.Length; sourceListIndex++)
            {
                if (sourceListIndex == listCollection.ConversationListIndex
                    || listCollection.Lists[sourceListIndex] == null
                    || listCollection.Lists[sourceListIndex].elementFields == null
                    || listCollection.Lists[sourceListIndex].elementValues == null)
                {
                    continue;
                }

                int[] referenceFieldIndexes;
                if (!referenceFieldIndexesBySourceList.TryGetValue(sourceListIndex, out referenceFieldIndexes)
                    || referenceFieldIndexes == null
                    || referenceFieldIndexes.Length == 0)
                {
                    continue;
                }

                string[] fields = listCollection.Lists[sourceListIndex].elementFields;
                int elementCount = listCollection.Lists[sourceListIndex].elementValues.Length;
                for (int sourceElementIndex = 0; sourceElementIndex < elementCount; sourceElementIndex++)
                {
                    for (int i = 0; i < referenceFieldIndexes.Length; i++)
                    {
                        int fieldIndex = referenceFieldIndexes[i];
                        AddUsageForField(
                            listCollection,
                            referenceService,
                            sourceListIndex,
                            sourceElementIndex,
                            fieldIndex,
                            fields[fieldIndex] ?? string.Empty);
                    }
                }
            }
        }

        private void AddUsagesForSourceElement(
            eListCollection listCollection,
            ItemReferenceService referenceService,
            int sourceListIndex,
            int sourceElementIndex)
        {
            string[] fields = listCollection.Lists[sourceListIndex].elementFields;
            int[] referenceFieldIndexes;
            if (!referenceFieldIndexesBySourceList.TryGetValue(sourceListIndex, out referenceFieldIndexes)
                || referenceFieldIndexes == null
                || referenceFieldIndexes.Length == 0)
            {
                return;
            }

            for (int i = 0; i < referenceFieldIndexes.Length; i++)
            {
                int fieldIndex = referenceFieldIndexes[i];
                AddUsageForField(
                    listCollection,
                    referenceService,
                    sourceListIndex,
                    sourceElementIndex,
                    fieldIndex,
                    fields[fieldIndex] ?? string.Empty);
            }
        }

        private void AddUsageForField(
            eListCollection listCollection,
            ItemReferenceService referenceService,
            int sourceListIndex,
            int sourceElementIndex,
            int fieldIndex,
            string fieldName)
        {
            int targetListIndex;
            if (!referenceService.TryGetTargetListIndex(listCollection, sourceListIndex, sourceElementIndex, fieldName, out targetListIndex))
            {
                return;
            }

            int targetId;
            if (!int.TryParse(listCollection.GetValue(sourceListIndex, sourceElementIndex, fieldIndex), out targetId) || targetId <= 0)
            {
                return;
            }

            ReferenceUsage usage = BuildUsage(listCollection, sourceListIndex, sourceElementIndex, fieldIndex, fieldName, targetId);
            if (referenceService.IsItemListTargetIndex(targetListIndex))
            {
                AddUsageForItemTargets(itemListIndexesById, targetId, usage);
            }
            else
            {
                AddUsage(targetListIndex, targetId, usage);
            }
        }

        private static Dictionary<int, List<int>> BuildItemListIndexMap(
            eListCollection listCollection,
            ItemReferenceService referenceService)
        {
            Dictionary<int, List<int>> itemListIndexesById = new Dictionary<int, List<int>>();
            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (!referenceService.IsItemBearingList(listCollection, listIndex)
                    || listCollection.Lists[listIndex].elementValues == null)
                {
                    continue;
                }

                for (int elementIndex = 0; elementIndex < listCollection.Lists[listIndex].elementValues.Length; elementIndex++)
                {
                    int candidateId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, elementIndex, 0), out candidateId) || candidateId <= 0)
                    {
                        continue;
                    }

                    List<int> listIndexes;
                    if (!itemListIndexesById.TryGetValue(candidateId, out listIndexes))
                    {
                        listIndexes = new List<int>();
                        itemListIndexesById[candidateId] = listIndexes;
                    }

                    if (!listIndexes.Contains(listIndex))
                    {
                        listIndexes.Add(listIndex);
                    }
                }
            }

            return itemListIndexesById;
        }

        private void BuildReferenceFieldIndexMap(
            eListCollection listCollection,
            ItemReferenceService referenceService)
        {
            referenceFieldIndexesBySourceList.Clear();
            if (listCollection == null || listCollection.Lists == null || referenceService == null)
            {
                return;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (listIndex == listCollection.ConversationListIndex
                    || listCollection.Lists[listIndex] == null
                    || listCollection.Lists[listIndex].elementFields == null)
                {
                    continue;
                }

                string[] fields = listCollection.Lists[listIndex].elementFields;
                List<int> referenceFieldIndexes = new List<int>();
                for (int fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    string fieldName = fields[fieldIndex] ?? string.Empty;
                    if (referenceService.IsReferenceField(listCollection, listIndex, fieldName))
                    {
                        referenceFieldIndexes.Add(fieldIndex);
                    }
                }

                referenceFieldIndexesBySourceList[listIndex] = referenceFieldIndexes.ToArray();
            }
        }

        private void AddUsageForItemTargets(
            Dictionary<int, List<int>> itemListIndexesById,
            int targetId,
            ReferenceUsage usage)
        {
            AddUsage(-1, targetId, usage);

            List<int> listIndexes;
            if (itemListIndexesById == null || !itemListIndexesById.TryGetValue(targetId, out listIndexes))
            {
                return;
            }

            for (int i = 0; i < listIndexes.Count; i++)
            {
                AddUsage(listIndexes[i], targetId, usage);
            }
        }

        private static bool TryGetTitleDefinitionIdForTitlePropElement(
            eListCollection listCollection,
            int titlePropListIndex,
            int titlePropId,
            out int titleDefinitionId)
        {
            titleDefinitionId = 0;
            if (listCollection == null
                || listCollection.Lists == null
                || titlePropListIndex < 0
                || titlePropListIndex >= listCollection.Lists.Length
                || titlePropId <= 0)
            {
                return false;
            }

            eList list = listCollection.Lists[titlePropListIndex];
            if (list == null
                || list.elementFields == null
                || list.elementValues == null
                || !NormalizeToken(list.listName).EndsWith("TITLEPROPCONFIG", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int titleFieldIndex = -1;
            for (int i = 0; i < list.elementFields.Length; i++)
            {
                if (string.Equals(NormalizeToken(list.elementFields[i]), "IDTITLE", StringComparison.OrdinalIgnoreCase))
                {
                    titleFieldIndex = i;
                    break;
                }
            }

            if (titleFieldIndex < 0)
            {
                return false;
            }

            for (int elementIndex = 0; elementIndex < list.elementValues.Length; elementIndex++)
            {
                int id;
                if (!int.TryParse(listCollection.GetValue(titlePropListIndex, elementIndex, 0), out id)
                    || id != titlePropId)
                {
                    continue;
                }

                return int.TryParse(listCollection.GetValue(titlePropListIndex, elementIndex, titleFieldIndex), out titleDefinitionId)
                    && titleDefinitionId > 0;
            }

            return false;
        }

        private static string NormalizeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            char[] buffer = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                {
                    buffer[count++] = char.ToUpperInvariant(c);
                }
            }

            return new string(buffer, 0, count);
        }

        private void AddGameShopUsages(Dictionary<int, List<int>> itemListIndexesById)
        {
            if (itemListIndexesById == null || itemListIndexesById.Count == 0)
            {
                return;
            }

            HashSet<int> knownItemIds = new HashSet<int>(itemListIndexesById.Keys);
            List<GameShopEntry> entries = gameShopDataService.LoadEntries(ElementsPath);
            for (int i = 0; i < entries.Count; i++)
            {
                GameShopEntry entry = entries[i];
                int targetId = entry != null ? entry.ItemId : 0;
                if (targetId <= 0 || !knownItemIds.Contains(targetId))
                {
                    continue;
                }

                ReferenceUsage usage = new ReferenceUsage
                {
                    SourceListIndex = GameShopDataService.SourceListIndex,
                    SourceElementIndex = entry.RowIndex,
                    SourceFieldIndex = -1,
                    SourceListName = GameShopDataService.SourceListName,
                    SourceItemId = entry.Id > 0 ? entry.Id.ToString() : entry.RowIndex.ToString(),
                    SourceItemName = entry.Name,
                    SourceFieldName = "Item",
                    RawValue = targetId.ToString()
                };

                AddUsageForItemTargets(itemListIndexesById, targetId, usage);
            }
        }

        private static void AddUniqueUsages(List<ReferenceUsage> target, List<ReferenceUsage> source)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ReferenceUsage usage = source[i];
                if (usage == null || ContainsUsage(target, usage))
                {
                    continue;
                }

                target.Add(usage);
            }
        }

        private static void AddUniqueTitleDefinitionUsagesForTitleProp(
            List<ReferenceUsage> target,
            List<ReferenceUsage> source,
            int titlePropListIndex,
            int titlePropId)
        {
            if (target == null || source == null)
            {
                return;
            }

            for (int i = 0; i < source.Count; i++)
            {
                ReferenceUsage usage = source[i];
                if (usage == null || IsSelfTitlePropDefinitionUsage(usage, titlePropListIndex, titlePropId) || ContainsUsage(target, usage))
                {
                    continue;
                }

                target.Add(usage);
            }
        }

        private static bool IsSelfTitlePropDefinitionUsage(ReferenceUsage usage, int titlePropListIndex, int titlePropId)
        {
            int sourceId;
            return usage != null
                && usage.SourceListIndex == titlePropListIndex
                && int.TryParse(usage.SourceItemId, out sourceId)
                && sourceId == titlePropId
                && string.Equals(NormalizeToken(usage.SourceFieldName), "IDTITLE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsUsage(List<ReferenceUsage> usages, ReferenceUsage candidate)
        {
            if (usages == null || candidate == null)
            {
                return false;
            }

            for (int i = 0; i < usages.Count; i++)
            {
                ReferenceUsage usage = usages[i];
                if (usage != null
                    && usage.SourceListIndex == candidate.SourceListIndex
                    && usage.SourceElementIndex == candidate.SourceElementIndex
                    && usage.SourceFieldIndex == candidate.SourceFieldIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private static ReferenceUsage BuildUsage(
            eListCollection listCollection,
            int sourceListIndex,
            int sourceElementIndex,
            int sourceFieldIndex,
            string sourceFieldName,
            int rawValue)
        {
            int nameIndex = GetNameFieldIndex(listCollection, sourceListIndex);
            string sourceItemName = nameIndex >= 0
                ? listCollection.GetValue(sourceListIndex, sourceElementIndex, nameIndex)
                : string.Empty;

            return new ReferenceUsage
            {
                SourceListIndex = sourceListIndex,
                SourceElementIndex = sourceElementIndex,
                SourceFieldIndex = sourceFieldIndex,
                SourceListName = listCollection.Lists[sourceListIndex].listName ?? string.Empty,
                SourceItemId = listCollection.GetValue(sourceListIndex, sourceElementIndex, 0),
                SourceItemName = sourceItemName,
                SourceFieldName = sourceFieldName ?? string.Empty,
                RawValue = rawValue.ToString()
            };
        }

        private void AddUsage(int targetListIndex, int targetId, ReferenceUsage usage)
        {
            string key = BuildKey(targetListIndex, targetId);
            List<ReferenceUsage> usages;
            if (!referencesByTarget.TryGetValue(key, out usages))
            {
                usages = new List<ReferenceUsage>();
                referencesByTarget[key] = usages;
            }
            usages.Add(usage);
        }

        private void RemoveUsagesForSourceElement(int sourceListIndex, int sourceElementIndex)
        {
            List<string> emptyKeys = null;
            foreach (KeyValuePair<string, List<ReferenceUsage>> pair in referencesByTarget)
            {
                pair.Value.RemoveAll(usage =>
                    usage != null
                    && usage.SourceListIndex == sourceListIndex
                    && usage.SourceElementIndex == sourceElementIndex);

                if (pair.Value.Count == 0)
                {
                    if (emptyKeys == null)
                    {
                        emptyKeys = new List<string>();
                    }

                    emptyKeys.Add(pair.Key);
                }
            }

            if (emptyKeys == null)
            {
                return;
            }

            for (int i = 0; i < emptyKeys.Count; i++)
            {
                referencesByTarget.Remove(emptyKeys[i]);
            }
        }

        private static int GetNameFieldIndex(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return -1;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            if (fields == null)
            {
                return -1;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], "name", System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string BuildKey(int listIndex, int id)
        {
            return listIndex.ToString() + "|" + id.ToString();
        }

        private static Dictionary<string, List<ReferenceUsage>> CloneIndex(Dictionary<string, List<ReferenceUsage>> source)
        {
            Dictionary<string, List<ReferenceUsage>> clone = new Dictionary<string, List<ReferenceUsage>>();
            if (source == null)
            {
                return clone;
            }

            foreach (KeyValuePair<string, List<ReferenceUsage>> pair in source)
            {
                List<ReferenceUsage> usages = new List<ReferenceUsage>();
                if (pair.Value != null)
                {
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        ReferenceUsage usage = pair.Value[i];
                        if (usage == null)
                        {
                            continue;
                        }

                        usages.Add(new ReferenceUsage
                        {
                            SourceListIndex = usage.SourceListIndex,
                            SourceElementIndex = usage.SourceElementIndex,
                            SourceFieldIndex = usage.SourceFieldIndex,
                            SourceListName = usage.SourceListName,
                            SourceItemId = usage.SourceItemId,
                            SourceItemName = usage.SourceItemName,
                            SourceFieldName = usage.SourceFieldName,
                            RawValue = usage.RawValue
                        });
                    }
                }

                clone[pair.Key] = usages;
            }

            return clone;
        }
    }
}

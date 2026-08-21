using System;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcLearnProducePortraitService
    {
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();

        public bool TryResolveLearnProducePortraitPath(
            eListCollection listCollection,
            CacheSave database,
            int learnProduceServiceId,
            out string mappedPath)
        {
            mappedPath = string.Empty;
            if (learnProduceServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            string rawIconValue;
            if (!TryFindNpcLearnProduceRow(listCollection, learnProduceServiceId, out npcListIndex, out rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortraitPath(database, rawIconValue, out _, out mappedPath);
        }

        public bool TryResolveLearnProducePortrait(
            eListCollection listCollection,
            CacheSave database,
            int learnProduceServiceId,
            out Bitmap icon)
        {
            icon = null;
            if (learnProduceServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            string rawIconValue;
            if (!TryFindNpcLearnProduceRow(listCollection, learnProduceServiceId, out npcListIndex, out rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortrait(database, listCollection, npcListIndex, rawIconValue, out icon);
        }

        private static bool TryFindNpcLearnProduceRow(
            eListCollection listCollection,
            int learnProduceServiceId,
            out int npcListIndex,
            out string rawIconValue)
        {
            npcListIndex = -1;
            rawIconValue = string.Empty;
            if (listCollection == null || learnProduceServiceId <= 0 || listCollection.Lists == null)
            {
                return false;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null
                    || list.elementFields == null
                    || list.elementValues == null
                    || !string.Equals(NormalizeListName(list.listName), "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int serviceFieldIndex = GetFieldIndex(list.elementFields, "id_learn_produce");
                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                if (serviceFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    int currentServiceId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, serviceFieldIndex), out currentServiceId)
                        || currentServiceId != learnProduceServiceId)
                    {
                        continue;
                    }

                    npcListIndex = listIndex;
                    rawIconValue = listCollection.GetValue(listIndex, rowIndex, iconFieldIndex);
                    return !string.IsNullOrWhiteSpace(rawIconValue);
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
                if (string.Equals(fields[i], fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetIconFieldIndex(string[] fields)
        {
            int primary = GetFieldIndex(fields, "file_icon");
            return primary >= 0 ? primary : GetFieldIndex(fields, "file_icon1");
        }

        private static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}

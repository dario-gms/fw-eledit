using System;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcTradePortraitService
    {
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();

        public bool TryResolveTradePortraitPath(
            eListCollection listCollection,
            CacheSave database,
            int tradeServiceId,
            out string mappedPath)
        {
            mappedPath = string.Empty;
            if (tradeServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            int elementIndex;
            string rawIconValue;
            if (!TryFindNpcTradeRow(listCollection, tradeServiceId, out npcListIndex, out elementIndex, out rawIconValue))
            {
                return false;
            }

            int pathId;
            return portraitIconService.TryResolvePortraitPath(database, rawIconValue, out pathId, out mappedPath);
        }

        public bool TryResolveTradePortrait(
            eListCollection listCollection,
            CacheSave database,
            int tradeServiceId,
            out Bitmap icon)
        {
            icon = null;
            if (tradeServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            int elementIndex;
            string rawIconValue;
            if (!TryFindNpcTradeRow(listCollection, tradeServiceId, out npcListIndex, out elementIndex, out rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortrait(database, listCollection, npcListIndex, rawIconValue, out icon);
        }

        private static bool TryFindNpcTradeRow(
            eListCollection listCollection,
            int tradeServiceId,
            out int npcListIndex,
            out int elementIndex,
            out string rawIconValue)
        {
            npcListIndex = -1;
            elementIndex = -1;
            rawIconValue = string.Empty;
            if (listCollection == null || tradeServiceId <= 0 || listCollection.Lists == null)
            {
                return false;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null || list.elementFields == null || list.elementValues == null)
                {
                    continue;
                }

                string listName = NormalizeListName(list.listName);
                if (!string.Equals(listName, "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int tradeFieldIndex = GetFieldIndex(list.elementFields, "id_item_trade_service");
                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                if (tradeFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    int currentTradeServiceId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, tradeFieldIndex), out currentTradeServiceId)
                        || currentTradeServiceId != tradeServiceId)
                    {
                        continue;
                    }

                    npcListIndex = listIndex;
                    elementIndex = rowIndex;
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

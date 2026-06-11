using System;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcSellPortraitService
    {
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();

        public bool TryResolveSellPortraitPath(
            eListCollection listCollection,
            CacheSave database,
            int sellServiceId,
            out string mappedPath)
        {
            mappedPath = string.Empty;
            if (sellServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            if (!TryFindNpcSellRow(listCollection, sellServiceId, out int npcListIndex, out _, out string rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortraitPath(database, rawIconValue, out _, out mappedPath);
        }

        public bool TryResolveSellPortrait(
            eListCollection listCollection,
            CacheSave database,
            int sellServiceId,
            out Bitmap icon)
        {
            icon = null;
            if (sellServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            if (!TryFindNpcSellRow(listCollection, sellServiceId, out int npcListIndex, out _, out string rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortrait(database, listCollection, npcListIndex, rawIconValue, out icon);
        }

        private static bool TryFindNpcSellRow(
            eListCollection listCollection,
            int sellServiceId,
            out int npcListIndex,
            out int elementIndex,
            out string rawIconValue)
        {
            npcListIndex = -1;
            elementIndex = -1;
            rawIconValue = string.Empty;
            if (listCollection == null || sellServiceId <= 0 || listCollection.Lists == null)
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

                if (!string.Equals(NormalizeListName(list.listName), "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int sellFieldIndex = GetFieldIndex(list.elementFields, "id_sell_service");
                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                if (sellFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, sellFieldIndex), out int currentSellServiceId)
                        || currentSellServiceId != sellServiceId)
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
            int iconFieldIndex = GetFieldIndex(fields, "file_icon");
            return iconFieldIndex >= 0 ? iconFieldIndex : GetFieldIndex(fields, "file_icon1");
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

using System;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcTransmitPortraitService
    {
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();

        public bool TryResolveTransmitPortraitPath(
            eListCollection listCollection,
            CacheSave database,
            int transmitServiceId,
            out string mappedPath)
        {
            mappedPath = string.Empty;
            if (transmitServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            string rawIconValue;
            if (!TryFindNpcTransmitRow(listCollection, transmitServiceId, out npcListIndex, out rawIconValue))
            {
                return false;
            }

            int pathId;
            return portraitIconService.TryResolvePortraitPath(database, rawIconValue, out pathId, out mappedPath);
        }

        public bool TryResolveTransmitPortrait(
            eListCollection listCollection,
            CacheSave database,
            int transmitServiceId,
            out Bitmap icon)
        {
            icon = null;
            if (transmitServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            string rawIconValue;
            if (!TryFindNpcTransmitRow(listCollection, transmitServiceId, out npcListIndex, out rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortrait(database, listCollection, npcListIndex, rawIconValue, out icon);
        }

        private static bool TryFindNpcTransmitRow(
            eListCollection listCollection,
            int transmitServiceId,
            out int npcListIndex,
            out string rawIconValue)
        {
            npcListIndex = -1;
            rawIconValue = string.Empty;
            if (listCollection == null || transmitServiceId <= 0 || listCollection.Lists == null)
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

                int transmitFieldIndex = GetFieldIndex(list.elementFields, "id_transmit_service");
                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                if (transmitFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    int currentTransmitServiceId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, transmitFieldIndex), out currentTransmitServiceId)
                        || currentTransmitServiceId != transmitServiceId)
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

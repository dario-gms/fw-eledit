using System;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcHotelPortraitService
    {
        private readonly CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();

        public bool TryResolveHotelPortraitPath(
            eListCollection listCollection,
            CacheSave database,
            int hotelServiceId,
            out string mappedPath)
        {
            mappedPath = string.Empty;
            if (hotelServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            string rawIconValue;
            if (!TryFindNpcHotelRow(listCollection, hotelServiceId, out npcListIndex, out rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortraitPath(database, rawIconValue, out _, out mappedPath);
        }

        public bool TryResolveHotelPortrait(
            eListCollection listCollection,
            CacheSave database,
            int hotelServiceId,
            out Bitmap icon)
        {
            icon = null;
            if (hotelServiceId <= 0 || listCollection == null || database == null)
            {
                return false;
            }

            int npcListIndex;
            string rawIconValue;
            if (!TryFindNpcHotelRow(listCollection, hotelServiceId, out npcListIndex, out rawIconValue))
            {
                return false;
            }

            return portraitIconService.TryResolvePortrait(database, listCollection, npcListIndex, rawIconValue, out icon);
        }

        private static bool TryFindNpcHotelRow(
            eListCollection listCollection,
            int hotelServiceId,
            out int npcListIndex,
            out string rawIconValue)
        {
            npcListIndex = -1;
            rawIconValue = string.Empty;
            if (listCollection == null || hotelServiceId <= 0 || listCollection.Lists == null)
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

                int hotelFieldIndex = GetFieldIndex(list.elementFields, "id_hotel_service");
                int iconFieldIndex = GetIconFieldIndex(list.elementFields);
                if (hotelFieldIndex < 0 || iconFieldIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    int currentHotelServiceId;
                    if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, hotelFieldIndex), out currentHotelServiceId)
                        || currentHotelServiceId != hotelServiceId)
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

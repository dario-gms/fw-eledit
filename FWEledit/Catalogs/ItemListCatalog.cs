using System;

namespace FWEledit
{
    public static class ItemListCatalog
    {
        public static bool IsItemList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            eList list = listCollection.Lists[listIndex];
            if (list == null)
            {
                return false;
            }

            string listName = NormalizeListName(list.listName);
            if (IsEquipmentEssenceAlias(listName))
            {
                return true;
            }
            if (IsMountEssenceAlias(listName))
            {
                return true;
            }
            if (IsSuiteEssenceAlias(listName))
            {
                return true;
            }

            string[] fields = list.elementFields;
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

        public static string NormalizeListName(string listName)
        {
            if (string.IsNullOrWhiteSpace(listName))
            {
                return string.Empty;
            }

            string[] split = listName.Split(new string[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }

        public static bool IsEquipmentEssenceAlias(string listName)
        {
            return string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMountEssenceAlias(string listName)
        {
            return string.Equals(listName, "Aircraft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Aircraft Essence", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "AIRCRAFT_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Vehicle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Vehicle Essence", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "VEHICLE_ESSENCE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSuiteEssenceAlias(string listName)
        {
            return string.Equals(listName, "Suite", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Suite Essence", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "SUITE_ESSENCE", StringComparison.OrdinalIgnoreCase);
        }
    }
}

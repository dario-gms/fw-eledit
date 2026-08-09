using System;
using System.Drawing;
using System.Globalization;

namespace FWEledit
{
    public static class EntityTypeColorCatalog
    {
        private static readonly Color NpcNameColor = Color.FromArgb(255, 190, 72);
        private static readonly Color MonsterLowLevelNameColor = Color.FromArgb(106, 158, 55);
        private static readonly Color MonsterHighLevelNameColor = Color.FromArgb(198, 176, 70);
        private static readonly Color MonsterVeryHighLevelNameColor = Color.FromArgb(218, 128, 52);
        private static readonly Color MonsterExtremeLevelNameColor = Color.FromArgb(222, 82, 82);

        public static bool TryGetNameColor(eListCollection listCollection, int listIndex, out Color color)
        {
            return TryGetNameColor(listCollection, listIndex, -1, out color);
        }

        public static bool TryGetNameColor(eListCollection listCollection, int listIndex, int elementIndex, out Color color)
        {
            color = Color.Empty;
            if (listCollection == null
                || listCollection.Lists == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex] == null)
            {
                return false;
            }

            string normalizedListName = NormalizeListName(listCollection.Lists[listIndex].listName);
            if (string.Equals(normalizedListName, "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                color = NpcNameColor;
                return true;
            }

            if (string.Equals(normalizedListName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase))
            {
                return TryGetMonsterEditorNameColor(listCollection, listIndex, elementIndex, out color);
            }

            return false;
        }

        private static bool TryGetMonsterEditorNameColor(eListCollection listCollection, int listIndex, int elementIndex, out Color color)
        {
            color = Color.Empty;
            if (elementIndex < 0
                || listCollection.Lists[listIndex].elementFields == null
                || listCollection.Lists[listIndex].elementValues == null
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            int level = GetFirstMonsterLevel(listCollection, listIndex, elementIndex);
            if (level <= 0)
            {
                return false;
            }

            if (level <= 50)
            {
                color = MonsterLowLevelNameColor;
                return true;
            }

            if (level <= 60)
            {
                return false;
            }

            if (level <= 80)
            {
                color = MonsterHighLevelNameColor;
                return true;
            }

            if (level <= 100)
            {
                color = MonsterVeryHighLevelNameColor;
                return true;
            }

            color = MonsterExtremeLevelNameColor;
            return true;
        }

        private static int GetFirstMonsterLevel(eListCollection listCollection, int listIndex, int elementIndex)
        {
            int level = 0;
            string[] preferredFields = new string[] { "level_1", "level_0" };
            for (int i = 0; i < preferredFields.Length; i++)
            {
                int index = GetFieldIndex(listCollection.Lists[listIndex].elementFields, preferredFields[i]);
                if (index >= 0 && TryParsePositiveInt(listCollection.GetValue(listIndex, elementIndex, index), out level))
                {
                    return level;
                }
            }

            for (int i = 0; i < listCollection.Lists[listIndex].elementFields.Length; i++)
            {
                string fieldName = listCollection.Lists[listIndex].elementFields[i] ?? string.Empty;
                if (fieldName.StartsWith("level_", StringComparison.OrdinalIgnoreCase)
                    && TryParsePositiveInt(listCollection.GetValue(listIndex, elementIndex, i), out level))
                {
                    return level;
                }
            }

            return 0;
        }

        private static bool TryParsePositiveInt(string rawValue, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return false;
            }

            if (!int.TryParse(rawValue.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            return value > 0;
        }

        private static int GetFieldIndex(string[] fields, string fieldName)
        {
            if (fields == null || string.IsNullOrWhiteSpace(fieldName))
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

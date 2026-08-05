using System;
using System.Drawing;

namespace FWEledit
{
    public static class EntityTypeColorCatalog
    {
        private static readonly Color NpcNameColor = Color.FromArgb(255, 190, 72);

        public static bool TryGetNameColor(eListCollection listCollection, int listIndex, out Color color)
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

            return false;
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

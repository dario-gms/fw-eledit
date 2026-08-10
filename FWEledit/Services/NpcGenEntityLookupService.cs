using System;
using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class NpcGenEntityInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Bitmap Icon { get; set; }
        public string SourceList { get; set; }
        public Color? NameColor { get; set; }
        public int ListIndex { get; set; }
        public int ElementIndex { get; set; }
    }

    public sealed class NpcGenEntityLookupService
    {
        private readonly IconResolutionService iconResolutionService = new IconResolutionService();
        private readonly CreaturePortraitIconService creaturePortraitIconService = new CreaturePortraitIconService();
        private eListCollection cachedListCollection;
        private CacheSave cachedDatabase;
        private Dictionary<int, NpcGenEntityInfo> entitiesById;

        public NpcGenEntityInfo Resolve(eListCollection listCollection, CacheSave database, int id)
        {
            EnsureIndex(listCollection, database);
            NpcGenEntityInfo info;
            if (entitiesById != null && entitiesById.TryGetValue(id, out info))
            {
                return info;
            }
            return new NpcGenEntityInfo
            {
                Id = id,
                Name = id == 0 ? "NONE!" : string.Empty,
                Icon = Properties.Resources.blank,
                SourceList = string.Empty,
                ListIndex = -1,
                ElementIndex = -1
            };
        }

        private void EnsureIndex(eListCollection listCollection, CacheSave database)
        {
            if (ReferenceEquals(cachedListCollection, listCollection)
                && ReferenceEquals(cachedDatabase, database)
                && entitiesById != null)
            {
                return;
            }

            cachedListCollection = listCollection;
            cachedDatabase = database;
            entitiesById = new Dictionary<int, NpcGenEntityInfo>();

            if (listCollection == null || listCollection.Lists == null)
            {
                return;
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                eList list = listCollection.Lists[listIndex];
                if (list == null || list.elementFields == null || list.elementValues == null)
                {
                    continue;
                }

                string listName = NormalizeListName(list.listName);
                if (!string.Equals(listName, "NPC_ESSENCE", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(listName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int idIndex = GetFieldIndex(list.elementFields, "id");
                int nameIndex = GetFieldIndex(list.elementFields, "name");
                int iconIndex = GetIconFieldIndex(list.elementFields);
                if (idIndex < 0)
                {
                    continue;
                }

                for (int rowIndex = 0; rowIndex < list.elementValues.Length; rowIndex++)
                {
                    int id;
                    if (!int.TryParse(listCollection.GetValue(listIndex, rowIndex, idIndex), out id) || id <= 0)
                    {
                        continue;
                    }
                    if (entitiesById.ContainsKey(id))
                    {
                        continue;
                    }

                    Bitmap icon = Properties.Resources.blank;
                    if (database != null && iconIndex >= 0)
                    {
                        string rawIcon = listCollection.GetValue(listIndex, rowIndex, iconIndex);
                        Bitmap portrait;
                        if (creaturePortraitIconService.TryResolvePortrait(database, listCollection, listIndex, rawIcon, out portrait)
                            && portrait != null)
                        {
                            icon = portrait;
                        }
                        else
                        {
                            string iconKey = iconResolutionService.ResolveIconKeyForList(database, listCollection, listIndex, rawIcon);
                            if (!string.IsNullOrWhiteSpace(iconKey))
                            {
                                icon = database.images(iconKey);
                            }
                        }
                    }

                    Color nameColor;
                    Color? resolvedNameColor = EntityTypeColorCatalog.TryGetNameColor(listCollection, listIndex, rowIndex, out nameColor)
                        ? (Color?)nameColor
                        : null;

                    entitiesById[id] = new NpcGenEntityInfo
                    {
                        Id = id,
                        Name = nameIndex >= 0 ? listCollection.GetValue(listIndex, rowIndex, nameIndex) : string.Empty,
                        Icon = icon,
                        SourceList = listName,
                        NameColor = resolvedNameColor,
                        ListIndex = listIndex,
                        ElementIndex = rowIndex
                    };
                }
            }
        }

        private static int GetIconFieldIndex(string[] fields)
        {
            int index = GetFieldIndex(fields, "file_icon");
            return index >= 0 ? index : GetFieldIndex(fields, "file_icon1");
        }

        private static int GetFieldIndex(string[] fields, string name)
        {
            if (fields == null)
            {
                return -1;
            }
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], name, StringComparison.OrdinalIgnoreCase))
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
            string[] split = listName.Split(new[] { " - " }, StringSplitOptions.None);
            return split.Length > 1 ? split[1].Trim() : listName.Trim();
        }
    }
}

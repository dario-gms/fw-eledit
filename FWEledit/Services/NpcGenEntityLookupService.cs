using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

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
        public long? ZoneMask { get; set; }
        public int? AiScriptId { get; set; }
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

        public List<NpcGenEntityInfo> FindByAiScript(eListCollection listCollection, CacheSave database, int aiScriptId)
        {
            EnsureIndex(listCollection, database);
            if (entitiesById == null || aiScriptId <= 0)
            {
                return new List<NpcGenEntityInfo>();
            }

            return entitiesById.Values
                .Where(info => info.AiScriptId.HasValue && info.AiScriptId.Value == aiScriptId)
                .OrderBy(info => info.Id)
                .ToList();
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
                int zoneMaskIndex = GetFieldIndex(list.elementFields, "server_zone_mask");
                int aiScriptIndex = GetAiScriptFieldIndex(list.elementFields, listName);
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
                        ElementIndex = rowIndex,
                        ZoneMask = zoneMaskIndex >= 0 ? TryParseZoneMask(listCollection.GetValue(listIndex, rowIndex, zoneMaskIndex)) : null,
                        AiScriptId = aiScriptIndex >= 0 ? TryParseAiScriptId(listCollection.GetValue(listIndex, rowIndex, aiScriptIndex)) : null
                    };
                }
            }
        }

        private static int? TryParseAiScriptId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            int parsed;
            if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) || parsed == 0)
            {
                return null;
            }

            return Math.Abs(parsed);
        }

        private static long? TryParseZoneMask(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            long signedValue;
            if (long.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out signedValue))
            {
                return signedValue;
            }

            ulong unsignedValue;
            if (ulong.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out unsignedValue))
            {
                return unsignedValue > long.MaxValue ? -1L : (long)unsignedValue;
            }

            return null;
        }

        private static int GetIconFieldIndex(string[] fields)
        {
            int index = GetFieldIndex(fields, "file_icon");
            return index >= 0 ? index : GetFieldIndex(fields, "file_icon1");
        }

        private static int GetAiScriptFieldIndex(string[] fields, string listName)
        {
            int index = GetFieldIndex(fields, "id_ai_script");
            if (index >= 0)
            {
                return index;
            }

            index = GetFieldIndex(fields, "ai_script");
            if (index >= 0)
            {
                return index;
            }

            index = GetFieldIndex(fields, "id_strategy");
            if (index >= 0)
            {
                return index;
            }

            return string.Equals(listName, "MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase)
                ? GetFieldIndex(fields, "Strategy")
                : -1;
        }

        private static int GetFieldIndex(string[] fields, string name)
        {
            if (fields == null)
            {
                return -1;
            }
            string normalizedName = NormalizeFieldName(name);
            for (int i = 0; i < fields.Length; i++)
            {
                if (string.Equals(fields[i], name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(NormalizeFieldName(fields[i]), normalizedName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        private static string NormalizeFieldName(string fieldName)
        {
            return string.IsNullOrWhiteSpace(fieldName)
                ? string.Empty
                : fieldName.Replace(" ", string.Empty).Replace("_", string.Empty).Trim();
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

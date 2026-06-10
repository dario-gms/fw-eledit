using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public sealed class SkillReferenceOption
    {
        public int Value { get; set; }
        public string Label { get; set; }
        public string Description { get; set; }
        public string Kind { get; set; }

        public override string ToString()
        {
            return Value.ToString(CultureInfo.InvariantCulture) + " - " + (Label ?? string.Empty);
        }
    }

    public static class SkillReferenceCatalog
    {
        private static readonly object SyncRoot = new object();
        private static string[] cachedSource = new string[0];
        private static string[] cachedBuffSource = new string[0];
        private static List<SkillReferenceOption> cachedOptions = new List<SkillReferenceOption>();
        private static Dictionary<int, SkillReferenceOption> cachedOptionsById = new Dictionary<int, SkillReferenceOption>();

        public static bool IsSkillFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            if (normalized.IndexOf("service", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (string.Equals(normalized, "id_skill", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "skill_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_skill_result", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_skill_require", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "id_skill_required", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.StartsWith("id_skill_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (normalized.EndsWith("_skill_id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return normalized.IndexOf("addon_skills_", StringComparison.OrdinalIgnoreCase) >= 0
                && normalized.EndsWith("_id", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(CacheSave database, string rawValue)
        {
            int skillId;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out skillId) || skillId <= 0)
            {
                return rawValue ?? string.Empty;
            }

            SkillReferenceOption option;
            if (TryGetOption(database, skillId, out option) && option != null && !string.IsNullOrWhiteSpace(option.Label))
            {
                return option.Label;
            }

            return rawValue ?? string.Empty;
        }

        public static string FormatDisplay(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            string fieldName,
            CacheSave database,
            string rawValue)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return FormatDisplay(database, rawValue);
            }

            string normalizedField = fieldName.Trim();
            if (IsSkillMatterList(listCollection, listIndex))
            {
                if (string.Equals(normalizedField, "id_skill", StringComparison.OrdinalIgnoreCase))
                {
                    return FormatSkillMatterSkill(listCollection, listIndex, elementIndex, database, rawValue);
                }

                if (string.Equals(normalizedField, "level_skill", StringComparison.OrdinalIgnoreCase))
                {
                    return FormatSkillMatterLevel(rawValue);
                }

                if (string.Equals(normalizedField, "cast_skill", StringComparison.OrdinalIgnoreCase))
                {
                    return FormatSkillMatterCast(rawValue);
                }

                if (string.Equals(normalizedField, "skill_matter_type", StringComparison.OrdinalIgnoreCase))
                {
                    return FormatSkillMatterType(rawValue);
                }
            }

            return FormatDisplay(database, rawValue);
        }

        public static string NormalizeInput(CacheSave database, string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            int numericValue;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue)
                || int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.CurrentCulture, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            if (TryParseLeadingId(trimmed, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            List<SkillReferenceOption> options = BuildOptions(database);
            for (int i = 0; i < options.Count; i++)
            {
                SkillReferenceOption option = options[i];
                if (option == null)
                {
                    continue;
                }

                if (string.Equals(option.Label ?? string.Empty, trimmed, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(option.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return option.Value.ToString(CultureInfo.InvariantCulture);
                }
            }

            return trimmed;
        }

        public static bool TryGetOption(CacheSave database, int skillId, out SkillReferenceOption option)
        {
            option = null;
            if (skillId <= 0)
            {
                return false;
            }

            EnsureCache(database);
            lock (SyncRoot)
            {
                return cachedOptionsById.TryGetValue(skillId, out option);
            }
        }

        public static List<SkillReferenceOption> BuildOptions(CacheSave database)
        {
            EnsureCache(database);
            lock (SyncRoot)
            {
                return new List<SkillReferenceOption>(cachedOptions);
            }
        }

        private static void EnsureCache(CacheSave database)
        {
            string[] source = database != null && database.skillstr != null
                ? database.skillstr
                : new string[0];
            string[] buffSource = database != null && database.buff_str != null
                ? database.buff_str
                : new string[0];

            lock (SyncRoot)
            {
                if (object.ReferenceEquals(cachedSource, source)
                    && object.ReferenceEquals(cachedBuffSource, buffSource)
                    && cachedOptions.Count > 0)
                {
                    return;
                }

                cachedSource = source;
                cachedBuffSource = buffSource;
                cachedOptions = new List<SkillReferenceOption>();
                cachedOptionsById = new Dictionary<int, SkillReferenceOption>();

                for (int i = 0; i + 3 < source.Length; i += 4)
                {
                    int rawNameId;
                    if (!int.TryParse((source[i] ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out rawNameId))
                    {
                        continue;
                    }

                    int skillId = rawNameId / 10;
                    if (skillId <= 0)
                    {
                        continue;
                    }

                    string name = CleanText(source[i + 1]);
                    string description = CleanText(source[i + 3]);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = "Skill " + skillId.ToString(CultureInfo.InvariantCulture);
                    }

                    SkillReferenceOption option;
                    if (!cachedOptionsById.TryGetValue(skillId, out option))
                    {
                        option = new SkillReferenceOption
                        {
                            Value = skillId,
                            Label = name,
                            Description = description,
                            Kind = "Skill"
                        };
                        cachedOptionsById[skillId] = option;
                        cachedOptions.Add(option);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(option.Label) && !string.IsNullOrWhiteSpace(name))
                        {
                            option.Label = name;
                        }
                        if (string.IsNullOrWhiteSpace(option.Description) && !string.IsNullOrWhiteSpace(description))
                        {
                            option.Description = description;
                        }
                        if (string.IsNullOrWhiteSpace(option.Kind))
                        {
                            option.Kind = "Skill";
                        }
                    }
                }

                for (int i = 0; i + 1 < buffSource.Length; i += 2)
                {
                    int buffId;
                    if (!int.TryParse((buffSource[i] ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out buffId)
                        || buffId <= 0)
                    {
                        continue;
                    }

                    string name = CleanText(buffSource[i + 1]);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = "Buff " + buffId.ToString(CultureInfo.InvariantCulture);
                    }

                    SkillReferenceOption option;
                    if (!cachedOptionsById.TryGetValue(buffId, out option))
                    {
                        option = new SkillReferenceOption
                        {
                            Value = buffId,
                            Label = name,
                            Description = string.Empty,
                            Kind = "Buff"
                        };
                        cachedOptionsById[buffId] = option;
                        cachedOptions.Add(option);
                    }
                    else if (string.IsNullOrWhiteSpace(option.Label))
                    {
                        option.Label = name;
                        if (string.IsNullOrWhiteSpace(option.Kind))
                        {
                            option.Kind = "Buff";
                        }
                    }
                }

                cachedOptions.Sort(delegate (SkillReferenceOption left, SkillReferenceOption right)
                {
                    string leftLabel = left != null ? left.Label ?? string.Empty : string.Empty;
                    string rightLabel = right != null ? right.Label ?? string.Empty : string.Empty;
                    int compare = string.Compare(leftLabel, rightLabel, StringComparison.OrdinalIgnoreCase);
                    if (compare != 0)
                    {
                        return compare;
                    }

                    int leftValue = left != null ? left.Value : 0;
                    int rightValue = right != null ? right.Value : 0;
                    return leftValue.CompareTo(rightValue);
                });
            }
        }

        private static string CleanText(string text)
        {
            string cleaned = text ?? string.Empty;
            try
            {
                cleaned = Extensions.ColorClean(cleaned);
            }
            catch
            {
            }

            cleaned = cleaned
                .Replace("\\r", Environment.NewLine)
                .Replace("\\n", Environment.NewLine)
                .Replace("\\t", "\t")
                .Replace("%%", "%")
                .Replace("\r", Environment.NewLine)
                .Trim();

            return cleaned;
        }

        private static bool TryParseLeadingId(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            int separator = text.IndexOf(" - ", StringComparison.OrdinalIgnoreCase);
            if (separator <= 0)
            {
                return false;
            }

            string candidate = text.Substring(0, separator).Trim();
            return int.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                || int.TryParse(candidate, NumberStyles.Integer, CultureInfo.CurrentCulture, out value);
        }

        private static bool IsSkillMatterList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            return listName.IndexOf("SKILLMATTER_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatSkillMatterSkill(
            eListCollection listCollection,
            int listIndex,
            int elementIndex,
            CacheSave database,
            string rawValue)
        {
            string label = FormatDisplay(database, rawValue);
            int level = TryGetSiblingIntValue(listCollection, listIndex, elementIndex, "level_skill");
            int castSkill = TryGetSiblingIntValue(listCollection, listIndex, elementIndex, "cast_skill");

            string prefix = castSkill != 0 ? "Use skill" : "Linked skill";
            if (string.IsNullOrWhiteSpace(label) || string.Equals(label, rawValue ?? string.Empty, StringComparison.Ordinal))
            {
                label = "ID " + (rawValue ?? "0");
            }

            if (level > 0)
            {
                return prefix + ": " + label + " (Lv " + level.ToString(CultureInfo.InvariantCulture) + ")";
            }

            return prefix + ": " + label;
        }

        private static string FormatSkillMatterLevel(string rawValue)
        {
            int level;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out level))
            {
                return rawValue ?? string.Empty;
            }

            return level <= 0 ? "Lv 0" : "Lv " + level.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatSkillMatterCast(string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            return value != 0 ? "Casts the configured skill on use" : "Passive or linked effect only";
        }

        private static string FormatSkillMatterType(string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            switch (value)
            {
                case 0:
                    return "General skill item";
                case 1:
                    return "HP medicine";
                case 2:
                    return "MP medicine";
                case 3:
                    return "HP and MP medicine";
                case 4:
                    return "HP food";
                case 5:
                    return "MP food";
                case 6:
                    return "HP and MP food";
                default:
                    return "Type " + value.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static int TryGetSiblingIntValue(eListCollection listCollection, int listIndex, int elementIndex, string siblingFieldName)
        {
            if (listCollection == null
                || string.IsNullOrWhiteSpace(siblingFieldName)
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || elementIndex < 0
                || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return 0;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            if (fields == null)
            {
                return 0;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (!string.Equals(fields[i], siblingFieldName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int value;
                return int.TryParse(listCollection.GetValue(listIndex, elementIndex, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
                    ? value
                    : 0;
            }

            return 0;
        }
    }
}

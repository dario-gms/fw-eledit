using System;
using System.Collections.Generic;

namespace FWEledit
{
    public static class ModelFieldLabelCatalog
    {
        private static readonly Dictionary<string, string> AircraftVehicleRaceLabels =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "file_models_1", "file_models_human" },
                { "file_models_2", "file_models_elf" },
                { "file_models_3", "file_models_dwarf" },
                { "file_models_4", "file_models_stoneman" },
                { "file_models_5", "file_models_kindred" },
                { "file_models_6", "file_models_lycan" },
                { "model_name_1", "model_name_human" },
                { "model_name_2", "model_name_elf" },
                { "model_name_3", "model_name_dwarf" },
                { "model_name_4", "model_name_stoneman" },
                { "model_name_5", "model_name_kindred" },
                { "model_name_6", "model_name_lycan" }
            };

        public static string GetDisplayFieldName(
            eListCollection listCollection,
            string listName,
            int listIndex,
            int fieldIndex,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return string.Empty;
            }

            string normalizedFieldName = fieldName.Trim();
            if (NpcSellServiceCatalog.IsNpcSellServiceList(listCollection, listIndex))
            {
                return NpcSellServiceCatalog.GetDisplayFieldName(listCollection, listIndex, fieldIndex, normalizedFieldName);
            }

            if ((listIndex == 21 || listIndex == 77)
                && AircraftVehicleRaceLabels.TryGetValue(normalizedFieldName, out string displayFieldName))
            {
                return displayFieldName;
            }

            string statFieldName = GetCombatStatDisplayFieldName(listCollection, listIndex, fieldIndex, normalizedFieldName);
            if (!string.IsNullOrWhiteSpace(statFieldName))
            {
                return statFieldName;
            }

            return normalizedFieldName;
        }

        private static string GetCombatStatDisplayFieldName(eListCollection listCollection, int listIndex, int fieldIndex, string fieldName)
        {
            if (listCollection == null
                || listIndex < 0
                || listIndex >= listCollection.Lists.Length
                || listCollection.Lists[listIndex].elementFields == null
                || fieldIndex < 0)
            {
                return string.Empty;
            }

            string[] fields = listCollection.Lists[listIndex].elementFields;
            if (fieldIndex >= fields.Length)
            {
                return string.Empty;
            }

            if (string.Equals(fieldName, "attack", StringComparison.OrdinalIgnoreCase)
                && IsCombatAccuracyField(fields, fieldIndex, string.Empty))
            {
                return "accuracy";
            }

            if (string.Equals(fieldName, "armor", StringComparison.OrdinalIgnoreCase)
                && IsCombatEvasionField(fields, fieldIndex, string.Empty))
            {
                return "evasion";
            }

            if (string.Equals(fieldName, "lvlup_attack", StringComparison.OrdinalIgnoreCase)
                && IsCombatAccuracyField(fields, fieldIndex, "lvlup_"))
            {
                return "lvlup_accuracy";
            }

            if (string.Equals(fieldName, "lvlup_armor", StringComparison.OrdinalIgnoreCase)
                && IsCombatEvasionField(fields, fieldIndex, "lvlup_"))
            {
                return "lvlup_evasion";
            }

            return string.Empty;
        }

        private static bool IsCombatAccuracyField(string[] fields, int fieldIndex, string prefix)
        {
            if (!IsFieldAt(fields, fieldIndex + 1, prefix + "armor")
                || !HasFieldNearBefore(fields, fieldIndex, prefix + "defense", prefix + "defence", 4))
            {
                return false;
            }

            return HasFieldNearAfter(fields, fieldIndex, prefix + "crit_rate", 4)
                || IsFieldAt(fields, fieldIndex - 1, prefix + "defense")
                || IsFieldAt(fields, fieldIndex - 1, prefix + "defence");
        }

        private static bool IsCombatEvasionField(string[] fields, int fieldIndex, string prefix)
        {
            if (!IsFieldAt(fields, fieldIndex - 1, prefix + "attack")
                || !HasFieldNearBefore(fields, fieldIndex, prefix + "defense", prefix + "defence", 5))
            {
                return false;
            }

            return HasFieldNearAfter(fields, fieldIndex, prefix + "crit_rate", 3)
                || IsFieldAt(fields, fieldIndex - 2, prefix + "defense")
                || IsFieldAt(fields, fieldIndex - 2, prefix + "defence");
        }

        private static bool HasFieldNearBefore(string[] fields, int fieldIndex, string name1, string name2, int maxDistance)
        {
            int start = Math.Max(0, fieldIndex - maxDistance);
            for (int i = fieldIndex - 1; i >= start; i--)
            {
                if (IsFieldAt(fields, i, name1) || IsFieldAt(fields, i, name2))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFieldNearAfter(string[] fields, int fieldIndex, string name1, string name2, int maxDistance)
        {
            int end = Math.Min(fields.Length - 1, fieldIndex + maxDistance);
            for (int i = fieldIndex + 1; i <= end; i++)
            {
                if (IsFieldAt(fields, i, name1) || IsFieldAt(fields, i, name2))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasFieldNearAfter(string[] fields, int fieldIndex, string name, int maxDistance)
        {
            return HasFieldNearAfter(fields, fieldIndex, name, name, maxDistance);
        }

        private static bool IsFieldAt(string[] fields, int index, string expected)
        {
            return fields != null
                && index >= 0
                && index < fields.Length
                && string.Equals(fields[index], expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}

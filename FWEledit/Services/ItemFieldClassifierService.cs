using System;

namespace FWEledit
{
    public sealed class ItemFieldClassifierService
    {
        public bool IsIconFieldName(string fieldName)
        {
            return string.Equals(fieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "file_icon1", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsItemQualityFieldName(string fieldName)
        {
            return string.Equals(fieldName, "item_quality", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsModelFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("file_models", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("model_name", StringComparison.OrdinalIgnoreCase)
                // Legacy FW lists encode model fields as "..._file_model_..."
                // (for example: models_1_file_model_male, data_1_race_data_1_file_model_female).
                || normalized.IndexOf("_file_model_", StringComparison.OrdinalIgnoreCase) >= 0
                // PET_BEDGE_ESSENCE uses these model display slots.
                || normalized.StartsWith("file_to_shown_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_default_weapon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_default_weapon_l", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsAddonTypeField(eListCollection listCollection, int listIndex, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            if (!string.Equals(fieldName.Trim(), "type", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }
            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            if (string.Equals(listName, "EQUIPMENT_ADDON", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return listIndex == 0;
        }

        public bool IsModelUsageFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            if (IsModelFieldName(fieldName))
            {
                return true;
            }
            return fieldName.Trim().StartsWith("gfx_file", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsShiftedModelFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string normalized = fieldName.Trim();
            return normalized.StartsWith("file_models_", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("model_name_", StringComparison.OrdinalIgnoreCase)
                || normalized.IndexOf("_file_model_", StringComparison.OrdinalIgnoreCase) >= 0
                || normalized.StartsWith("file_to_shown_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_default_weapon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "file_default_weapon_l", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsShiftedModelFieldName(string fieldName, string listName)
        {
            if (IsShiftedModelFieldName(fieldName))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            if (!string.Equals(fieldName.Trim(), "file_model", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string normalizedListName = (listName ?? string.Empty).Trim();
            return normalizedListName.IndexOf("NPC_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0
                || normalizedListName.IndexOf("MONSTER_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

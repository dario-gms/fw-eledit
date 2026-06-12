using System;
using System.Collections.Generic;
using System.Globalization;

namespace FWEledit
{
    public static class RandomGiftBagRewardTypeCatalog
    {
        public const int NoneValue = 0;
        public const int ItemValue = 1;
        public const int TitleValue = 4;

        private static readonly List<QualityOption> RewardTypeOptions = new List<QualityOption>
        {
            new QualityOption { Value = NoneValue, Label = "None" },
            new QualityOption { Value = ItemValue, Label = "Item" },
            new QualityOption { Value = TitleValue, Label = "Title" }
        };

        public static List<QualityOption> Options
        {
            get { return RewardTypeOptions; }
        }

        public static bool IsRewardTypeFieldName(eListCollection listCollection, int listIndex, string fieldName)
        {
            if (!IsRandomGiftBagList(listCollection, listIndex) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string typeFieldName;
            return TryParseRewardTypeFieldName(fieldName, out typeFieldName);
        }

        public static bool IsRewardIdFieldName(eListCollection listCollection, int listIndex, string fieldName)
        {
            if (!IsRandomGiftBagList(listCollection, listIndex) || string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string typeFieldName;
            return TryGetRewardTypeFieldNameForIdField(fieldName, out typeFieldName);
        }

        public static bool TryGetRewardTypeFieldNameForIdField(string rewardIdFieldName, out string rewardTypeFieldName)
        {
            rewardTypeFieldName = string.Empty;
            if (string.IsNullOrWhiteSpace(rewardIdFieldName))
            {
                return false;
            }

            string[] parts = rewardIdFieldName.Trim().Split('_');
            if (parts.Length == 4
                && string.Equals(parts[0], "reward", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[3], "id", StringComparison.OrdinalIgnoreCase))
            {
                rewardTypeFieldName = "reward_" + parts[1] + "_1_type";
                return true;
            }

            if (parts.Length == 3
                && string.Equals(parts[0], "reward", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[2], "id", StringComparison.OrdinalIgnoreCase))
            {
                rewardTypeFieldName = "reward_" + parts[1] + "_type";
                return true;
            }

            return false;
        }

        public static string FormatDisplay(string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            string label;
            if (TryGetLabel(value, out label))
            {
                return value.ToString(CultureInfo.InvariantCulture) + " - " + label;
            }

            return value.ToString(CultureInfo.InvariantCulture) + " - Unknown reward type";
        }

        public static bool TryGetLabel(int value, out string label)
        {
            for (int i = 0; i < RewardTypeOptions.Count; i++)
            {
                QualityOption option = RewardTypeOptions[i];
                if (option != null && option.Value == value)
                {
                    label = option.Label ?? string.Empty;
                    return true;
                }
            }

            label = string.Empty;
            return false;
        }

        public static string NormalizeInput(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            int numericValue;
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericValue))
            {
                return numericValue.ToString(CultureInfo.InvariantCulture);
            }

            string normalized = trimmed
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .ToLowerInvariant();

            if (normalized == "none" || normalized == "empty" || normalized == "disabled")
            {
                return NoneValue.ToString(CultureInfo.InvariantCulture);
            }

            if (normalized == "item" || normalized == "items")
            {
                return ItemValue.ToString(CultureInfo.InvariantCulture);
            }

            if (normalized == "title" || normalized == "titles")
            {
                return TitleValue.ToString(CultureInfo.InvariantCulture);
            }

            return trimmed;
        }

        private static bool TryParseRewardTypeFieldName(string fieldName, out string normalizedFieldName)
        {
            normalizedFieldName = string.Empty;
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string[] parts = fieldName.Trim().Split('_');
            if (parts.Length == 4
                && string.Equals(parts[0], "reward", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[3], "type", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFieldName = "reward_" + parts[1] + "_" + parts[2] + "_type";
                return true;
            }

            if (parts.Length == 3
                && string.Equals(parts[0], "reward", StringComparison.OrdinalIgnoreCase)
                && string.Equals(parts[2], "type", StringComparison.OrdinalIgnoreCase))
            {
                normalizedFieldName = "reward_" + parts[1] + "_type";
                return true;
            }

            return false;
        }

        private static bool IsRandomGiftBagList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }

            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            return listName.IndexOf("RANDOM_GIFT_BAG_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

using System;
using System.Globalization;

namespace FWEledit
{
    public static class BooleanFlagCatalog
    {
        public static bool IsYesNoFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return string.Equals(normalized, "is_forbid_transfer_ehance", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_forbid_transfer_enhance", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_alpha_fashion_equip", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "can_decompose", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "can_auction", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "sell_for_bind_money", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_category", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_shared_in_team", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_hared_by_team", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_boss", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_fly_state", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_exp_affected_by_kill_num", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "has_attack_behavior", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_collide_monster", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "hide_aggressive_tag", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "show_on_minimap", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "show_injured_info", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "show_damage_info", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "can_selected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "can_select_by_tab", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "auto_lock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "show_hint", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "return_to_ori_place_after_battle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "is_fast", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "aggressive_mode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "patroll_mode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "patrol_mode", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "can_attack", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "hide_name", StringComparison.OrdinalIgnoreCase);
        }

        public static string FormatDisplay(string rawValue)
        {
            int value;
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                return rawValue ?? string.Empty;
            }

            return value == 0 ? "No" : "Yes";
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
                return numericValue == 0 ? "0" : "1";
            }

            string normalized = trimmed
                .Replace("-", string.Empty)
                .Replace("_", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();

            if (normalized == "yes" || normalized == "true" || normalized == "enabled" || normalized == "enable")
            {
                return "1";
            }

            if (normalized == "no" || normalized == "false" || normalized == "disabled" || normalized == "disable")
            {
                return "0";
            }

            return trimmed;
        }

        public static string ToggleInput(string value)
        {
            string normalized = NormalizeInput(value);
            return string.Equals(normalized, "1", StringComparison.Ordinal) ? "0" : "1";
        }
    }
}

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

            string normalized = NormalizeFieldName(fieldName);
            return normalized == "is_forbid_transfer_ehance"
                || normalized == "is_forbid_transfer_enhance"
                || normalized == "bind_return_town"
                || normalized == "is_undercity"
                || normalized == "is_used_for_transfer_world"
                || normalized == "is_alpha_fashion_equip"
                || normalized == "is_two_player"
                || normalized == "can_change_color"
                || normalized == "can_decompose"
                || normalized == "can_auction"
                || normalized == "sell_for_bind_money"
                || normalized == "is_category"
                || normalized == "is_shared_in_team"
                || normalized == "is_hared_by_team"
                || normalized == "is_boss"
                || normalized == "is_fly_state"
                || normalized == "is_exp_affected_by_kill_num"
                || normalized == "has_attack_behavior"
                || normalized == "is_collide_monster"
                || normalized == "hide_aggressive_tag"
                || normalized == "show_on_minimap"
                || normalized == "show_injured_info"
                || normalized == "show_damage_info"
                || normalized == "can_selected"
                || normalized == "can_select_by_tab"
                || normalized == "auto_lock"
                || normalized == "show_hint"
                || normalized == "return_to_ori_place_after_battle"
                || normalized == "is_fast"
                || normalized == "stand_mode"
                || normalized == "aggressive_mode"
                || normalized == "patroll_mode"
                || normalized == "patrol_mode"
                || normalized == "can_attack"
                || normalized == "hide_name";
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

        private static string NormalizeFieldName(string fieldName)
        {
            return (fieldName ?? string.Empty)
                .Trim()
                .Replace(' ', '_')
                .Replace('-', '_')
                .ToLowerInvariant();
        }
    }
}

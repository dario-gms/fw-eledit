using System;

namespace FWEledit
{
    public sealed class EquipmentFieldService
    {
        public bool IsEquipmentEssenceList(eListCollection listCollection, int listIndex)
        {
            if (listCollection == null || listIndex < 0 || listIndex >= listCollection.Lists.Length)
            {
                return false;
            }
            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            return string.Equals(listName, "EQUIPMENT_ESSENCE", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listName, "Equipment", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEquipmentModelsField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string name = fieldName.Trim();
            return name.StartsWith("models_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("model_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("file_model", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_change_model", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEquipmentRefineField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string name = fieldName.Trim();
            return name.StartsWith("refine_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("level_stone_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("id_estone_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("enhanced_prop_package_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_sign_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "refine_max_level", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_identify", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "basic_show_level", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "can_sign", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEquipmentOtherField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }
            string name = fieldName.Trim();
            return name.StartsWith("color_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("decompose_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("max_recast_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("min_recast_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("extend_identify_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("auction_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("fashion_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("gfx_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("id_full_pvp_", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("id_cancel_full_pvp_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "can_decompose", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "can_auction", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "auction_fee", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "show_gfx_need_gem_value", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "id_special_addon_package", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "equip_transform_cfg_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "fashion_dye_cfg_id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "fashion_adorn_hook_name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "unknown_608_1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "pile_num_max", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "proc_type", StringComparison.OrdinalIgnoreCase);
        }
    }
}

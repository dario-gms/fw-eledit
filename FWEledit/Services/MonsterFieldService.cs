using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MonsterFieldService
    {
        public bool IsMonsterEssenceList(eListCollection listCollection, int listIndex)
        {
            return MonsterFieldCatalog.IsMonsterEssenceList(listCollection, listIndex);
        }

        public bool IsMonsterMasteryField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("mastery_", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("resistance_", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsMonsterLevelUpField(string fieldName)
        {
            return !string.IsNullOrWhiteSpace(fieldName)
                && fieldName.Trim().StartsWith("lvlup_", StringComparison.OrdinalIgnoreCase);
        }

        public bool IsMonsterOtherField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim();
            return normalized.StartsWith("soul_drop_", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("catch_pet_", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("heart_attack_", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith("level_up_increase_heart_attack_", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "attack_skill", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "born_action", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "inborn_skill", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "resource_value", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "score_value", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "monster_pet_type", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "exp_reward", StringComparison.OrdinalIgnoreCase);
        }

        public bool ShouldIncludeField(eListCollection listCollection, int listIndex, string fieldName, EquipmentValuesTab tab)
        {
            if (!IsMonsterEssenceList(listCollection, listIndex))
            {
                return true;
            }

            if (tab == EquipmentValuesTab.All)
            {
                return true;
            }

            bool mastery = IsMonsterMasteryField(fieldName);
            bool levelUp = IsMonsterLevelUpField(fieldName);
            bool other = IsMonsterOtherField(fieldName);

            switch (tab)
            {
                case EquipmentValuesTab.MonsterMastery:
                    return mastery;
                case EquipmentValuesTab.MonsterLevelUp:
                    return levelUp;
                case EquipmentValuesTab.MonsterOther:
                    return other;
                case EquipmentValuesTab.Main:
                default:
                    return !mastery && !levelUp && !other;
            }
        }

        public void EnsureTabForField(TabControl tabs, eListCollection listCollection, int listIndex, string fieldName)
        {
            if (tabs == null || !tabs.Visible || !IsMonsterEssenceList(listCollection, listIndex))
            {
                return;
            }

            if (IsMonsterMasteryField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.MonsterMastery);
                return;
            }
            if (IsMonsterLevelUpField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.MonsterLevelUp);
                return;
            }
            if (IsMonsterOtherField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.MonsterOther);
                return;
            }

            SelectTaggedTab(tabs, EquipmentValuesTab.Main);
        }

        private static void SelectTaggedTab(TabControl tabs, EquipmentValuesTab tab)
        {
            foreach (TabPage page in tabs.TabPages)
            {
                if (page.Tag is EquipmentValuesTab && (EquipmentValuesTab)page.Tag == tab)
                {
                    tabs.SelectedTab = page;
                    return;
                }
            }
        }
    }
}

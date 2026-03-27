using System.Windows.Forms;

namespace FWEledit
{
    public sealed class EquipmentTabService
    {
        public EquipmentValuesTab GetSelectedTab(TabControl tabs)
        {
            if (tabs == null || !tabs.Visible)
            {
                return EquipmentValuesTab.All;
            }
            switch (tabs.SelectedIndex)
            {
                case 1:
                    return EquipmentValuesTab.Refine;
                case 2:
                    return EquipmentValuesTab.Models;
                case 3:
                    return EquipmentValuesTab.Other;
                default:
                    return EquipmentValuesTab.Main;
            }
        }

        public bool ShouldIncludeField(EquipmentFieldService equipmentFieldService, eListCollection listCollection, int listIndex, string fieldName, EquipmentValuesTab tab)
        {
            if (equipmentFieldService == null || listCollection == null)
            {
                return true;
            }
            if (!equipmentFieldService.IsEquipmentEssenceList(listCollection, listIndex))
            {
                return true;
            }

            if (tab == EquipmentValuesTab.All)
            {
                return true;
            }

            bool isModels = equipmentFieldService.IsEquipmentModelsField(fieldName);
            bool isRefine = equipmentFieldService.IsEquipmentRefineField(fieldName);
            bool isOther = equipmentFieldService.IsEquipmentOtherField(fieldName);

            switch (tab)
            {
                case EquipmentValuesTab.Models:
                    return isModels;
                case EquipmentValuesTab.Refine:
                    return isRefine;
                case EquipmentValuesTab.Other:
                    return isOther;
                case EquipmentValuesTab.Main:
                default:
                    return !isModels && !isRefine && !isOther;
            }
        }

        public void EnsureTabForField(TabControl tabs, EquipmentFieldService equipmentFieldService, eListCollection listCollection, int listIndex, string fieldName)
        {
            if (tabs == null || !tabs.Visible || equipmentFieldService == null || listCollection == null)
            {
                return;
            }
            if (!equipmentFieldService.IsEquipmentEssenceList(listCollection, listIndex))
            {
                return;
            }

            int targetIndex = 0;
            if (equipmentFieldService.IsEquipmentRefineField(fieldName))
            {
                targetIndex = 1;
            }
            else if (equipmentFieldService.IsEquipmentModelsField(fieldName))
            {
                targetIndex = 2;
            }
            else if (equipmentFieldService.IsEquipmentOtherField(fieldName))
            {
                targetIndex = 3;
            }

            if (tabs.SelectedIndex != targetIndex)
            {
                tabs.SelectedIndex = targetIndex;
            }
        }

        public void UpdateVisibility(TabControl tabs, bool show)
        {
            if (tabs == null)
            {
                return;
            }
            tabs.Visible = show;
            if (show && tabs.SelectedIndex < 0)
            {
                tabs.SelectedIndex = 0;
            }
            if (show)
            {
                tabs.BringToFront();
            }
        }
    }
}

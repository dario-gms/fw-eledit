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

            if (tabs.SelectedTab != null && tabs.SelectedTab.Tag is EquipmentValuesTab)
            {
                return (EquipmentValuesTab)tabs.SelectedTab.Tag;
            }

            return EquipmentValuesTab.Main;
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
            bool isDecompose = equipmentFieldService.IsEquipmentDecomposeField(fieldName);
            bool isOther = equipmentFieldService.IsEquipmentOtherField(fieldName);

            switch (tab)
            {
                case EquipmentValuesTab.Models:
                    return isModels;
                case EquipmentValuesTab.Refine:
                    return isRefine;
                case EquipmentValuesTab.Decompose:
                    return isDecompose;
                case EquipmentValuesTab.Other:
                    return isOther;
                case EquipmentValuesTab.Main:
                default:
                    return !isModels && !isRefine && !isDecompose;
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

            if (equipmentFieldService.IsEquipmentModelsField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.Models);
                return;
            }
            if (equipmentFieldService.IsEquipmentRefineField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.Refine);
                return;
            }
            if (equipmentFieldService.IsEquipmentDecomposeField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.Decompose);
                return;
            }

            SelectTaggedTab(tabs, EquipmentValuesTab.Main);
        }

        public void UpdateVisibility(TabControl tabs, bool show, TabPage modelsTab, TabPage refineTab, TabPage decomposeTab, TabPage descriptionTab)
        {
            if (tabs == null)
            {
                return;
            }

            SetEquipmentPageVisible(tabs, modelsTab, show, descriptionTab);
            SetEquipmentPageVisible(tabs, refineTab, show, descriptionTab);
            SetEquipmentPageVisible(tabs, decomposeTab, show, descriptionTab);

            if (!show && tabs.SelectedTab != null && tabs.SelectedTab.Tag is EquipmentValuesTab)
            {
                EquipmentValuesTab selected = (EquipmentValuesTab)tabs.SelectedTab.Tag;
                if (selected != EquipmentValuesTab.Main)
                {
                    SelectTaggedTab(tabs, EquipmentValuesTab.Main);
                }
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

        private static void SelectTaggedTab(TabControl tabs, EquipmentValuesTab tab)
        {
            if (tabs == null)
            {
                return;
            }

            foreach (TabPage page in tabs.TabPages)
            {
                if (page.Tag is EquipmentValuesTab && (EquipmentValuesTab)page.Tag == tab)
                {
                    tabs.SelectedTab = page;
                    return;
                }
            }
        }

        private static void SetEquipmentPageVisible(TabControl tabs, TabPage page, bool show, TabPage descriptionTab)
        {
            if (tabs == null || page == null)
            {
                return;
            }

            bool isVisible = tabs.TabPages.Contains(page);
            if (show && !isVisible)
            {
                int insertIndex = descriptionTab != null && tabs.TabPages.Contains(descriptionTab)
                    ? tabs.TabPages.IndexOf(descriptionTab)
                    : tabs.TabPages.Count;
                tabs.TabPages.Insert(insertIndex, page);
            }
            else if (!show && isVisible)
            {
                tabs.TabPages.Remove(page);
            }
        }
    }
}

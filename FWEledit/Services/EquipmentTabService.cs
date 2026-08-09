using System.Windows.Forms;

namespace FWEledit
{
    public sealed class EquipmentTabService
    {
        private readonly MonsterFieldService monsterFieldService = new MonsterFieldService();

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
            if (monsterFieldService.IsMonsterEssenceList(listCollection, listIndex))
            {
                return monsterFieldService.ShouldIncludeField(listCollection, listIndex, fieldName, tab);
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
            bool isMainPinned = equipmentFieldService.IsEquipmentMainPinnedField(fieldName);

            switch (tab)
            {
                case EquipmentValuesTab.Models:
                    return isModels;
                case EquipmentValuesTab.Refine:
                    return isRefine;
                case EquipmentValuesTab.Decompose:
                    return isDecompose;
                case EquipmentValuesTab.Other:
                    return !isMainPinned && (isOther || isDecompose);
                case EquipmentValuesTab.Main:
                default:
                    return isMainPinned || (!isModels && !isRefine && !isDecompose && !isOther);
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
                monsterFieldService.EnsureTabForField(tabs, listCollection, listIndex, fieldName);
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
            if (equipmentFieldService.IsEquipmentDecomposeField(fieldName)
                || equipmentFieldService.IsEquipmentOtherField(fieldName))
            {
                SelectTaggedTab(tabs, EquipmentValuesTab.Other);
                return;
            }

            SelectTaggedTab(tabs, EquipmentValuesTab.Main);
        }

        public void UpdateVisibility(
            TabControl tabs,
            bool show,
            TabPage modelsTab,
            TabPage refineTab,
            TabPage decomposeTab,
            TabPage otherTab,
            TabPage descriptionTab,
            bool showMonster,
            TabPage monsterMasteryTab,
            TabPage monsterLevelUpTab,
            TabPage monsterOtherTab)
        {
            if (tabs == null)
            {
                return;
            }

            SetEquipmentPageVisible(tabs, refineTab, show, descriptionTab);
            SetEquipmentPageVisible(tabs, modelsTab, show, descriptionTab);
            SetEquipmentPageVisible(tabs, otherTab, show, descriptionTab);
            SetEquipmentPageVisible(tabs, decomposeTab, false, descriptionTab);
            SetEquipmentPageVisible(tabs, monsterMasteryTab, showMonster, descriptionTab);
            SetEquipmentPageVisible(tabs, monsterLevelUpTab, showMonster, descriptionTab);
            SetEquipmentPageVisible(tabs, monsterOtherTab, showMonster, descriptionTab);

            if (!show && !showMonster && tabs.SelectedTab != null && tabs.SelectedTab.Tag is EquipmentValuesTab)
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
                int insertIndex = GetInsertIndexBeforeTrailingTabs(tabs, descriptionTab);
                tabs.TabPages.Insert(insertIndex, page);
            }
            else if (!show && isVisible)
            {
                tabs.TabPages.Remove(page);
            }
        }

        private static int GetInsertIndexBeforeTrailingTabs(TabControl tabs, TabPage descriptionTab)
        {
            if (descriptionTab != null && tabs.TabPages.Contains(descriptionTab))
            {
                return tabs.TabPages.IndexOf(descriptionTab);
            }

            for (int i = 0; i < tabs.TabPages.Count; i++)
            {
                if (string.Equals(tabs.TabPages[i].Text, "References", System.StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return tabs.TabPages.Count;
        }
    }
}

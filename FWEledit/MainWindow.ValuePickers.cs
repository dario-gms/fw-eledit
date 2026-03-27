using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private bool ShouldIncludeFieldInValuesTab(int listIndex, string fieldName)
        {
            EquipmentValuesTab tab = equipmentTabService.GetSelectedTab(fwEquipmentTabs);
            return equipmentTabService.ShouldIncludeField(equipmentFieldService, sessionService.ListCollection, listIndex, fieldName, tab);
        }

        private void EnsureEquipmentTabForField(int listIndex, string fieldName)
        {
            equipmentTabService.EnsureTabForField(fwEquipmentTabs, equipmentFieldService, sessionService.ListCollection, listIndex, fieldName);
        }

        private void UpdateEquipmentTabsVisibility(int listIndex)
        {
            equipmentTabService.UpdateVisibility(fwEquipmentTabs, equipmentFieldService.IsEquipmentEssenceList(sessionService.ListCollection, listIndex));
        }

        private void OpenModelPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenModelPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                modelPickerService,
                modelPickerCacheService,
                modelPackageNotificationService,
                dialogService,
                pathIdResolutionService,
                this);
        }

        private void OpenAddonTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenAddonTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                dataGridView_elems,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                addonTypeOptionService,
                Application.StartupPath,
                AssetManager.GameRootPath,
                valueRowIndex => valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, valueRowIndex),
                message => MessageBox.Show(message, "Added Attribute", MessageBoxButtons.OK, MessageBoxIcon.Information),
                this);
        }

        private void OpenItemQualityPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenItemQualityPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                ItemQualityOptions,
                this);
        }

        private void OpenIconPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenIconPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                valueRowIndexService,
                iconUsageLookupService,
                this);
        }
        private void OpenModelPreviewForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenModelPreviewForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService.AssetManager,
                sessionService.Database,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                modelPreviewService,
                message => MessageBox.Show(message));
        }

        private void UpdatePickIconButtonState()
        {
            mainWindowValuePickerCoordinatorService.UpdatePickIconButtonState(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                inlineIconButtonService,
                fwInlinePickIconButton,
                dataGridView_item,
                fwRightTabs,
                fwValuesTab,
                itemFieldClassifierService,
                ref fwInlinePickIconRowIndex,
                viewModel.SuppressValuesUiRefresh);
        }

        private void click_pick_icon(object sender, EventArgs e)
        {
            mainWindowValuePickerCoordinatorService.HandleInlinePickIconClick(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                fwInlinePickIconRowIndex,
                OpenIconPickerForValueRow);
        }

        private void dataGridView_item_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            mainWindowValuePickerCoordinatorService.HandleCellDoubleClick(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                e,
                itemFieldClassifierService,
                OpenIconPickerForValueRow,
                OpenAddonTypePickerForValueRow,
                OpenItemQualityPickerForValueRow,
                OpenModelPickerForValueRow,
                UpdatePickIconButtonState,
                message => MessageBox.Show(message));
        }

    }
}



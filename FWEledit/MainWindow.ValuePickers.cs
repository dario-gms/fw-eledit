using System;
using System.Drawing;
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
                sessionService.ListCollection,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                pathIdResolutionService,
                modelPickerService,
                modelPreviewService,
                this,
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

        private void dataGridView_item_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e == null || e.Button != MouseButtons.Right || e.RowIndex < 0 || dataGridView_item == null)
            {
                return;
            }
            if (e.RowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            if (e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView_item.Columns.Count)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
            else if (dataGridView_item.Columns.Count > 2)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[e.RowIndex].Cells[2];
            }

            ShowValueRowContextMenu(e.RowIndex, dataGridView_item.PointToScreen(new Point(e.X, e.Y)));
        }

        private void ShowValueRowContextMenu(int rowIndex, Point screenLocation)
        {
            if (dataGridView_item == null || rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            bool isModelField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelFieldName(fieldName);
            bool isIconField = itemFieldClassifierService != null && itemFieldClassifierService.IsIconFieldName(fieldName);
            bool isAddonTypeField = itemFieldClassifierService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemFieldClassifierService.IsAddonTypeField(sessionService.ListCollection, comboBox_lists.SelectedIndex, fieldName);
            bool isItemQualityField = itemFieldClassifierService != null && itemFieldClassifierService.IsItemQualityFieldName(fieldName);

            ContextMenuStrip menu = new ContextMenuStrip();

            if (isModelField)
            {
                menu.Items.Add("Choose Model...", null, (menuSender, args) => OpenModelPickerForValueRow(rowIndex));
                menu.Items.Add("Preview 3D Model", null, (menuSender, args) => OpenModelPreviewForValueRow(rowIndex));
            }

            if (isIconField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Icon...", null, (menuSender, args) => OpenIconPickerForValueRow(rowIndex));
            }

            if (isAddonTypeField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Added Attribute Type...", null, (menuSender, args) => OpenAddonTypePickerForValueRow(rowIndex));
            }

            if (isItemQualityField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Item Quality...", null, (menuSender, args) => OpenItemQualityPickerForValueRow(rowIndex));
            }

            if (menu.Items.Count == 0)
            {
                menu.Dispose();
                return;
            }

            menu.Show(screenLocation);
        }

    }
}



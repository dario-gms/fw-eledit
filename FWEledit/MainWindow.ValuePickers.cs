using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private bool ShouldIncludeFieldInValuesTab(int listIndex, int fieldIndex, string fieldName)
        {
            if (ShouldIncludeNpcSellFieldInValuesTab(listIndex, fieldIndex, fieldName))
            {
                return true;
            }

            if (NpcSellServiceCatalog.IsNpcSellServiceList(sessionService.ListCollection, listIndex))
            {
                return false;
            }

            EquipmentValuesTab tab = equipmentTabService.GetSelectedTab(fwEquipmentTabs);
            return equipmentTabService.ShouldIncludeField(equipmentFieldService, sessionService.ListCollection, listIndex, fieldName, tab);
        }

        private void EnsureEquipmentTabForField(int listIndex, string fieldName)
        {
            equipmentTabService.EnsureTabForField(fwEquipmentTabs, equipmentFieldService, sessionService.ListCollection, listIndex, fieldName);
        }

        private void UpdateEquipmentTabsVisibility(int listIndex)
        {
            equipmentTabService.UpdateVisibility(
                fwEquipmentTabs,
                equipmentFieldService.IsEquipmentEssenceList(sessionService.ListCollection, listIndex),
                fwEquipmentTabModels,
                fwEquipmentTabRefine,
                fwEquipmentTabDecompose,
                fwDescriptionTab);
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
                sessionService.AssetManager,
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

        private void OpenGenderTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenGenderTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenPetFoodTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenPetFoodTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenPetHeroPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenPetHeroPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenImmuneTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenImmuneTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenBindFlagPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenBindFlagPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenNpcSellMoneyTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenNpcSellMoneyTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService.ListCollection,
                comboBox_lists.SelectedIndex,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenReputationPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenReputationPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenSoulToolRewardTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenSoulToolRewardTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService != null ? sessionService.ListCollection : null,
                comboBox_lists.SelectedIndex,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenProcTypePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenProcTypePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenProfessionMaskPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenProfessionMaskPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenRaceMaskPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenRaceMaskPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenModelProfessionPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenModelProfessionPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenModelRacePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenModelRacePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenCombinedServicesPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenCombinedServicesPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemFieldClassifierService,
                this);
        }

        private void OpenSkillPickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenSkillPickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                rowIndex,
                itemFieldClassifierService,
                this,
                message => MessageBox.Show(message, "Skill or buff", MessageBoxButtons.OK, MessageBoxIcon.Information));
        }

        private void OpenItemReferencePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenItemReferencePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                ResolveCurrentElementIndex(),
                rowIndex,
                itemReferenceService,
                iconResolutionService,
                sessionService.AssetManager,
                this,
                message => MessageBox.Show(message, "Item Reference", MessageBoxButtons.OK, MessageBoxIcon.Information));
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

        private sealed class CurrentItemModelPreviewResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public ModelPreviewMeshData MeshData { get; set; }
        }

        private void OpenModelPreviewForCurrentItem()
        {
            OpenModelPreviewForCurrentItem(true, true);
        }

        private async void OpenModelPreviewForCurrentItem(bool showMessages, bool enableLivePreview)
        {
            if (sessionService == null
                || sessionService.AssetManager == null
                || sessionService.Database == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || modelPreviewService == null
                || modelPickerService == null
                || itemFieldClassifierService == null)
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int elementIndex = ResolveCurrentElementIndex();
            int fieldIndex;
            string fieldName;
            int pathId;
            if (!TryResolveFirstModelPreviewFieldForCurrentItem(listIndex, elementIndex, out fieldIndex, out fieldName, out pathId))
            {
                if (showMessages)
                {
                    MessageBox.Show("This item does not have a model preview field.");
                }
                return;
            }

            string listName = sessionService.ListCollection.Lists[listIndex].listName ?? string.Empty;
            bool restoreWaitCursor = UseWaitCursor;
            try
            {
                UseWaitCursor = true;
                CurrentItemModelPreviewResult result = await Task.Run(delegate
                {
                    CurrentItemModelPreviewResult buildResult = new CurrentItemModelPreviewResult();
                    string errorMessage;
                    ModelPreviewMeshData meshData;
                    bool ok = modelPreviewService.TryBuildPreviewMeshData(
                        sessionService.AssetManager,
                        sessionService.Database,
                        pathId,
                        fieldName,
                        listName,
                        modelPickerService,
                        out meshData,
                        out errorMessage);

                    buildResult.Success = ok;
                    buildResult.Error = errorMessage ?? string.Empty;
                    buildResult.MeshData = meshData;
                    return buildResult;
                });

                if (!result.Success)
                {
                    if (showMessages && !string.IsNullOrWhiteSpace(result.Error))
                    {
                        MessageBox.Show(result.Error);
                    }
                    return;
                }

                modelPreviewService.ShowPreviewWindow(result.MeshData);
                if (enableLivePreview && valueRowPickerUiService != null)
                {
                    valueRowPickerUiService.EnableLiveModelPreview(pathId, listIndex, fieldIndex);
                }
            }
            catch (Exception ex)
            {
                if (showMessages)
                {
                    MessageBox.Show("MODEL PREVIEW ERROR!\n" + ex.Message);
                }
            }
            finally
            {
                UseWaitCursor = restoreWaitCursor;
            }
        }

        private bool TryResolveFirstModelPreviewFieldForCurrentItem(
            int listIndex,
            int elementIndex,
            out int fieldIndex,
            out string fieldName,
            out int pathId)
        {
            fieldIndex = -1;
            fieldName = string.Empty;
            pathId = 0;

            if (sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || elementIndex < 0
                || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            string[] fields = sessionService.ListCollection.Lists[listIndex].elementFields;
            if (fields == null)
            {
                return false;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                string candidateFieldName = fields[i] ?? string.Empty;
                if (!itemFieldClassifierService.IsModelUsageFieldName(candidateFieldName))
                {
                    continue;
                }

                string rawValue = sessionService.ListCollection.GetValue(listIndex, elementIndex, i);
                int candidatePathId;
                if ((modelPickerService.TryExtractPathId(rawValue, out candidatePathId) || int.TryParse(rawValue, out candidatePathId))
                    && candidatePathId > 0)
                {
                    fieldIndex = i;
                    fieldName = candidateFieldName;
                    pathId = candidatePathId;
                    return true;
                }
            }

            return false;
        }

        private void dataGridView_item_CurrentCellChanged(object sender, EventArgs e)
        {
            if (viewModel != null && viewModel.SuppressValuesUiRefresh)
            {
                return;
            }

            UpdateRawValueEditorFromCurrentCell();
            RefreshLiveModelPreviewFromCurrentRow(false);
        }

        private void dataGridView_item_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e == null || e.RowIndex < 0 || e.ColumnIndex != 2 || dataGridView_item == null)
            {
                return;
            }
            if (sessionService == null || sessionService.ListCollection == null || itemReferenceService == null)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, e.RowIndex);
            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, e.RowIndex);
            int elementIndex = ResolveCurrentElementIndex();
            if (listIndex < 0 || fieldIndex < 0 || elementIndex < 0)
            {
                return;
            }

            object cachedTag = dataGridView_item.Rows[e.RowIndex].Cells[2].Tag;
            ValueCellState cachedState = cachedTag as ValueCellState;
            string rawValue = cachedState != null
                ? (cachedState.RawValue ?? string.Empty)
                : sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex);
            if (creaturePortraitIconService.IsCreaturePortraitField(sessionService.ListCollection, listIndex, fieldName))
            {
                PaintPortraitValueCell(e, creaturePortraitIconService, rawValue);
                return;
            }

            ItemReferenceOption option = cachedState != null ? cachedState.ReferenceOption : null;
            if (option == null
                && !itemReferenceService.TryResolveReferenceOption(
                    sessionService.ListCollection,
                    listIndex,
                    elementIndex,
                    fieldName,
                    rawValue,
                    sessionService.Database,
                    iconResolutionService,
                    out option))
            {
                return;
            }

            PaintReferenceValueCell(e, option);
        }

        private void PaintReferenceValueCell(DataGridViewCellPaintingEventArgs e, ItemReferenceOption option)
        {
            bool selected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            Color backColor = selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
            Color textColor = ResolveReferenceValueTextColor(option, selected, e.CellStyle.ForeColor);

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.CellBounds);
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border | DataGridViewPaintParts.Focus);

            Rectangle iconBounds = new Rectangle(e.CellBounds.Left + 6, e.CellBounds.Top + 2, 20, Math.Max(16, e.CellBounds.Height - 4));
            iconBounds.Width = Math.Min(20, iconBounds.Height);
            DrawReferenceValueIcon(e.Graphics, option, iconBounds);

            Rectangle textBounds = new Rectangle(
                iconBounds.Right + 7,
                e.CellBounds.Top,
                Math.Max(4, e.CellBounds.Width - iconBounds.Width - 18),
                e.CellBounds.Height);

            string text = string.IsNullOrWhiteSpace(option.Name) ? option.Id.ToString() : option.Name;
            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.CellStyle.Font,
                textBounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.Handled = true;
        }

        private void PaintPortraitValueCell(DataGridViewCellPaintingEventArgs e, CreaturePortraitIconService portraitIconService, string rawValue)
        {
            bool selected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            Color backColor = selected ? e.CellStyle.SelectionBackColor : e.CellStyle.BackColor;
            Color textColor = selected ? Color.White : e.CellStyle.ForeColor;

            using (SolidBrush brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.CellBounds);
            }

            e.Paint(e.CellBounds, DataGridViewPaintParts.Border | DataGridViewPaintParts.Focus);

            int pathId;
            string mappedPath;
            Bitmap icon = Properties.Resources.NoIcon;
            string text = rawValue ?? string.Empty;
            if (portraitIconService.TryResolvePortraitPath(sessionService.Database, rawValue, out pathId, out mappedPath))
            {
                Bitmap loaded = portraitIconService.TryLoadPortraitThumbnail(mappedPath, 32);
                if (loaded != null)
                {
                    icon = loaded;
                }

                string name = System.IO.Path.GetFileName(mappedPath);
                text = string.IsNullOrWhiteSpace(name) ? mappedPath : name + " | " + mappedPath;
            }

            Rectangle iconBounds = new Rectangle(e.CellBounds.Left + 6, e.CellBounds.Top + 2, 20, Math.Max(16, e.CellBounds.Height - 4));
            iconBounds.Width = Math.Min(20, iconBounds.Height);
            e.Graphics.DrawImage(icon, iconBounds);

            Rectangle textBounds = new Rectangle(
                iconBounds.Right + 7,
                e.CellBounds.Top,
                Math.Max(4, e.CellBounds.Width - iconBounds.Width - 18),
                e.CellBounds.Height);

            TextRenderer.DrawText(
                e.Graphics,
                text,
                e.CellStyle.Font,
                textBounds,
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            e.Handled = true;
        }

        private void DrawReferenceValueIcon(Graphics graphics, ItemReferenceOption option, Rectangle bounds)
        {
            Bitmap icon = Properties.Resources.NoIcon;
            if (sessionService != null
                && sessionService.Database != null
                && option != null
                && !string.IsNullOrWhiteSpace(option.IconKey))
            {
                if (sessionService.Database.sourceBitmap != null
                    && sessionService.Database.ContainsKey(option.IconKey))
                {
                    icon = sessionService.Database.images(option.IconKey);
                }
                else
                {
                    Bitmap portrait = creaturePortraitIconService.TryLoadPortraitThumbnail(option.IconKey, 20);
                    if (portrait != null)
                    {
                        icon = portrait;
                    }
                }
            }

            graphics.DrawImage(icon, bounds);
        }

        private Color ResolveReferenceValueTextColor(ItemReferenceOption option, bool selected, Color fallback)
        {
            Color accentColor;
            if (TryParseReferenceAccentColor(option != null ? option.AccentHex : string.Empty, out accentColor))
            {
                return accentColor;
            }

            int quality = option != null ? option.Quality : -1;
            Color color;
            if (ItemQualityCatalog.TryGetColor(quality, out color))
            {
                if (quality == 1 && !selected)
                {
                    return fwDarkMode ? Color.FromArgb(235, 238, 244) : Color.FromArgb(29, 36, 45);
                }
                return color;
            }
            return selected ? Color.White : fallback;
        }

        private static bool TryParseReferenceAccentColor(string accentHex, out Color color)
        {
            color = Color.Empty;
            if (string.IsNullOrWhiteSpace(accentHex))
            {
                return false;
            }

            string normalized = accentHex.Trim().TrimStart('#');
            if (normalized.Length != 6)
            {
                return false;
            }

            int rgb;
            if (!int.TryParse(
                normalized,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out rgb))
            {
                return false;
            }

            color = Color.FromArgb((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
            return true;
        }

        private void UpdateRawValueEditorFromCurrentCell()
        {
            if (textBox_SetValue == null)
            {
                return;
            }

            string rawValue;
            bool enabled = TryGetRawValueForCurrentCell(out rawValue);

            textBox_SetValue.Enabled = enabled;
            textBox_SetValue.Text = rawValue ?? string.Empty;
            if (fwRawValueUpButton != null)
            {
                fwRawValueUpButton.Enabled = enabled && IsIntegerText(rawValue);
            }
            if (fwRawValueDownButton != null)
            {
                fwRawValueDownButton.Enabled = enabled && IsIntegerText(rawValue);
            }
            if (button_SetValue != null)
            {
                button_SetValue.Enabled = enabled;
            }

            UpdateCurrentIdUsageIndicator();
        }

        private bool TryGetRawValueForCurrentCell(out string rawValue)
        {
            rawValue = string.Empty;
            if (sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_item == null
                || dataGridView_item.CurrentCell == null)
            {
                return false;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int rowIndex = dataGridView_item.CurrentCell.RowIndex;
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, rowIndex);
            int elementIndex = ResolveCurrentElementIndex();
            if (listIndex < 0 || fieldIndex < 0 || elementIndex < 0)
            {
                return false;
            }
            if (listIndex >= sessionService.ListCollection.Lists.Length
                || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length
                || fieldIndex >= sessionService.ListCollection.Lists[listIndex].elementFields.Length)
            {
                return false;
            }

            rawValue = sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex);
            return true;
        }

        private int ResolveCurrentElementIndex()
        {
            if (sessionService == null || sessionService.ListCollection == null || comboBox_lists == null)
            {
                return -1;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int gridRowIndex = gridActiveRowService.GetActiveRowIndex(dataGridView_elems, gridSelectionService);
            return elementIndexResolverService.ResolveElementIndexFromGridRow(
                sessionService.ListCollection,
                listIndex,
                gridRowIndex,
                dataGridView_elems);
        }

        private void ApplyRawValueEditorToCurrentCell()
        {
            if (dataGridView_item == null || dataGridView_item.CurrentCell == null || textBox_SetValue == null)
            {
                return;
            }

            int rowIndex = dataGridView_item.CurrentCell.RowIndex;
            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string rawValue = textBox_SetValue.Text ?? string.Empty;

            object current = dataGridView_item.Rows[rowIndex].Cells[2].Tag ?? dataGridView_item.Rows[rowIndex].Cells[2].Value;
            ValueCellState currentState = current as ValueCellState;
            string currentRawValue = currentState != null ? (currentState.RawValue ?? string.Empty) : Convert.ToString(current);
            if (string.Equals(currentRawValue, rawValue, StringComparison.Ordinal))
            {
                return;
            }

            dataGridView_item.Rows[rowIndex].Cells[2].Tag = rawValue;
            dataGridView_item.Rows[rowIndex].Cells[2].Value = rawValue;
            if (fwRawValueUpButton != null)
            {
                fwRawValueUpButton.Enabled = IsIntegerText(rawValue);
            }
            if (fwRawValueDownButton != null)
            {
                fwRawValueDownButton.Enabled = IsIntegerText(rawValue);
            }
            if (button_SetValue != null)
            {
                button_SetValue.Enabled = true;
            }

            UpdateCurrentIdUsageIndicator();
        }

        private void UpdateCurrentIdUsageIndicator()
        {
            RestoreIdValueEditorStyle();

            if (textBox_SetValue == null
                || dataGridView_item == null
                || dataGridView_item.CurrentCell == null
                || comboBox_lists == null
                || sessionService == null
                || sessionService.ListCollection == null)
            {
                return;
            }

            int valueRowIndex = dataGridView_item.CurrentCell.RowIndex;
            if (valueRowIndex < 0 || valueRowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, valueRowIndex);
            if (!string.Equals(fieldName, "id", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int elementIndex = ResolveCurrentElementIndex();
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, valueRowIndex);
            if (listIndex < 0 || elementIndex < 0 || fieldIndex < 0)
            {
                return;
            }

            string candidateRawValue = textBox_SetValue.Text ?? string.Empty;
            int candidateId;
            if (!int.TryParse(candidateRawValue, out candidateId) || candidateId <= 0)
            {
                return;
            }

            int currentStoredId = 0;
            int.TryParse(sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex), out currentStoredId);

            int duplicateCount = CountIdOccurrencesInList(sessionService.ListCollection, listIndex, candidateId);
            bool hasDuplicateId = candidateId == currentStoredId
                ? duplicateCount > 1
                : duplicateCount > 0;

            if (!hasDuplicateId)
            {
                return;
            }

            DataGridViewCell valueCell = dataGridView_item.Rows[valueRowIndex].Cells[2];
            Font baseFont = dataGridView_item.DefaultCellStyle.Font ?? dataGridView_item.Font ?? textBox_SetValue.Font;
            if (baseFont == null)
            {
                baseFont = valueCell.InheritedStyle.Font;
            }

            valueCell.Style.ForeColor = Color.Red;
            valueCell.Style.SelectionForeColor = Color.Red;
            if (baseFont != null)
            {
                valueCell.Style.Font = new Font(baseFont, FontStyle.Bold);
                textBox_SetValue.Font = new Font(baseFont, FontStyle.Bold);
            }

            textBox_SetValue.ForeColor = Color.Red;
        }

        private void RestoreIdValueEditorStyle()
        {
            if (textBox_SetValue == null)
            {
                return;
            }

            Font baseFont = null;
            if (dataGridView_item != null)
            {
                baseFont = dataGridView_item.DefaultCellStyle.Font ?? dataGridView_item.Font;
            }

            textBox_SetValue.ForeColor = dataGridView_item != null
                ? dataGridView_item.DefaultCellStyle.ForeColor
                : SystemColors.WindowText;
            if (baseFont != null)
            {
                textBox_SetValue.Font = baseFont;
            }

            if (dataGridView_item == null
                || comboBox_lists == null
                || sessionService == null
                || sessionService.ListCollection == null)
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int elementIndex = ResolveCurrentElementIndex();
            if (listIndex < 0 || elementIndex < 0)
            {
                return;
            }

            for (int rowIndex = 0; rowIndex < dataGridView_item.Rows.Count; rowIndex++)
            {
                if (!string.Equals(ValueGridFieldNameService.GetFieldName(dataGridView_item, rowIndex), "id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, rowIndex);
                if (fieldIndex < 0)
                {
                    continue;
                }

                DataGridViewCell valueCell = dataGridView_item.Rows[rowIndex].Cells[2];
                Font cellBaseFont = dataGridView_item.DefaultCellStyle.Font ?? dataGridView_item.Font ?? valueCell.InheritedStyle.Font;
                valueCell.Style.Font = cellBaseFont;

                if (mainWindowDirtyTrackingService.IsFieldInvalid(dirtyStateTracker, listIndex, elementIndex, fieldIndex))
                {
                    valueCell.Style.ForeColor = Color.Red;
                    valueCell.Style.SelectionForeColor = Color.Red;
                }
                else if (mainWindowDirtyTrackingService.IsFieldDirty(dirtyStateTracker, listIndex, elementIndex, fieldIndex))
                {
                    valueCell.Style.ForeColor = Color.DeepSkyBlue;
                    valueCell.Style.SelectionForeColor = Color.DeepSkyBlue;
                }
                else
                {
                    valueCell.Style.ForeColor = dataGridView_item.DefaultCellStyle.ForeColor;
                    valueCell.Style.SelectionForeColor = dataGridView_item.DefaultCellStyle.SelectionForeColor;
                }
            }
        }

        private void AdjustRawValueEditor(int delta)
        {
            if (textBox_SetValue == null || !textBox_SetValue.Enabled)
            {
                return;
            }

            int value;
            if (!int.TryParse(textBox_SetValue.Text, out value))
            {
                value = 0;
            }
            value += delta;
            if (value < 0)
            {
                value = 0;
            }
            textBox_SetValue.Text = value.ToString();
            textBox_SetValue.SelectAll();
        }

        private static bool IsIntegerText(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed);
        }

        private void RefreshLiveModelPreviewFromCurrentRow(bool preferFirstModelField)
        {
            if (liveModelPreviewRefreshInProgress
                || valueRowPickerUiService == null
                || !valueRowPickerUiService.IsLiveModelPreviewEnabled
                || modelPreviewService == null
                || itemFieldClassifierService == null
                || dataGridView_item == null
                || comboBox_lists == null
                || sessionService == null
                || sessionService.AssetManager == null
                || sessionService.Database == null
                || sessionService.ListCollection == null)
            {
                return;
            }

            if (!modelPreviewService.IsPreviewWindowOpen())
            {
                valueRowPickerUiService.DisableLiveModelPreview();
                return;
            }

            int rowIndex = -1;
            if (!preferFirstModelField && dataGridView_item.CurrentCell != null)
            {
                rowIndex = dataGridView_item.CurrentCell.RowIndex;
            }

            if (!preferFirstModelField)
            {
                if (!IsModelFieldRow(rowIndex))
                {
                    return;
                }
            }
            else if (!IsModelFieldRow(rowIndex))
            {
                rowIndex = FindFirstModelFieldRow();
            }

            if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                if (preferFirstModelField)
                {
                    liveModelPreviewRefreshInProgress = true;
                    try
                    {
                        OpenModelPreviewForCurrentItem(false, true);
                    }
                    finally
                    {
                        liveModelPreviewRefreshInProgress = false;
                    }
                }
                return;
            }

            liveModelPreviewRefreshInProgress = true;
            try
            {
                valueRowPickerUiService.OpenModelPreviewForValueRow(
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
                    message => MessageBox.Show(message),
                    true,
                    true);
            }
            finally
            {
                liveModelPreviewRefreshInProgress = false;
            }
        }

        private bool IsModelFieldRow(int rowIndex)
        {
            if (rowIndex < 0 || dataGridView_item == null || rowIndex >= dataGridView_item.Rows.Count || itemFieldClassifierService == null)
            {
                return false;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, rowIndex);
            return itemFieldClassifierService.IsModelUsageFieldName(fieldName);
        }

        private int FindFirstModelFieldRow()
        {
            if (dataGridView_item == null || itemFieldClassifierService == null)
            {
                return -1;
            }

            for (int i = 0; i < dataGridView_item.Rows.Count; i++)
            {
                string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, i);
                if (itemFieldClassifierService.IsModelUsageFieldName(fieldName))
                {
                    return i;
                }
            }

            return -1;
        }

        private void UpdatePickIconButtonState()
        {
            mainWindowValuePickerCoordinatorService.UpdatePickIconButtonState(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                inlineIconButtonService,
                fwInlinePickIconButton,
                dataGridView_item,
                fwRightTabs,
                fwValuesTab,
                comboBox_lists.SelectedIndex,
                itemFieldClassifierService,
                itemReferenceService,
                ref fwInlinePickIconRowIndex,
                viewModel.SuppressValuesUiRefresh);
        }

        private void click_pick_icon(object sender, EventArgs e)
        {
            int targetRow = fwInlinePickIconRowIndex;
            if (targetRow < 0 && dataGridView_item != null && dataGridView_item.CurrentCell != null)
            {
                targetRow = dataGridView_item.CurrentCell.RowIndex;
            }
            if (targetRow < 0 || dataGridView_item == null || targetRow >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, targetRow);
            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;

            if (itemFieldClassifierService.IsIconFieldName(fieldName))
            {
                OpenIconPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsAddonTypeField(sessionService != null ? sessionService.ListCollection : null, listIndex, fieldName))
            {
                OpenAddonTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsItemQualityFieldName(fieldName))
            {
                OpenItemQualityPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsGenderTypeFieldName(fieldName))
            {
                OpenGenderTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsPetFoodTypeFieldName(fieldName))
            {
                OpenPetFoodTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsPetHeroFieldName(fieldName))
            {
                OpenPetHeroPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsImmuneTypeFieldName(fieldName))
            {
                OpenImmuneTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsBindFlagFieldName(fieldName))
            {
                OpenBindFlagPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsNpcSellMoneyTypeFieldName(
                sessionService != null ? sessionService.ListCollection : null,
                listIndex,
                valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, targetRow),
                fieldName))
            {
                OpenNpcSellMoneyTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsReputationFieldName(fieldName))
            {
                OpenReputationPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsRewardTypeFieldName(
                sessionService != null ? sessionService.ListCollection : null,
                listIndex,
                fieldName))
            {
                OpenSoulToolRewardTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsProcTypeFieldName(fieldName))
            {
                OpenProcTypePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsProfessionMaskFieldName(fieldName))
            {
                OpenProfessionMaskPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsRaceMaskFieldName(fieldName))
            {
                OpenRaceMaskPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsModelProfessionFieldName(fieldName))
            {
                OpenModelProfessionPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsModelRaceFieldName(fieldName))
            {
                OpenModelRacePickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsCombinedServicesFieldName(fieldName))
            {
                OpenCombinedServicesPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsSkillFieldName(fieldName))
            {
                OpenSkillPickerForValueRow(targetRow);
            }
            else if (itemFieldClassifierService.IsModelFieldName(fieldName))
            {
                OpenModelPickerForValueRow(targetRow);
            }
            else if (sessionService != null
                && itemReferenceService != null
                && itemReferenceService.IsReferenceField(sessionService.ListCollection, listIndex, fieldName))
            {
                OpenItemReferencePickerForValueRow(targetRow);
            }
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
                itemReferenceService,
                OpenIconPickerForValueRow,
                OpenAddonTypePickerForValueRow,
                OpenItemQualityPickerForValueRow,
                OpenGenderTypePickerForValueRow,
                OpenPetFoodTypePickerForValueRow,
                OpenPetHeroPickerForValueRow,
                OpenImmuneTypePickerForValueRow,
                OpenBindFlagPickerForValueRow,
                OpenNpcSellMoneyTypePickerForValueRow,
                OpenReputationPickerForValueRow,
                OpenSoulToolRewardTypePickerForValueRow,
                OpenProcTypePickerForValueRow,
                OpenProfessionMaskPickerForValueRow,
                OpenRaceMaskPickerForValueRow,
                OpenModelProfessionPickerForValueRow,
                OpenModelRacePickerForValueRow,
                OpenCombinedServicesPickerForValueRow,
                OpenSkillPickerForValueRow,
                OpenModelPickerForValueRow,
                OpenItemReferencePickerForValueRow,
                UpdatePickIconButtonState,
                message => MessageBox.Show(
                    message,
                    "Field action",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.G))
            {
                GoToReferencedItemFromCurrentValueRow(true);
                return true;
            }

            if (keyData == (Keys.Control | Keys.C) && IsValueGridShortcutTarget())
            {
                return TryCopyCurrentValueRowToClipboard();
            }

            if (keyData == (Keys.Control | Keys.V) && IsValueGridShortcutTarget())
            {
                return TryPasteClipboardIntoCurrentValueRow();
            }

            if (keyData == (Keys.Control | Keys.Z) && IsValueGridShortcutTarget())
            {
                return UndoLastValueEdit();
            }

            if (keyData == Keys.Delete && IsValueGridShortcutTarget())
            {
                return TryClearCurrentValueRows();
            }

            return base.ProcessCmdKey(ref msg, keyData);
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

            bool preserveSelection = e.ColumnIndex >= 0
                && e.ColumnIndex < dataGridView_item.Columns.Count
                && dataGridView_item.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected;
            List<Point> selectedCells = preserveSelection ? CaptureSelectedValueCells() : null;

            if (e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView_item.Columns.Count)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
            else if (dataGridView_item.Columns.Count > 2)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[e.RowIndex].Cells[2];
            }

            if (preserveSelection)
            {
                RestoreSelectedValueCells(selectedCells, e.RowIndex, e.ColumnIndex);
            }
            else if (e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView_item.Columns.Count)
            {
                dataGridView_item.ClearSelection();
                dataGridView_item.Rows[e.RowIndex].Cells[e.ColumnIndex].Selected = true;
            }

            Point screenLocation;
            if (e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView_item.Columns.Count)
            {
                Rectangle cellRect = dataGridView_item.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                Point clientPoint = new Point(cellRect.Left + e.X, cellRect.Top + e.Y);
                screenLocation = dataGridView_item.PointToScreen(clientPoint);
            }
            else
            {
                screenLocation = Cursor.Position;
            }

            ShowValueRowContextMenu(e.RowIndex, screenLocation);
        }

        private void ShowValueRowContextMenu(int rowIndex, Point screenLocation)
        {
            if (dataGridView_item == null || rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, rowIndex);
            bool isModelField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelFieldName(fieldName);
            bool isModelPreviewField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelUsageFieldName(fieldName);
            bool isIconField = itemFieldClassifierService != null && itemFieldClassifierService.IsIconFieldName(fieldName);
            bool isPortraitIconField = creaturePortraitIconService.IsCreaturePortraitField(sessionService != null ? sessionService.ListCollection : null, comboBox_lists.SelectedIndex, fieldName);
            bool isAddonTypeField = itemFieldClassifierService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemFieldClassifierService.IsAddonTypeField(sessionService.ListCollection, comboBox_lists.SelectedIndex, fieldName);
            bool isItemQualityField = itemFieldClassifierService != null && itemFieldClassifierService.IsItemQualityFieldName(fieldName);
            bool isGenderTypeField = itemFieldClassifierService != null && itemFieldClassifierService.IsGenderTypeFieldName(fieldName);
            bool isPetFoodTypeField = itemFieldClassifierService != null && itemFieldClassifierService.IsPetFoodTypeFieldName(fieldName);
            bool isPetHeroField = itemFieldClassifierService != null && itemFieldClassifierService.IsPetHeroFieldName(fieldName);
            bool isImmuneTypeField = itemFieldClassifierService != null && itemFieldClassifierService.IsImmuneTypeFieldName(fieldName);
            bool isBindFlagField = itemFieldClassifierService != null && itemFieldClassifierService.IsBindFlagFieldName(fieldName);
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, rowIndex);
            bool isNpcSellMoneyTypeField = itemFieldClassifierService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemFieldClassifierService.IsNpcSellMoneyTypeFieldName(sessionService.ListCollection, comboBox_lists.SelectedIndex, fieldIndex, fieldName);
            bool isReputationField = itemFieldClassifierService != null && itemFieldClassifierService.IsReputationFieldName(fieldName);
            bool isSoulToolRewardTypeField = itemFieldClassifierService != null
                && itemFieldClassifierService.IsRewardTypeFieldName(
                    sessionService != null ? sessionService.ListCollection : null,
                    comboBox_lists.SelectedIndex,
                    fieldName);
            bool isProfessionMaskField = itemFieldClassifierService != null && itemFieldClassifierService.IsProfessionMaskFieldName(fieldName);
            bool isRaceMaskField = itemFieldClassifierService != null && itemFieldClassifierService.IsRaceMaskFieldName(fieldName);
            bool isModelProfessionField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelProfessionFieldName(fieldName);
            bool isModelRaceField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelRaceFieldName(fieldName);
            bool isCombinedServicesField = itemFieldClassifierService != null && itemFieldClassifierService.IsCombinedServicesFieldName(fieldName);
            bool isSkillField = itemFieldClassifierService != null && itemFieldClassifierService.IsSkillFieldName(fieldName);
            bool isReferenceField = itemReferenceService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemReferenceService.IsReferenceField(sessionService.ListCollection, comboBox_lists.SelectedIndex, fieldName);

            ContextMenuStrip menu = new ContextMenuStrip();

            int[] selectedValueRows = GetSelectedValueRowIndices(rowIndex);
            bool hasMultipleValueRows = selectedValueRows.Length > 1;

            ToolStripMenuItem copyFieldItem = new ToolStripMenuItem(hasMultipleValueRows ? "Copy Fields" : "Copy Field");
            copyFieldItem.ShortcutKeyDisplayString = "Ctrl+C";
            copyFieldItem.Click += (menuSender, args) => CopyValueRowsToClipboard(selectedValueRows);
            menu.Items.Add(copyFieldItem);

            ToolStripMenuItem pasteFieldItem = new ToolStripMenuItem(hasMultipleValueRows ? "Paste Fields" : "Paste Field");
            pasteFieldItem.ShortcutKeyDisplayString = "Ctrl+V";
            pasteFieldItem.Enabled = ClipboardHasText();
            pasteFieldItem.Click += (menuSender, args) => PasteClipboardIntoValueRows(selectedValueRows);
            menu.Items.Add(pasteFieldItem);

            ToolStripMenuItem undoFieldItem = new ToolStripMenuItem("Undo Field Change");
            undoFieldItem.ShortcutKeyDisplayString = "Ctrl+Z";
            undoFieldItem.Enabled = valueEditUndoStack.Count > 0;
            undoFieldItem.Click += (menuSender, args) => UndoLastValueEdit();
            menu.Items.Add(undoFieldItem);

            ToolStripMenuItem clearFieldItem = new ToolStripMenuItem(hasMultipleValueRows ? "Clear Fields" : "Clear Field");
            clearFieldItem.ShortcutKeyDisplayString = "Del";
            clearFieldItem.Click += (menuSender, args) => ClearValueRows(selectedValueRows);
            menu.Items.Add(clearFieldItem);

            if (isReferenceField && CanResolveReferencedItemForValueRow(rowIndex))
            {
                menu.Items.Add(new ToolStripSeparator());
                ToolStripMenuItem goToItem = new ToolStripMenuItem("Go to referenced item");
                goToItem.ShortcutKeyDisplayString = "Ctrl+Shift+G";
                goToItem.Click += (menuSender, args) => GoToReferencedItemFromValueRow(rowIndex, true);
                menu.Items.Add(goToItem);
            }

            if (isModelField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Model...", null, (menuSender, args) => OpenModelPickerForValueRow(rowIndex));
            }

            if (isModelPreviewField)
            {
                menu.Items.Add("Preview 3D Model", null, (menuSender, args) => OpenModelPreviewForValueRow(rowIndex));
            }

            if (isIconField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add(isPortraitIconField ? "Choose TGA Portrait..." : "Choose Icon...", null, (menuSender, args) => OpenIconPickerForValueRow(rowIndex));
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

            if (isGenderTypeField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Gender Type...", null, (menuSender, args) => OpenGenderTypePickerForValueRow(rowIndex));
            }

            if (isPetFoodTypeField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Pet Food Type...", null, (menuSender, args) => OpenPetFoodTypePickerForValueRow(rowIndex));
            }

            if (isPetHeroField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Pet Type...", null, (menuSender, args) => OpenPetHeroPickerForValueRow(rowIndex));
            }

            if (isImmuneTypeField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Immunity Flags...", null, (menuSender, args) => OpenImmuneTypePickerForValueRow(rowIndex));
            }

            if (isBindFlagField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Bind State...", null, (menuSender, args) => OpenBindFlagPickerForValueRow(rowIndex));
            }

            if (isReputationField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Reputation...", null, (menuSender, args) => OpenReputationPickerForValueRow(rowIndex));
            }

            if (isNpcSellMoneyTypeField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Shop Currency...", null, (menuSender, args) => OpenNpcSellMoneyTypePickerForValueRow(rowIndex));
            }

            if (isSoulToolRewardTypeField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Reward Type...", null, (menuSender, args) => OpenSoulToolRewardTypePickerForValueRow(rowIndex));
            }

            if (isProfessionMaskField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Allowed Professions...", null, (menuSender, args) => OpenProfessionMaskPickerForValueRow(rowIndex));
            }

            if (isRaceMaskField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Allowed Races...", null, (menuSender, args) => OpenRaceMaskPickerForValueRow(rowIndex));
            }

            if (isModelProfessionField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Model Profession...", null, (menuSender, args) => OpenModelProfessionPickerForValueRow(rowIndex));
            }

            if (isModelRaceField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Model Race...", null, (menuSender, args) => OpenModelRacePickerForValueRow(rowIndex));
            }

            if (isCombinedServicesField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Portable Services...", null, (menuSender, args) => OpenCombinedServicesPickerForValueRow(rowIndex));
            }

            if (isSkillField)
            {
                if (menu.Items.Count > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());
                }
                menu.Items.Add("Choose Skill / Buff...", null, (menuSender, args) => OpenSkillPickerForValueRow(rowIndex));
            }

            if (menu.Items.Count == 0)
            {
                menu.Dispose();
                return;
            }

            menu.Show(screenLocation);
        }

        private bool IsValueGridShortcutTarget()
        {
            return dataGridView_item != null
                && dataGridView_item.ContainsFocus
                && dataGridView_item.CurrentCell != null
                && dataGridView_item.CurrentCell.RowIndex >= 0;
        }

        private bool TryCopyCurrentValueRowToClipboard()
        {
            if (dataGridView_item == null || dataGridView_item.CurrentCell == null)
            {
                return false;
            }

            return CopyValueRowsToClipboard(GetSelectedValueRowIndices(dataGridView_item.CurrentCell.RowIndex));
        }

        private bool CopyValueRowsToClipboard(int[] rowIndices)
        {
            if (rowIndices == null || rowIndices.Length == 0)
            {
                return false;
            }

            try
            {
                string[] rawValues = new string[rowIndices.Length];
                for (int i = 0; i < rowIndices.Length; i++)
                {
                    rawValues[i] = GetRawValueForValueRow(rowIndices[i]) ?? string.Empty;
                }

                DataObject dataObject = new DataObject();
                dataObject.SetData(ValueRowsClipboardFormat, false, rawValues);
                dataObject.SetText(string.Join(Environment.NewLine, rawValues));
                Clipboard.SetDataObject(dataObject, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryPasteClipboardIntoCurrentValueRow()
        {
            if (dataGridView_item == null || dataGridView_item.CurrentCell == null)
            {
                return false;
            }

            return PasteClipboardIntoValueRows(GetSelectedValueRowIndices(dataGridView_item.CurrentCell.RowIndex));
        }

        private bool TryClearCurrentValueRows()
        {
            if (dataGridView_item == null || dataGridView_item.CurrentCell == null)
            {
                return false;
            }

            return ClearValueRows(GetSelectedValueRowIndices(dataGridView_item.CurrentCell.RowIndex));
        }

        private bool PasteClipboardIntoValueRows(int[] rowIndices)
        {
            if (dataGridView_item == null
                || rowIndices == null
                || rowIndices.Length == 0
                || textBox_SetValue == null)
            {
                return false;
            }

            string[] clipboardValues;
            if (!TryGetClipboardValues(out clipboardValues) || clipboardValues.Length == 0)
            {
                return false;
            }

            string[] valuesToApply;
            if (clipboardValues.Length == 1)
            {
                valuesToApply = new string[rowIndices.Length];
                for (int i = 0; i < rowIndices.Length; i++)
                {
                    valuesToApply[i] = clipboardValues[0];
                }
            }
            else if (clipboardValues.Length == rowIndices.Length)
            {
                valuesToApply = clipboardValues;
            }
            else if (clipboardValues.Length > 1 && rowIndices.Length == 1)
            {
                int startRow = rowIndices[0];
                if (startRow < 0 || startRow + clipboardValues.Length > dataGridView_item.Rows.Count)
                {
                    MessageBox.Show(
                        "There are not enough rows below the selected field to paste the copied sequence.",
                        "Paste fields",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return false;
                }

                rowIndices = BuildSequentialValueRowIndices(startRow, clipboardValues.Length);
                valuesToApply = clipboardValues;
            }
            else
            {
                MessageBox.Show(
                    "Select the same number of fields you copied, copy a single field to repeat it, or select the first destination field to paste the sequence downward.",
                    "Paste fields",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }

            BeginValueEditUndoGroup(rowIndices);
            try
            {
                bool applied = false;
                for (int i = 0; i < rowIndices.Length; i++)
                {
                    applied |= ApplyRawValueToValueRow(rowIndices[i], valuesToApply[i]);
                }

                if (!applied)
                {
                    DiscardPendingValueEditUndoGroup();
                }
            }
            finally
            {
                EndValueEditUndoGroup();
            }

            return true;
        }

        private bool ClearValueRows(int[] rowIndices)
        {
            if (dataGridView_item == null
                || rowIndices == null
                || rowIndices.Length == 0
                || textBox_SetValue == null)
            {
                return false;
            }

            BeginValueEditUndoGroup(rowIndices);
            try
            {
                bool applied = false;
                for (int i = 0; i < rowIndices.Length; i++)
                {
                    applied |= ApplyRawValueToValueRow(rowIndices[i], string.Empty);
                }

                if (!applied)
                {
                    DiscardPendingValueEditUndoGroup();
                }

                return applied;
            }
            finally
            {
                EndValueEditUndoGroup();
            }
        }

        private bool ApplyRawValueToValueRow(int rowIndex, string rawValue)
        {
            if (dataGridView_item == null
                || textBox_SetValue == null
                || rowIndex < 0
                || rowIndex >= dataGridView_item.Rows.Count)
            {
                return false;
            }

            string previousRawValue = GetRawValueForValueRow(rowIndex);
            if (dataGridView_item.CurrentCell == null || dataGridView_item.CurrentCell.RowIndex != rowIndex)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[rowIndex].Cells[2];
            }

            textBox_SetValue.Text = rawValue ?? string.Empty;
            ApplyRawValueEditorToCurrentCell();
            return !string.Equals(previousRawValue, GetRawValueForValueRow(rowIndex), StringComparison.Ordinal);
        }

        private int[] GetSelectedValueRowIndices(int fallbackRowIndex)
        {
            int[] selectedRows = gridCellSelectionService != null
                ? gridCellSelectionService.GetSelectedRowIndices(dataGridView_item)
                : new int[0];

            if (selectedRows.Length == 0 && fallbackRowIndex >= 0)
            {
                return new int[] { fallbackRowIndex };
            }

            if (fallbackRowIndex >= 0)
            {
                bool containsFallback = false;
                for (int i = 0; i < selectedRows.Length; i++)
                {
                    if (selectedRows[i] == fallbackRowIndex)
                    {
                        containsFallback = true;
                        break;
                    }
                }

                if (!containsFallback)
                {
                    int[] merged = new int[selectedRows.Length + 1];
                    Array.Copy(selectedRows, merged, selectedRows.Length);
                    merged[merged.Length - 1] = fallbackRowIndex;
                    Array.Sort(merged);
                    return merged;
                }
            }

            return selectedRows;
        }

        private static int[] BuildSequentialValueRowIndices(int startRow, int count)
        {
            int[] rows = new int[count];
            for (int i = 0; i < count; i++)
            {
                rows[i] = startRow + i;
            }

            return rows;
        }

        private List<Point> CaptureSelectedValueCells()
        {
            List<Point> selectedCells = new List<Point>();
            if (dataGridView_item == null || dataGridView_item.SelectedCells == null)
            {
                return selectedCells;
            }

            for (int i = 0; i < dataGridView_item.SelectedCells.Count; i++)
            {
                DataGridViewCell cell = dataGridView_item.SelectedCells[i];
                if (cell != null && cell.RowIndex >= 0 && cell.ColumnIndex >= 0)
                {
                    selectedCells.Add(new Point(cell.ColumnIndex, cell.RowIndex));
                }
            }

            return selectedCells;
        }

        private void RestoreSelectedValueCells(List<Point> selectedCells, int clickedRowIndex, int clickedColumnIndex)
        {
            if (dataGridView_item == null || selectedCells == null || selectedCells.Count == 0)
            {
                return;
            }

            dataGridView_item.ClearSelection();
            for (int i = 0; i < selectedCells.Count; i++)
            {
                Point cellPoint = selectedCells[i];
                if (cellPoint.Y >= 0
                    && cellPoint.Y < dataGridView_item.Rows.Count
                    && cellPoint.X >= 0
                    && cellPoint.X < dataGridView_item.Columns.Count)
                {
                    dataGridView_item.Rows[cellPoint.Y].Cells[cellPoint.X].Selected = true;
                }
            }

            if (clickedRowIndex >= 0
                && clickedRowIndex < dataGridView_item.Rows.Count
                && clickedColumnIndex >= 0
                && clickedColumnIndex < dataGridView_item.Columns.Count)
            {
                dataGridView_item.Rows[clickedRowIndex].Cells[clickedColumnIndex].Selected = true;
            }
        }

        private static bool ClipboardHasText()
        {
            try
            {
                return Clipboard.ContainsText();
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetClipboardValues(out string[] clipboardValues)
        {
            clipboardValues = null;
            try
            {
                IDataObject dataObject = Clipboard.GetDataObject();
                if (dataObject != null && dataObject.GetDataPresent(ValueRowsClipboardFormat))
                {
                    string[] storedValues = dataObject.GetData(ValueRowsClipboardFormat) as string[];
                    if (storedValues != null && storedValues.Length > 0)
                    {
                        clipboardValues = storedValues;
                        return true;
                    }
                }

                if (!Clipboard.ContainsText())
                {
                    return false;
                }

                string clipboardText = Clipboard.GetText();
                clipboardValues = NormalizeClipboardTextToValues(clipboardText);
                return true;
            }
            catch
            {
                clipboardValues = null;
                return false;
            }
        }

        private static string[] NormalizeClipboardTextToValues(string clipboardText)
        {
            if (clipboardText == null)
            {
                return new string[] { string.Empty };
            }

            string normalized = clipboardText.Replace("\r\n", "\n");
            if (normalized.IndexOf('\n') < 0)
            {
                return new string[] { clipboardText };
            }

            return normalized.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        private ValueEditUndoCandidate CaptureValueEditUndoCandidate(DataGridViewCellEventArgs ea)
        {
            if (suppressValueEditUndoCapture
                || ea == null
                || sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_item == null
                || dataGridView_elems == null)
            {
                return null;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, ea.RowIndex);
            if (listIndex < 0 || fieldIndex < 0)
            {
                return null;
            }

            int[] selectedGridRows = gridSelectionService.GetSelectedIndices(dataGridView_elems);
            int activeGridRow = gridActiveRowService.GetActiveRowIndex(dataGridView_elems, gridSelectionService);
            if (selectedGridRows == null || selectedGridRows.Length == 0)
            {
                if (activeGridRow < 0)
                {
                    return null;
                }

                selectedGridRows = new int[] { activeGridRow };
            }

            int[] selectedElementIndices = new int[selectedGridRows.Length];
            string[] oldValues = new string[selectedGridRows.Length];
            bool[] rowWasDirty = new bool[selectedGridRows.Length];
            bool[] fieldWasDirty = new bool[selectedGridRows.Length];
            for (int i = 0; i < selectedGridRows.Length; i++)
            {
                int elementIndex = elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    selectedGridRows[i],
                    dataGridView_elems);
                if (elementIndex < 0)
                {
                    return null;
                }

                selectedElementIndices[i] = elementIndex;
                oldValues[i] = sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex);
                rowWasDirty[i] = mainWindowDirtyTrackingService.IsRowDirty(dirtyStateTracker, listIndex, elementIndex);
                fieldWasDirty[i] = mainWindowDirtyTrackingService.IsFieldDirty(dirtyStateTracker, listIndex, elementIndex, fieldIndex);
            }

            return new ValueEditUndoCandidate
            {
                ListIndex = listIndex,
                FieldIndex = fieldIndex,
                FieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, ea.RowIndex),
                ValueRowIndex = ea.RowIndex,
                ActiveGridRow = activeGridRow,
                SelectedGridRows = selectedGridRows,
                SelectedElementIndices = selectedElementIndices,
                OldValues = oldValues,
                RowWasDirty = rowWasDirty,
                FieldWasDirty = fieldWasDirty,
                PreviousHasUnsavedChanges = viewModel != null && viewModel.HasUnsavedChanges
            };
        }

        private void CommitValueEditUndoCandidate(ValueEditUndoCandidate candidate)
        {
            if (candidate == null
                || suppressValueEditUndoCapture
                || sessionService == null
                || sessionService.ListCollection == null)
            {
                return;
            }

            string[] newValues = new string[candidate.SelectedElementIndices.Length];
            bool changed = false;
            for (int i = 0; i < candidate.SelectedElementIndices.Length; i++)
            {
                int elementIndex = candidate.SelectedElementIndices[i];
                string newValue = sessionService.ListCollection.GetValue(candidate.ListIndex, elementIndex, candidate.FieldIndex);
                newValues[i] = newValue;
                if (!string.Equals(candidate.OldValues[i], newValue, StringComparison.Ordinal))
                {
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            ValueEditUndoChange change = new ValueEditUndoChange
            {
                FieldIndex = candidate.FieldIndex,
                FieldName = candidate.FieldName ?? string.Empty,
                ValueRowIndex = candidate.ValueRowIndex,
                SelectedElementIndices = candidate.SelectedElementIndices,
                OldValues = candidate.OldValues,
                NewValues = newValues,
                RowWasDirty = candidate.RowWasDirty,
                FieldWasDirty = candidate.FieldWasDirty
            };

            if (valueEditUndoGroupDepth > 0)
            {
                if (pendingValueEditUndoBatch == null)
                {
                    pendingValueEditUndoBatch = new ValueEditUndoBatch
                    {
                        ListIndex = candidate.ListIndex,
                        ActiveGridRow = candidate.ActiveGridRow,
                        SelectedGridRows = candidate.SelectedGridRows,
                        PreviousHasUnsavedChanges = candidate.PreviousHasUnsavedChanges
                    };
                }

                pendingValueEditUndoBatch.Changes.Add(change);
                return;
            }

            ValueEditUndoBatch batch = new ValueEditUndoBatch
            {
                ListIndex = candidate.ListIndex,
                ActiveGridRow = candidate.ActiveGridRow,
                SelectedGridRows = candidate.SelectedGridRows,
                PreviousHasUnsavedChanges = candidate.PreviousHasUnsavedChanges
            };
            batch.Changes.Add(change);
            valueEditUndoStack.Push(batch);
        }

        private void BeginValueEditUndoGroup(int[] selectedValueRows)
        {
            valueEditUndoGroupDepth++;
            if (valueEditUndoGroupDepth == 1)
            {
                pendingValueEditUndoBatch = null;
            }
        }

        private void DiscardPendingValueEditUndoGroup()
        {
            pendingValueEditUndoBatch = null;
        }

        private void EndValueEditUndoGroup()
        {
            if (valueEditUndoGroupDepth <= 0)
            {
                return;
            }

            valueEditUndoGroupDepth--;
            if (valueEditUndoGroupDepth == 0)
            {
                if (pendingValueEditUndoBatch != null && pendingValueEditUndoBatch.Changes.Count > 0)
                {
                    valueEditUndoStack.Push(pendingValueEditUndoBatch);
                }

                pendingValueEditUndoBatch = null;
            }
        }

        private bool UndoLastValueEdit()
        {
            if (valueEditUndoStack.Count == 0 || sessionService == null || sessionService.ListCollection == null || comboBox_lists == null)
            {
                return false;
            }

            ValueEditUndoBatch batch = valueEditUndoStack.Pop();
            if (batch == null || batch.Changes.Count == 0)
            {
                return false;
            }

            suppressValueEditUndoCapture = true;
            try
            {
                for (int changeIndex = 0; changeIndex < batch.Changes.Count; changeIndex++)
                {
                    ValueEditUndoChange change = batch.Changes[changeIndex];
                    if (change == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < change.SelectedElementIndices.Length; i++)
                    {
                        int elementIndex = change.SelectedElementIndices[i];
                        string valueToRestore = change.OldValues != null && i < change.OldValues.Length
                            ? change.OldValues[i]
                            : string.Empty;

                        if (string.Equals(change.FieldName, "id", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(change.FieldName, "ID", StringComparison.OrdinalIgnoreCase))
                        {
                            int oldId;
                            int newId;
                            if (change.NewValues != null
                                && i < change.NewValues.Length
                                && int.TryParse(change.NewValues[i], out newId)
                                && int.TryParse(valueToRestore, out oldId)
                                && newId > 0
                                && oldId > 0
                                && newId != oldId)
                            {
                                RemapDescriptionIdIfNeeded(newId, oldId);
                            }
                        }

                        sessionService.ListCollection.SetValue(batch.ListIndex, elementIndex, change.FieldIndex, valueToRestore);
                        bool rowWasDirty = change.RowWasDirty != null
                            && i < change.RowWasDirty.Length
                            && change.RowWasDirty[i];
                        bool fieldWasDirty = change.FieldWasDirty != null
                            && i < change.FieldWasDirty.Length
                            && change.FieldWasDirty[i];

                        if (rowWasDirty)
                        {
                            mainWindowDirtyTrackingService.MarkRowDirty(
                                dirtyStateTracker,
                                listDisplayService,
                                ref viewModel.HasUnsavedChanges,
                                batch.ListIndex,
                                elementIndex);
                        }
                        else
                        {
                            mainWindowDirtyTrackingService.ClearRowDirty(
                                dirtyStateTracker,
                                listDisplayService,
                                ref viewModel.HasUnsavedChanges,
                                batch.ListIndex,
                                elementIndex);
                        }

                        if (fieldWasDirty)
                        {
                            mainWindowDirtyTrackingService.MarkFieldDirty(
                                dirtyStateTracker,
                                ref viewModel.HasUnsavedChanges,
                                batch.ListIndex,
                                elementIndex,
                                change.FieldIndex);
                        }
                        else
                        {
                            mainWindowDirtyTrackingService.ClearFieldDirty(
                                dirtyStateTracker,
                                ref viewModel.HasUnsavedChanges,
                                batch.ListIndex,
                                elementIndex,
                                change.FieldIndex);
                        }
                        mainWindowDirtyTrackingService.ClearFieldInvalid(
                            dirtyStateTracker,
                            batch.ListIndex,
                            elementIndex,
                            change.FieldIndex);
                    }
                }

                searchSuggestionService.ClearCache();
                InvalidateItemReferenceOptionCaches();
                InvalidateReferenceIndexAndDisplays();

                if (comboBox_lists.SelectedIndex != batch.ListIndex)
                {
                    comboBox_lists.SelectedIndex = batch.ListIndex;
                }
                else
                {
                    change_list(null, null);
                }

                RestoreElementGridSelection(batch.SelectedGridRows, batch.ActiveGridRow);
                change_item(null, null);
                RestoreValueGridSelection(batch);
                UpdateNpcSellServiceUiForSelection();
                if (viewModel != null)
                {
                    viewModel.HasUnsavedChanges = batch.PreviousHasUnsavedChanges;
                }
                return true;
            }
            finally
            {
                suppressValueEditUndoCapture = false;
            }
        }

        private void RestoreElementGridSelection(int[] selectedGridRows, int activeGridRow)
        {
            if (dataGridView_elems == null)
            {
                return;
            }

            dataGridView_elems.ClearSelection();
            if (selectedGridRows != null)
            {
                for (int i = 0; i < selectedGridRows.Length; i++)
                {
                    int rowIndex = selectedGridRows[i];
                    if (rowIndex >= 0 && rowIndex < dataGridView_elems.Rows.Count)
                    {
                        dataGridView_elems.Rows[rowIndex].Selected = true;
                    }
                }
            }

            int targetRow = activeGridRow;
            if (targetRow < 0 && selectedGridRows != null && selectedGridRows.Length > 0)
            {
                targetRow = selectedGridRows[0];
            }

            if (targetRow >= 0 && targetRow < dataGridView_elems.Rows.Count)
            {
                dataGridView_elems.CurrentCell = dataGridView_elems.Rows[targetRow].Cells[0];
            }
        }

        private void RestoreValueGridSelection(ValueEditUndoBatch batch)
        {
            if (dataGridView_item == null || batch == null)
            {
                return;
            }

            dataGridView_item.ClearSelection();
            int targetRow = -1;
            for (int i = 0; i < batch.Changes.Count; i++)
            {
                int rowIndex = batch.Changes[i].ValueRowIndex;
                if (rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
                {
                    continue;
                }

                dataGridView_item.Rows[rowIndex].Cells[2].Selected = true;
                if (targetRow < 0)
                {
                    targetRow = rowIndex;
                }
            }

            if (targetRow >= 0 && targetRow < dataGridView_item.Rows.Count)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[targetRow].Cells[2];
            }
        }

        private void GoToReferencedItemFromCurrentValueRow(bool showMessage)
        {
            if (dataGridView_item == null || dataGridView_item.CurrentCell == null)
            {
                if (showMessage)
                {
                    MessageBox.Show("Select a referenced value first.", "Go to referenced item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            GoToReferencedItemFromValueRow(dataGridView_item.CurrentCell.RowIndex, showMessage);
        }

        private bool CanResolveReferencedItemForValueRow(int rowIndex)
        {
            ItemReferenceOption option;
            return TryResolveReferencedItemForValueRow(rowIndex, out option);
        }

        private void GoToReferencedItemFromValueRow(int rowIndex, bool showMessage)
        {
            ItemReferenceOption option;
            if (!TryResolveReferencedItemForValueRow(rowIndex, out option))
            {
                if (showMessage)
                {
                    MessageBox.Show("This value does not point to a known item.", "Go to referenced item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            NavigateToElement(option.ListIndex, option.ElementIndex);
        }

        private bool TryResolveReferencedItemForValueRow(int rowIndex, out ItemReferenceOption option)
        {
            option = null;
            if (sessionService == null
                || sessionService.ListCollection == null
                || dataGridView_item == null
                || comboBox_lists == null
                || itemReferenceService == null
                || rowIndex < 0
                || rowIndex >= dataGridView_item.Rows.Count)
            {
                return false;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            string fieldName = ValueGridFieldNameService.GetFieldName(dataGridView_item, rowIndex);
            int elementIndex = ResolveCurrentElementIndex();
            if (!itemReferenceService.IsReferenceField(sessionService.ListCollection, listIndex, elementIndex, fieldName))
            {
                return false;
            }

            string rawValue = GetRawValueForValueRow(rowIndex);
            return itemReferenceService.TryResolveReferenceOption(
                sessionService.ListCollection,
                listIndex,
                elementIndex,
                fieldName,
                rawValue,
                sessionService.Database,
                iconResolutionService,
                out option);
        }

        private string GetRawValueForValueRow(int rowIndex)
        {
            if (dataGridView_item == null || rowIndex < 0 || rowIndex >= dataGridView_item.Rows.Count)
            {
                return string.Empty;
            }

            object raw = dataGridView_item.Rows[rowIndex].Cells[2].Tag;
            ValueCellState state = raw as ValueCellState;
            if (state != null)
            {
                return state.RawValue ?? string.Empty;
            }
            if (raw != null)
            {
                return Convert.ToString(raw);
            }

            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, rowIndex);
            int elementIndex = ResolveCurrentElementIndex();
            if (sessionService != null
                && sessionService.ListCollection != null
                && listIndex >= 0
                && fieldIndex >= 0
                && elementIndex >= 0
                && listIndex < sessionService.ListCollection.Lists.Length
                && elementIndex < sessionService.ListCollection.Lists[listIndex].elementValues.Length
                && fieldIndex < sessionService.ListCollection.Lists[listIndex].elementFields.Length)
            {
                return sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex);
            }

            return Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[2].Value);
        }

        private void NavigateToElement(int listIndex, int elementIndex)
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_elems == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || elementIndex < 0
                || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
            {
                return;
            }

            if (listIndex < comboBox_lists.Items.Count && comboBox_lists.SelectedIndex != listIndex)
            {
                comboBox_lists.SelectedIndex = listIndex;
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(() => NavigateToElement(listIndex, elementIndex)));
                }
                return;
            }

            if (elementIndex >= dataGridView_elems.Rows.Count)
            {
                return;
            }

            dataGridView_elems.ClearSelection();
            dataGridView_elems.CurrentCell = dataGridView_elems.Rows[elementIndex].Cells[0];
            dataGridView_elems.Rows[elementIndex].Selected = true;
            try
            {
                dataGridView_elems.FirstDisplayedScrollingRowIndex = elementIndex;
            }
            catch
            {
            }
            change_item(null, null);
            PersistNavigationState();
        }

    }
}




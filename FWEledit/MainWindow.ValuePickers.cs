using System;
using System.Drawing;
using System.Threading.Tasks;
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

        private void OpenItemReferencePickerForValueRow(int rowIndex)
        {
            mainWindowValuePickerCoordinatorService.OpenItemReferencePickerForValueRow(
                mainWindowValueRowPickerUiService,
                valueRowPickerUiService,
                sessionService,
                dataGridView_item,
                comboBox_lists.SelectedIndex,
                rowIndex,
                itemReferenceService,
                iconResolutionService,
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

            string fieldName = Convert.ToString(dataGridView_item.Rows[e.RowIndex].Cells[0].Value);
            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int fieldIndex = valueRowIndexService.GetFieldIndexForValueRow(dataGridView_item, e.RowIndex);
            int elementIndex = ResolveCurrentElementIndex();
            if (listIndex < 0 || fieldIndex < 0 || elementIndex < 0)
            {
                return;
            }

            string rawValue = sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex);
            if (creaturePortraitIconService.IsCreaturePortraitField(sessionService.ListCollection, listIndex, fieldName))
            {
                PaintPortraitValueCell(e, creaturePortraitIconService, rawValue);
                return;
            }

            ItemReferenceOption option;
            if (!itemReferenceService.TryResolveReferenceOption(
                sessionService.ListCollection,
                listIndex,
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
            Color textColor = ResolveReferenceValueTextColor(option.Quality, selected, e.CellStyle.ForeColor);

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
                && sessionService.Database.sourceBitmap != null
                && option != null
                && !string.IsNullOrWhiteSpace(option.IconKey)
                && sessionService.Database.ContainsKey(option.IconKey))
            {
                icon = sessionService.Database.images(option.IconKey);
            }

            graphics.DrawImage(icon, bounds);
        }

        private Color ResolveReferenceValueTextColor(int quality, bool selected, Color fallback)
        {
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
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return;
            }

            object current = dataGridView_item.Rows[rowIndex].Cells[2].Tag ?? dataGridView_item.Rows[rowIndex].Cells[2].Value;
            if (string.Equals(Convert.ToString(current), rawValue, StringComparison.Ordinal))
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

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
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
                string fieldName = Convert.ToString(dataGridView_item.Rows[i].Cells[0].Value);
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
                itemReferenceService,
                OpenIconPickerForValueRow,
                OpenAddonTypePickerForValueRow,
                OpenItemQualityPickerForValueRow,
                OpenModelPickerForValueRow,
                OpenItemReferencePickerForValueRow,
                UpdatePickIconButtonState,
                message => MessageBox.Show(message));
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Shift | Keys.G))
            {
                GoToReferencedItemFromCurrentValueRow(true);
                return true;
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

            if (e.ColumnIndex >= 0 && e.ColumnIndex < dataGridView_item.Columns.Count)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[e.RowIndex].Cells[e.ColumnIndex];
            }
            else if (dataGridView_item.Columns.Count > 2)
            {
                dataGridView_item.CurrentCell = dataGridView_item.Rows[e.RowIndex].Cells[2];
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

            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            bool isModelField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelFieldName(fieldName);
            bool isModelPreviewField = itemFieldClassifierService != null && itemFieldClassifierService.IsModelUsageFieldName(fieldName);
            bool isIconField = itemFieldClassifierService != null && itemFieldClassifierService.IsIconFieldName(fieldName);
            bool isPortraitIconField = creaturePortraitIconService.IsCreaturePortraitField(sessionService != null ? sessionService.ListCollection : null, comboBox_lists.SelectedIndex, fieldName);
            bool isAddonTypeField = itemFieldClassifierService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemFieldClassifierService.IsAddonTypeField(sessionService.ListCollection, comboBox_lists.SelectedIndex, fieldName);
            bool isItemQualityField = itemFieldClassifierService != null && itemFieldClassifierService.IsItemQualityFieldName(fieldName);
            bool isReferenceField = itemReferenceService != null
                && sessionService != null
                && sessionService.ListCollection != null
                && itemReferenceService.IsReferenceField(sessionService.ListCollection, comboBox_lists.SelectedIndex, fieldName);

            ContextMenuStrip menu = new ContextMenuStrip();

            if (isReferenceField && CanResolveReferencedItemForValueRow(rowIndex))
            {
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

            if (menu.Items.Count == 0)
            {
                menu.Dispose();
                return;
            }

            menu.Show(screenLocation);
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
            string fieldName = Convert.ToString(dataGridView_item.Rows[rowIndex].Cells[0].Value);
            if (!itemReferenceService.IsReferenceField(sessionService.ListCollection, listIndex, fieldName))
            {
                return false;
            }

            string rawValue = GetRawValueForValueRow(rowIndex);
            return itemReferenceService.TryResolveReferenceOption(
                sessionService.ListCollection,
                listIndex,
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
            PersistNavigationState();
        }

    }
}



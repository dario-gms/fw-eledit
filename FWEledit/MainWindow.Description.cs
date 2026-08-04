using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void ApplyItemDescriptionRuntime(string[] data)
        {
            mainWindowDescriptionCoordinatorService.ApplyItemDescriptionRuntime(
                descriptionRuntimeBridgeService,
                sessionService,
                data,
                sessionService.Database);
        }

        private void LoadItemDescriptionsFromConfigs()
        {
            AssetManager manager = sessionService != null ? sessionService.AssetManager : null;
            if (manager != null)
            {
                manager.EnsurePackageExtracted("configs");
            }

            string resolvedDescriptionPath = ResolveItemDescriptionFilePath();
            if (!string.IsNullOrWhiteSpace(resolvedDescriptionPath) && File.Exists(resolvedDescriptionPath))
            {
                viewModel.DescriptionViewModel.LoadFromFile(resolvedDescriptionPath);
                if (fwDescriptionStatusLabel != null && !string.IsNullOrWhiteSpace(viewModel.DescriptionViewModel.StatusText))
                {
                    fwDescriptionStatusLabel.Text = viewModel.DescriptionViewModel.StatusText;
                }

                descriptionLoadService.SyncRuntime(
                    viewModel.DescriptionViewModel,
                    descriptionRuntimeService,
                    ApplyItemDescriptionRuntime);
                return;
            }

            mainWindowDescriptionCoordinatorService.LoadItemDescriptionsFromConfigs(
                mainWindowDescriptionUiService,
                descriptionLoadUiService,
                descriptionLoadService,
                viewModel,
                itemDescriptionFileService,
                descriptionRuntimeService,
                AssetManager.GameRootPath,
                AssetManager.WorkspaceRootPath,
                status =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = status;
                    }
                },
                ApplyItemDescriptionRuntime);
        }

        private void UpdateDescriptionTabForSelection()
        {
            if (UpdateColorPreviewTabForSelection())
            {
                return;
            }

            if (CurrentListIsAddonPackageConfig())
            {
                RemoveDescriptionTabForAddonPackageConfig();
                ConfigureAddonPackageDescMode(false);
                return;
            }

            RestoreDescriptionTabIfNeeded();
            ConfigureAddonPackageDescMode(false);

            bool supportsDescriptions = CurrentListSupportsItemDescriptions();
            if (fwDescriptionEditor != null)
            {
                fwDescriptionEditor.ReadOnly = !supportsDescriptions;
            }

            if (!supportsDescriptions)
            {
                if (viewModel != null)
                {
                    viewModel.IsUpdatingDescriptionUi = true;
                    try
                    {
                        if (viewModel.DescriptionViewModel != null)
                        {
                            viewModel.DescriptionViewModel.GetEditorTextForItem(0, null);
                        }

                        if (fwDescriptionEditor != null)
                        {
                            fwDescriptionEditor.Text = string.Empty;
                        }

                        RenderDescriptionPreview(string.Empty);
                    }
                    finally
                    {
                        viewModel.IsUpdatingDescriptionUi = false;
                    }
                }

                return;
            }

            EnsureItemDescriptionsAvailable();
            ApplyDescriptionTabSelection();

            if (string.IsNullOrWhiteSpace(fwDescriptionEditor != null ? fwDescriptionEditor.Text : string.Empty)
                && TryGetCurrentDescriptionItemId(out int selectedItemId)
                && selectedItemId > 0
                && !HasLoadedItemDescriptions())
            {
                LoadItemDescriptionsFromConfigs();
                ApplyDescriptionTabSelection();
            }
        }

        private bool CurrentListIsAddonPackageConfig()
        {
            if (comboBox_lists == null || sessionService == null || sessionService.ListCollection == null)
            {
                return false;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0 || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return false;
            }

            return IsNamedConfigList(
                sessionService.ListCollection.Lists[listIndex].listName,
                "ADDON_PACKAGE_CONFIG");
        }

        private void RemoveDescriptionTabForAddonPackageConfig()
        {
            if (fwRightTabs == null || fwDescriptionTab == null)
            {
                return;
            }

            if (fwRightTabs.SelectedTab == fwDescriptionTab)
            {
                fwRightTabs.SelectedTab = fwValuesTab != null && fwRightTabs.TabPages.Contains(fwValuesTab)
                    ? fwValuesTab
                    : (fwRightTabs.TabPages.Count > 0 ? fwRightTabs.TabPages[0] : null);
            }

            if (fwRightTabs.TabPages.Contains(fwDescriptionTab))
            {
                fwRightTabs.TabPages.Remove(fwDescriptionTab);
            }
        }

        private void ApplyDescriptionTabSelection()
        {
            mainWindowDescriptionCoordinatorService.UpdateDescriptionTabForSelection(
                mainWindowDescriptionUiService,
                sessionService.ListCollection,
                comboBox_lists,
                dataGridView_elems,
                fwDescriptionEditor,
                viewModel,
                descriptionUiService,
                descriptionWorkflowService,
                ResolveDescriptionTextFallback,
                RenderDescriptionPreview);
        }

        private void EnsureItemDescriptionsAvailable()
        {
            if (HasLoadedItemDescriptions())
            {
                return;
            }

            LoadItemDescriptionsFromConfigs();
        }

        private bool HasLoadedItemDescriptions()
        {
            return sessionService != null
                && sessionService.Database != null
                && sessionService.Database.item_ext_desc != null
                && sessionService.Database.item_ext_desc.Length > 1;
        }

        private bool TryGetCurrentDescriptionItemId(out int itemId)
        {
            itemId = 0;
            if (dataGridView_elems == null
                || dataGridView_elems.CurrentCell == null
                || dataGridView_elems.CurrentCell.RowIndex < 0
                || dataGridView_elems.CurrentCell.RowIndex >= dataGridView_elems.Rows.Count)
            {
                return false;
            }

            object value = dataGridView_elems.Rows[dataGridView_elems.CurrentCell.RowIndex].Cells[0].Value;
            return int.TryParse(Convert.ToString(value), out itemId) && itemId > 0;
        }

        private string ResolveDescriptionTextFallback(int itemId)
        {
            string runtimeText = Extensions.ItemDesc(sessionService, itemId);
            if (!string.IsNullOrWhiteSpace(runtimeText))
            {
                return runtimeText;
            }

            return TryReadRawItemDescriptionFromFile(itemId, out string rawText)
                ? rawText
                : string.Empty;
        }

        private bool TryReadRawItemDescriptionFromFile(int itemId, out string rawText)
        {
            rawText = string.Empty;
            if (itemId <= 0)
            {
                return false;
            }

            string filePath = ResolveItemDescriptionFilePath();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return false;
            }

            try
            {
                Regex rx = new Regex("^\\s*(\\d+)\\s+\"(.*)\"\\s*$", RegexOptions.Compiled);
                using (StreamReader sr = new StreamReader(filePath, Encoding.Unicode, true))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith("//"))
                        {
                            continue;
                        }

                        Match match = rx.Match(line);
                        if (!match.Success)
                        {
                            continue;
                        }

                        if (int.TryParse(match.Groups[1].Value, out int parsedItemId) && parsedItemId == itemId)
                        {
                            rawText = match.Groups[2].Value ?? string.Empty;
                            return true;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private string ResolveItemDescriptionFilePath()
        {
            AssetManager manager = sessionService != null ? sessionService.AssetManager : null;
            if (manager != null)
            {
                manager.EnsurePackageExtracted("configs");
                string resolved = manager.ResolveResourceFilePublic(
                    Path.Combine("data", "item_ext_desc.txt"),
                    "item_ext_desc.txt",
                    Path.Combine("configs.pck.files", "item_ext_desc.txt"));
                if (!string.IsNullOrWhiteSpace(resolved))
                {
                    return resolved;
                }
            }

            return itemDescriptionFileService.ResolveItemExtDescFilePath(
                AssetManager.GameRootPath,
                AssetManager.WorkspaceRootPath);
        }

        private bool CurrentListSupportsItemDescriptions()
        {
            if (sessionService == null || sessionService.ListCollection == null || comboBox_lists == null)
            {
                return false;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0 || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return false;
            }

            if (listIndex == sessionService.ListCollection.ConversationListIndex)
            {
                return false;
            }

            return listIndex != 0;
        }

        private bool UpdateColorPreviewTabForSelection()
        {
            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            if (!IsColorPlanConfigList(listIndex))
            {
                RestoreDescriptionTabIfNeeded();
                return false;
            }

            EnsureColorPreviewTabCreated();
            SwapDescriptionTabForColorPreview();

            if (!TryGetCurrentColorPlanContext(
                out int contextListIndex,
                out int elementIndex,
                out int redMinFieldIndex,
                out int redMaxFieldIndex,
                out int greenMinFieldIndex,
                out int greenMaxFieldIndex,
                out int blueMinFieldIndex,
                out int blueMaxFieldIndex))
            {
                RenderStandaloneColorPreview(0, 0, 0, 0, 0, 0);
                return true;
            }

            RenderStandaloneColorPreview(
                ParseColorPlanChannelValue(contextListIndex, elementIndex, redMinFieldIndex),
                ParseColorPlanChannelValue(contextListIndex, elementIndex, redMaxFieldIndex),
                ParseColorPlanChannelValue(contextListIndex, elementIndex, greenMinFieldIndex),
                ParseColorPlanChannelValue(contextListIndex, elementIndex, greenMaxFieldIndex),
                ParseColorPlanChannelValue(contextListIndex, elementIndex, blueMinFieldIndex),
                ParseColorPlanChannelValue(contextListIndex, elementIndex, blueMaxFieldIndex));
            return true;
        }

        private bool IsColorPlanConfigList(int listIndex)
        {
            return sessionService != null
                && sessionService.ListCollection != null
                && listIndex >= 0
                && listIndex < sessionService.ListCollection.Lists.Length
                && sessionService.ListCollection.Lists[listIndex] != null
                && IsNamedConfigList(
                    sessionService.ListCollection.Lists[listIndex].listName,
                    "COLOR_PLAN_CONFIG");
        }

        private static bool IsNamedConfigList(string listName, string expectedName)
        {
            if (string.IsNullOrWhiteSpace(listName) || string.IsNullOrWhiteSpace(expectedName))
            {
                return false;
            }

            string normalizedListName = listName.Trim();
            string normalizedExpectedName = expectedName.Trim();
            return string.Equals(normalizedListName, normalizedExpectedName, StringComparison.OrdinalIgnoreCase)
                || normalizedListName.EndsWith(" - " + normalizedExpectedName, StringComparison.OrdinalIgnoreCase);
        }

        private void EnsureColorPreviewTabCreated()
        {
            if (fwColorPreviewTab != null)
            {
                return;
            }

            fwColorPreviewTab = new TabPage("Color Preview");
            fwColorPreviewTab.Padding = new Padding(8);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = new Padding(0);
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Panel headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Margin = new Padding(0);

            fwColorPreviewGenerateButton = new Button();
            fwColorPreviewGenerateButton.Text = "Color generator";
            fwColorPreviewGenerateButton.Dock = DockStyle.Right;
            fwColorPreviewGenerateButton.Width = 150;
            fwColorPreviewGenerateButton.Click += click_color_preview_generate;
            headerPanel.Controls.Add(fwColorPreviewGenerateButton);

            fwColorPreviewSwatch = new Panel();
            fwColorPreviewSwatch.Dock = DockStyle.Fill;
            fwColorPreviewSwatch.Margin = new Padding(0);
            fwColorPreviewSwatch.BorderStyle = BorderStyle.FixedSingle;
            fwColorPreviewSwatch.BackColor = Color.Black;

            fwColorPreviewValueLabel = new Label();
            fwColorPreviewValueLabel.Dock = DockStyle.Fill;
            fwColorPreviewValueLabel.TextAlign = ContentAlignment.MiddleCenter;
            fwColorPreviewValueLabel.Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)));
            fwColorPreviewValueLabel.Text = "#000000";
            fwColorPreviewSwatch.Controls.Add(fwColorPreviewValueLabel);

            layout.Controls.Add(headerPanel, 0, 0);
            layout.Controls.Add(fwColorPreviewSwatch, 0, 1);
            fwColorPreviewTab.Controls.Add(layout);
        }

        private void SwapDescriptionTabForColorPreview()
        {
            if (fwRightTabs == null || fwDescriptionTab == null || fwColorPreviewTab == null)
            {
                return;
            }

            if (fwRightTabs.TabPages.Contains(fwColorPreviewTab))
            {
                return;
            }

            int descriptionIndex = fwRightTabs.TabPages.Contains(fwDescriptionTab)
                ? fwRightTabs.TabPages.IndexOf(fwDescriptionTab)
                : fwRightTabs.TabPages.Count;
            bool selectReplacement = fwRightTabs.SelectedTab == fwDescriptionTab;

            if (fwRightTabs.TabPages.Contains(fwDescriptionTab))
            {
                fwRightTabs.TabPages.Remove(fwDescriptionTab);
            }

            fwRightTabs.TabPages.Insert(Math.Min(descriptionIndex, fwRightTabs.TabPages.Count), fwColorPreviewTab);
            if (selectReplacement)
            {
                fwRightTabs.SelectedTab = fwColorPreviewTab;
            }
        }

        private void RestoreDescriptionTabIfNeeded()
        {
            if (fwRightTabs == null || fwDescriptionTab == null || fwColorPreviewTab == null)
            {
                return;
            }

            if (!fwRightTabs.TabPages.Contains(fwColorPreviewTab))
            {
                return;
            }

            int colorPreviewIndex = fwRightTabs.TabPages.IndexOf(fwColorPreviewTab);
            bool selectReplacement = fwRightTabs.SelectedTab == fwColorPreviewTab;
            fwRightTabs.TabPages.Remove(fwColorPreviewTab);

            if (!fwRightTabs.TabPages.Contains(fwDescriptionTab))
            {
                fwRightTabs.TabPages.Insert(Math.Min(colorPreviewIndex, fwRightTabs.TabPages.Count), fwDescriptionTab);
            }

            if (selectReplacement)
            {
                fwRightTabs.SelectedTab = fwDescriptionTab;
            }
        }

        private void RenderStandaloneColorPreview(
            int redMin,
            int redMax,
            int greenMin,
            int greenMax,
            int blueMin,
            int blueMax)
        {
            if (fwColorPreviewSwatch == null || fwColorPreviewValueLabel == null)
            {
                return;
            }

            int previewRed = (redMin + redMax) / 2;
            int previewGreen = (greenMin + greenMax) / 2;
            int previewBlue = (blueMin + blueMax) / 2;
            Color previewColor = Color.FromArgb(previewRed, previewGreen, previewBlue);

            fwColorPreviewSwatch.BackColor = previewColor;
            fwColorPreviewValueLabel.ForeColor = GetPreviewContrastColor(previewColor);
            fwColorPreviewValueLabel.Text =
                "#" + previewRed.ToString("X2") + previewGreen.ToString("X2") + previewBlue.ToString("X2")
                + Environment.NewLine
                + "R " + redMin + "-" + redMax
                + "  G " + greenMin + "-" + greenMax
                + "  B " + blueMin + "-" + blueMax;
        }

        private void click_color_preview_generate(object sender, EventArgs e)
        {
            TryOpenColorPlanGenerator();
        }

        private bool TryApplyColorPlanPreviewModeForSelection()
        {
            if (!TryGetCurrentColorPlanContext(
                out int listIndex,
                out int elementIndex,
                out int redMinFieldIndex,
                out int redMaxFieldIndex,
                out int greenMinFieldIndex,
                out int greenMaxFieldIndex,
                out int blueMinFieldIndex,
                out int blueMaxFieldIndex))
            {
                return false;
            }

            ConfigureColorPlanPreviewMode(true);

            int redMin = ParseColorPlanChannelValue(listIndex, elementIndex, redMinFieldIndex);
            int redMax = ParseColorPlanChannelValue(listIndex, elementIndex, redMaxFieldIndex);
            int greenMin = ParseColorPlanChannelValue(listIndex, elementIndex, greenMinFieldIndex);
            int greenMax = ParseColorPlanChannelValue(listIndex, elementIndex, greenMaxFieldIndex);
            int blueMin = ParseColorPlanChannelValue(listIndex, elementIndex, blueMinFieldIndex);
            int blueMax = ParseColorPlanChannelValue(listIndex, elementIndex, blueMaxFieldIndex);

            RenderColorPlanPreview(redMin, redMax, greenMin, greenMax, blueMin, blueMax);

            if (fwDescriptionStatusLabel != null)
            {
                fwDescriptionStatusLabel.Text = "Preview from COLOR_PLAN_CONFIG";
            }

            return true;
        }

        private void ConfigureColorPlanPreviewMode(bool enabled)
        {
            if (fwDescriptionTab != null)
            {
                fwDescriptionTab.Text = enabled ? "Color Preview" : "Description";
            }

            if (fwDescriptionSaveButton != null)
            {
                fwDescriptionSaveButton.Text = enabled ? "Color generator" : "Stage Description";
            }

            if (fwDescriptionPreview != null)
            {
                fwDescriptionPreview.ReadOnly = true;
                fwDescriptionPreview.WordWrap = true;
                fwDescriptionPreview.ScrollBars = RichTextBoxScrollBars.Vertical;
                fwDescriptionPreview.BorderStyle = fwDarkMode ? BorderStyle.None : BorderStyle.FixedSingle;
                fwDescriptionPreview.Font = enabled
                    ? new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point, ((byte)(0)))
                    : new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

                if (!enabled)
                {
                    fwDescriptionPreview.BackColor = Color.FromArgb(24, 26, 30);
                    fwDescriptionPreview.ForeColor = Color.White;
                }
            }

            if (fwDescriptionEditor != null)
            {
                fwDescriptionEditor.Visible = !enabled;
            }

            if (fwDescriptionColorButton != null)
            {
                fwDescriptionColorButton.Visible = !enabled;
            }

            if (fwDescriptionLineBreakButton != null)
            {
                fwDescriptionLineBreakButton.Visible = !enabled;
            }

            if (fwDescriptionNormalFontButton != null)
            {
                fwDescriptionNormalFontButton.Visible = !enabled;
            }

            if (fwDescriptionSmallFontButton != null)
            {
                fwDescriptionSmallFontButton.Visible = !enabled;
            }

            if (fwDescriptionTitleFontButton != null)
            {
                fwDescriptionTitleFontButton.Visible = !enabled;
            }
        }

        private bool TryGetCurrentColorPlanContext(
            out int listIndex,
            out int elementIndex,
            out int redMinFieldIndex,
            out int redMaxFieldIndex,
            out int greenMinFieldIndex,
            out int greenMaxFieldIndex,
            out int blueMinFieldIndex,
            out int blueMaxFieldIndex)
        {
            listIndex = -1;
            elementIndex = -1;
            redMinFieldIndex = -1;
            redMaxFieldIndex = -1;
            greenMinFieldIndex = -1;
            greenMaxFieldIndex = -1;
            blueMinFieldIndex = -1;
            blueMaxFieldIndex = -1;

            if (sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_elems == null)
            {
                return false;
            }

            listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0 || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return false;
            }

            eList list = sessionService.ListCollection.Lists[listIndex];
            if (list == null
                || list.elementFields == null
                || !IsNamedConfigList(list.listName, "COLOR_PLAN_CONFIG"))
            {
                return false;
            }

            for (int fieldIndex = 0; fieldIndex < list.elementFields.Length; fieldIndex++)
            {
                string fieldName = list.elementFields[fieldIndex] ?? string.Empty;
                if (string.Equals(fieldName, "red_min", StringComparison.OrdinalIgnoreCase))
                {
                    redMinFieldIndex = fieldIndex;
                }
                else if (string.Equals(fieldName, "red_max", StringComparison.OrdinalIgnoreCase))
                {
                    redMaxFieldIndex = fieldIndex;
                }
                else if (string.Equals(fieldName, "green_min", StringComparison.OrdinalIgnoreCase))
                {
                    greenMinFieldIndex = fieldIndex;
                }
                else if (string.Equals(fieldName, "green_max", StringComparison.OrdinalIgnoreCase))
                {
                    greenMaxFieldIndex = fieldIndex;
                }
                else if (string.Equals(fieldName, "blue_min", StringComparison.OrdinalIgnoreCase))
                {
                    blueMinFieldIndex = fieldIndex;
                }
                else if (string.Equals(fieldName, "blue_max", StringComparison.OrdinalIgnoreCase))
                {
                    blueMaxFieldIndex = fieldIndex;
                }
            }

            if (redMinFieldIndex < 0
                || redMaxFieldIndex < 0
                || greenMinFieldIndex < 0
                || greenMaxFieldIndex < 0
                || blueMinFieldIndex < 0
                || blueMaxFieldIndex < 0
                || dataGridView_elems.CurrentCell == null)
            {
                return false;
            }

            int gridRowIndex = dataGridView_elems.CurrentCell.RowIndex;
            elementIndex = elementIndexResolverService != null
                ? elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    gridRowIndex,
                    dataGridView_elems)
                : gridRowIndex;

            return elementIndex >= 0
                && list.elementValues != null
                && elementIndex < list.elementValues.Length;
        }

        private int ParseColorPlanChannelValue(int listIndex, int elementIndex, int fieldIndex)
        {
            if (listIndex < 0 || elementIndex < 0 || fieldIndex < 0)
            {
                return 0;
            }

            string rawValue = sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex);
            return ClampColorChannel(rawValue);
        }

        private static int ClampColorChannel(string rawValue)
        {
            int parsedValue;
            if (!int.TryParse(rawValue, out parsedValue))
            {
                parsedValue = 0;
            }

            if (parsedValue < 0)
            {
                return 0;
            }

            if (parsedValue > 255)
            {
                return 255;
            }

            return parsedValue;
        }

        private void RenderColorPlanPreview(
            int redMin,
            int redMax,
            int greenMin,
            int greenMax,
            int blueMin,
            int blueMax)
        {
            if (fwDescriptionPreview == null)
            {
                return;
            }

            int previewRed = (redMin + redMax) / 2;
            int previewGreen = (greenMin + greenMax) / 2;
            int previewBlue = (blueMin + blueMax) / 2;
            Color previewColor = Color.FromArgb(previewRed, previewGreen, previewBlue);
            Color contrastColor = GetPreviewContrastColor(previewColor);

            fwDescriptionPreview.BackColor = previewColor;
            fwDescriptionPreview.ForeColor = contrastColor;
            fwDescriptionPreview.Text =
                "Preview color" + Environment.NewLine
                + "#" + previewRed.ToString("X2") + previewGreen.ToString("X2") + previewBlue.ToString("X2") + Environment.NewLine
                + Environment.NewLine
                + "Red: " + redMin + " - " + redMax + Environment.NewLine
                + "Green: " + greenMin + " - " + greenMax + Environment.NewLine
                + "Blue: " + blueMin + " - " + blueMax;
        }

        private static Color GetPreviewContrastColor(Color backgroundColor)
        {
            double luminance =
                (backgroundColor.R * 0.299d)
                + (backgroundColor.G * 0.587d)
                + (backgroundColor.B * 0.114d);

            return luminance >= 160d ? Color.Black : Color.White;
        }

        private bool TryOpenColorPlanGenerator()
        {
            if (!TryGetCurrentColorPlanContext(
                out int listIndex,
                out int elementIndex,
                out int redMinFieldIndex,
                out int redMaxFieldIndex,
                out int greenMinFieldIndex,
                out int greenMaxFieldIndex,
                out int blueMinFieldIndex,
                out int blueMaxFieldIndex))
            {
                return false;
            }

            Color initialColor = Color.FromArgb(
                (ParseColorPlanChannelValue(listIndex, elementIndex, redMinFieldIndex)
                    + ParseColorPlanChannelValue(listIndex, elementIndex, redMaxFieldIndex)) / 2,
                (ParseColorPlanChannelValue(listIndex, elementIndex, greenMinFieldIndex)
                    + ParseColorPlanChannelValue(listIndex, elementIndex, greenMaxFieldIndex)) / 2,
                (ParseColorPlanChannelValue(listIndex, elementIndex, blueMinFieldIndex)
                    + ParseColorPlanChannelValue(listIndex, elementIndex, blueMaxFieldIndex)) / 2);

            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = initialColor;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return true;
                }

                ApplyColorPlanChannelValue(listIndex, elementIndex, redMinFieldIndex, dialog.Color.R);
                ApplyColorPlanChannelValue(listIndex, elementIndex, redMaxFieldIndex, dialog.Color.R);
                ApplyColorPlanChannelValue(listIndex, elementIndex, greenMinFieldIndex, dialog.Color.G);
                ApplyColorPlanChannelValue(listIndex, elementIndex, greenMaxFieldIndex, dialog.Color.G);
                ApplyColorPlanChannelValue(listIndex, elementIndex, blueMinFieldIndex, dialog.Color.B);
                ApplyColorPlanChannelValue(listIndex, elementIndex, blueMaxFieldIndex, dialog.Color.B);
            }

            UpdateDescriptionTabForSelection();
            UpdateRawValueEditorFromCurrentCell();

            return true;
        }

        private void ApplyColorPlanChannelValue(int listIndex, int elementIndex, int fieldIndex, int value)
        {
            string valueText = value.ToString();
            string currentValue = sessionService.ListCollection.GetValue(listIndex, elementIndex, fieldIndex) ?? string.Empty;
            if (string.Equals(currentValue, valueText, StringComparison.Ordinal))
            {
                return;
            }

            sessionService.ListCollection.SetValue(listIndex, elementIndex, fieldIndex, valueText);
            mainWindowDirtyTrackingService.MarkRowDirty(
                dirtyStateTracker,
                listDisplayService,
                ref viewModel.HasUnsavedChanges,
                listIndex,
                elementIndex);
            mainWindowDirtyTrackingService.MarkFieldDirty(
                dirtyStateTracker,
                ref viewModel.HasUnsavedChanges,
                listIndex,
                elementIndex,
                fieldIndex);

            if (dataGridView_item == null)
            {
                return;
            }

            string fieldName = sessionService.ListCollection.Lists[listIndex].elementFields[fieldIndex];
            for (int rowIndex = 0; rowIndex < dataGridView_item.Rows.Count; rowIndex++)
            {
                if (string.Equals(
                    ValueGridFieldNameService.GetFieldName(dataGridView_item, rowIndex),
                    fieldName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    dataGridView_item.Rows[rowIndex].Cells[2].Value = valueText;
                    break;
                }
            }
        }

        private bool TryApplyAddonPackageDescModeForSelection()
        {
            if (!TryGetCurrentAddonPackageDescContext(out int listIndex, out int elementIndex, out int descFieldIndex))
            {
                return false;
            }

            ConfigureAddonPackageDescMode(true);

            string descText = NormalizeAddonPackageDescForEditor(
                sessionService.ListCollection.GetValue(listIndex, elementIndex, descFieldIndex) ?? string.Empty);
            viewModel.IsUpdatingDescriptionUi = true;
            try
            {
                if (fwDescriptionPreview != null && !string.Equals(fwDescriptionPreview.Text, descText, StringComparison.Ordinal))
                {
                    fwDescriptionPreview.Text = descText;
                }

                if (fwDescriptionEditor != null && !string.Equals(fwDescriptionEditor.Text, descText, StringComparison.Ordinal))
                {
                    fwDescriptionEditor.Text = descText;
                }
            }
            finally
            {
                viewModel.IsUpdatingDescriptionUi = false;
            }

            if (fwDescriptionStatusLabel != null)
            {
                fwDescriptionStatusLabel.Text = "Editing desc from elements.data";
            }

            return true;
        }

        private void ConfigureAddonPackageDescMode(bool enabled)
        {
            if (fwDescriptionTab != null)
            {
                fwDescriptionTab.Text = enabled ? "desc" : "Description";
            }

            if (fwDescriptionSaveButton != null)
            {
                fwDescriptionSaveButton.Text = enabled ? "Apply desc" : "Stage Description";
            }

            if (fwDescriptionPreview != null)
            {
                fwDescriptionPreview.ReadOnly = !enabled;
                fwDescriptionPreview.WordWrap = !enabled;
                fwDescriptionPreview.ScrollBars = enabled
                    ? RichTextBoxScrollBars.Both
                    : RichTextBoxScrollBars.Vertical;
                fwDescriptionPreview.Font = enabled
                    ? new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)))
                    : new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

                if (enabled)
                {
                    fwDescriptionPreview.BackColor = fwDescriptionEditor != null
                        ? fwDescriptionEditor.BackColor
                        : Color.FromArgb(18, 21, 26);
                    fwDescriptionPreview.ForeColor = fwDescriptionEditor != null
                        ? fwDescriptionEditor.ForeColor
                        : Color.FromArgb(229, 234, 242);
                    fwDescriptionPreview.BorderStyle = fwDarkMode ? BorderStyle.None : BorderStyle.FixedSingle;
                }
                else
                {
                    fwDescriptionPreview.BackColor = Color.FromArgb(24, 26, 30);
                    fwDescriptionPreview.ForeColor = Color.White;
                    fwDescriptionPreview.BorderStyle = fwDarkMode ? BorderStyle.None : BorderStyle.FixedSingle;
                }
            }

            if (fwDescriptionEditor != null)
            {
                fwDescriptionEditor.Visible = !enabled;
            }

            if (fwDescriptionColorButton != null)
            {
                fwDescriptionColorButton.Visible = !enabled;
            }

            if (fwDescriptionLineBreakButton != null)
            {
                fwDescriptionLineBreakButton.Visible = !enabled;
            }

            if (fwDescriptionNormalFontButton != null)
            {
                fwDescriptionNormalFontButton.Visible = !enabled;
            }

            if (fwDescriptionSmallFontButton != null)
            {
                fwDescriptionSmallFontButton.Visible = !enabled;
            }

            if (fwDescriptionTitleFontButton != null)
            {
                fwDescriptionTitleFontButton.Visible = !enabled;
            }
        }

        private bool TryGetCurrentAddonPackageDescContext(out int listIndex, out int elementIndex, out int descFieldIndex)
        {
            listIndex = -1;
            elementIndex = -1;
            descFieldIndex = -1;

            if (sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_elems == null)
            {
                return false;
            }

            listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0 || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return false;
            }

            eList list = sessionService.ListCollection.Lists[listIndex];
            string listName = list != null ? list.listName : string.Empty;
            bool isAddonPackageConfig = string.Equals(listName, "ADDON_PACKAGE_CONFIG", StringComparison.OrdinalIgnoreCase)
                || listIndex == 105;
            if (!isAddonPackageConfig || list == null || list.elementFields == null)
            {
                return false;
            }

            for (int fieldIndex = 0; fieldIndex < list.elementFields.Length; fieldIndex++)
            {
                if (string.Equals(list.elementFields[fieldIndex], "desc", StringComparison.OrdinalIgnoreCase))
                {
                    descFieldIndex = fieldIndex;
                    break;
                }
            }

            if (descFieldIndex < 0)
            {
                return false;
            }

            if (dataGridView_elems.CurrentCell == null)
            {
                return false;
            }

            int gridRowIndex = dataGridView_elems.CurrentCell.RowIndex;
            elementIndex = elementIndexResolverService != null
                ? elementIndexResolverService.ResolveElementIndexFromGridRow(
                    sessionService.ListCollection,
                    listIndex,
                    gridRowIndex,
                    dataGridView_elems)
                : gridRowIndex;

            return elementIndex >= 0
                && list.elementValues != null
                && elementIndex < list.elementValues.Length;
        }

        private static string NormalizeAddonPackageDescForEditor(string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue))
            {
                return string.Empty;
            }

            return storedValue
                .Replace("\\r\\n", "\n")
                .Replace("\\n", "\n")
                .Replace("\\r", "\n")
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\n", Environment.NewLine);
        }

        private static string NormalizeAddonPackageDescForStorage(string editorValue, string currentStoredValue)
        {
            if (string.IsNullOrEmpty(editorValue))
            {
                return string.Empty;
            }

            string newlineToken = DetectAddonPackageDescNewlineToken(currentStoredValue);
            return editorValue
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\n", newlineToken);
        }

        private static string DetectAddonPackageDescNewlineToken(string storedValue)
        {
            if (!string.IsNullOrEmpty(storedValue))
            {
                if (storedValue.Contains("\\r\\n"))
                {
                    return "\\r\\n";
                }

                if (storedValue.Contains("\\n"))
                {
                    return "\\n";
                }

                if (storedValue.Contains("\\r"))
                {
                    return "\\r";
                }

                if (storedValue.Contains("\r\n"))
                {
                    return "\r\n";
                }

                if (storedValue.Contains("\n"))
                {
                    return "\n";
                }

                if (storedValue.Contains('\r'))
                {
                    return "\r";
                }
            }

            return "\\n";
        }

        private void addon_package_desc_changed(object sender, EventArgs e)
        {
            if (viewModel == null
                || viewModel.IsUpdatingDescriptionUi
                || fwDescriptionPreview == null
                || !TryGetCurrentAddonPackageDescContext(out _, out _, out _))
            {
                return;
            }

            if (fwDescriptionStatusLabel != null)
            {
                fwDescriptionStatusLabel.Text = "Editing desc from elements.data";
            }
        }

        private bool TryApplyAddonPackageDescChange()
        {
            if (!TryGetCurrentAddonPackageDescContext(out int listIndex, out int elementIndex, out int descFieldIndex))
            {
                return false;
            }

            string editorText = fwDescriptionPreview != null ? fwDescriptionPreview.Text ?? string.Empty : string.Empty;
            string currentText = sessionService.ListCollection.GetValue(listIndex, elementIndex, descFieldIndex) ?? string.Empty;
            string newText = NormalizeAddonPackageDescForStorage(editorText, currentText);
            if (string.Equals(currentText, newText, StringComparison.Ordinal))
            {
                if (fwDescriptionStatusLabel != null)
                {
                    fwDescriptionStatusLabel.Text = "desc already up to date";
                }

                return true;
            }

            sessionService.ListCollection.SetValue(listIndex, elementIndex, descFieldIndex, newText);
            mainWindowDirtyTrackingService.MarkRowDirty(
                dirtyStateTracker,
                listDisplayService,
                ref viewModel.HasUnsavedChanges,
                listIndex,
                elementIndex);
            mainWindowDirtyTrackingService.MarkFieldDirty(
                dirtyStateTracker,
                ref viewModel.HasUnsavedChanges,
                listIndex,
                elementIndex,
                descFieldIndex);

            if (dataGridView_item != null)
            {
                for (int rowIndex = 0; rowIndex < dataGridView_item.Rows.Count; rowIndex++)
                {
                    if (string.Equals(
                        ValueGridFieldNameService.GetFieldName(dataGridView_item, rowIndex),
                        "desc",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        dataGridView_item.Rows[rowIndex].Cells[2].Value = editorText;
                        break;
                    }
                }
            }

            if (fwDescriptionStatusLabel != null)
            {
                fwDescriptionStatusLabel.Text = "desc applied to elements.data";
            }

            return true;
        }

        private void fw_description_changed(object sender, EventArgs e)
        {
            mainWindowDescriptionCoordinatorService.HandleDescriptionChanged(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                fwDescriptionEditor,
                fwDescriptionPreview,
                RenderDescriptionPreview,
                () => StageCurrentDescriptionChange(false));
        }

        private void StageCurrentDescriptionChange(bool updateStatus)
        {
            DescriptionChangeResult result = mainWindowDescriptionCoordinatorService.StageCurrentDescriptionChange(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                descriptionWorkflowService,
                descriptionLoadService,
                descriptionRuntimeService,
                fwDescriptionEditor != null ? fwDescriptionEditor.Text : string.Empty,
                ApplyItemDescriptionRuntime,
                null,
                () => comboBox_lists.SelectedIndex,
                GetSelectedDescriptionItemIds,
                GetSelectedDescriptionElementIndices,
                null,
                updateStatus,
                status =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = status;
                    }
                });

            if (result != null && result.Changed)
            {
                viewModel.HasUnsavedChanges = dirtyStateTracker.HasAnyDirtyEntries()
                    || (viewModel.DescriptionViewModel != null && viewModel.DescriptionViewModel.HasPendingChanges);
                RefreshDescriptionDirtyRows();
            }
        }

        private int[] GetSelectedDescriptionItemIds()
        {
            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            int[] elementIndices = GetSelectedDescriptionElementIndices();
            if (sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || listIndex == sessionService.ListCollection.ConversationListIndex)
            {
                return new int[0];
            }

            List<int> ids = new List<int>();
            HashSet<int> seenIds = new HashSet<int>();
            for (int i = 0; i < elementIndices.Length; i++)
            {
                int elementIndex = elementIndices[i];
                if (elementIndex < 0 || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
                {
                    continue;
                }

                int itemId;
                if (int.TryParse(sessionService.ListCollection.GetValue(listIndex, elementIndex, 0), out itemId)
                    && itemId > 0
                    && seenIds.Add(itemId))
                {
                    ids.Add(itemId);
                }
            }

            return ids.ToArray();
        }

        private int[] GetSelectedDescriptionElementIndices()
        {
            int listIndex = comboBox_lists != null ? comboBox_lists.SelectedIndex : -1;
            if (sessionService == null
                || sessionService.ListCollection == null
                || dataGridView_elems == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || listIndex == sessionService.ListCollection.ConversationListIndex)
            {
                return new int[0];
            }

            int[] gridRows = gridSelectionService != null
                ? gridSelectionService.GetSelectedIndices(dataGridView_elems)
                : new int[0];
            if ((gridRows == null || gridRows.Length == 0) && dataGridView_elems.CurrentCell != null)
            {
                gridRows = new int[] { dataGridView_elems.CurrentCell.RowIndex };
            }

            List<int> elementIndices = new List<int>();
            HashSet<int> seenIndices = new HashSet<int>();
            for (int i = 0; i < gridRows.Length; i++)
            {
                int gridRow = gridRows[i];
                int elementIndex = elementIndexResolverService != null
                    ? elementIndexResolverService.ResolveElementIndexFromGridRow(sessionService.ListCollection, listIndex, gridRow, dataGridView_elems)
                    : gridRow;
                if (elementIndex >= 0
                    && elementIndex < sessionService.ListCollection.Lists[listIndex].elementValues.Length
                    && seenIndices.Add(elementIndex))
                {
                    elementIndices.Add(elementIndex);
                }
            }

            return elementIndices.ToArray();
        }

        private int[] GetSelectedDescriptionGridRows()
        {
            if (dataGridView_elems == null)
            {
                return new int[0];
            }

            int[] gridRows = gridSelectionService != null
                ? gridSelectionService.GetSelectedIndices(dataGridView_elems)
                : new int[0];
            if ((gridRows == null || gridRows.Length == 0) && dataGridView_elems.CurrentCell != null)
            {
                gridRows = new int[] { dataGridView_elems.CurrentCell.RowIndex };
            }

            return gridRows ?? new int[0];
        }

        private void RefreshDescriptionDirtyRows()
        {
            if (sessionService == null
                || sessionService.ListCollection == null
                || comboBox_lists == null
                || dataGridView_elems == null)
            {
                return;
            }

            int listIndex = comboBox_lists.SelectedIndex;
            if (listIndex < 0 || listIndex >= sessionService.ListCollection.Lists.Length)
            {
                return;
            }

            int nameFieldIndex = fieldIndexLookupService.GetNameFieldIndex(sessionService.ListCollection, listIndex);
            int[] gridRows = GetSelectedDescriptionGridRows();
            for (int i = 0; i < gridRows.Length; i++)
            {
                int gridRow = gridRows[i];
                if (gridRow < 0 || gridRow >= dataGridView_elems.Rows.Count)
                {
                    continue;
                }

                int elementIndex = elementIndexResolverService != null
                    ? elementIndexResolverService.ResolveElementIndexFromGridRow(sessionService.ListCollection, listIndex, gridRow, dataGridView_elems)
                    : gridRow;
                if (elementIndex < 0 || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
                {
                    continue;
                }

                dataGridView_elems.Rows[gridRow].Cells[2].Value = listDisplayService.ComposeListDisplayName(
                    sessionService,
                    sessionService.ListCollection,
                    listIndex,
                    elementIndex,
                    nameFieldIndex,
                    IsElementMarkedDirty(listIndex, elementIndex));
            }
        }

        private bool IsElementMarkedDirty(int listIndex, int elementIndex)
        {
            bool rowDirty = mainWindowDirtyTrackingService.IsRowDirty(dirtyStateTracker, listIndex, elementIndex);
            if (rowDirty)
            {
                return true;
            }

            if (viewModel == null
                || viewModel.DescriptionViewModel == null
                || sessionService == null
                || sessionService.ListCollection == null
                || listIndex < 0
                || listIndex >= sessionService.ListCollection.Lists.Length
                || listIndex == sessionService.ListCollection.ConversationListIndex
                || elementIndex < 0
                || elementIndex >= sessionService.ListCollection.Lists[listIndex].elementValues.Length)
            {
                return false;
            }

            int itemId;
            return int.TryParse(sessionService.ListCollection.GetValue(listIndex, elementIndex, 0), out itemId)
                && viewModel.DescriptionViewModel.HasPendingChangeForItem(itemId);
        }

        private bool FlushPendingDescriptionsToDisk()
        {
            return mainWindowDescriptionCoordinatorService.FlushPendingDescriptionsToDisk(
                mainWindowDescriptionUiService,
                descriptionFlushUiService,
                descriptionWorkflowService,
                descriptionLoadService,
                descriptionRuntimeService,
                viewModel,
                sessionService.AssetManager,
                message => MessageBox.Show(message),
                status =>
                {
                    if (fwDescriptionStatusLabel != null)
                    {
                        fwDescriptionStatusLabel.Text = status;
                    }
                },
                ApplyItemDescriptionRuntime);
        }
        private void RemapDescriptionIdIfNeeded(int oldId, int newId)
        {
            mainWindowDescriptionCoordinatorService.RemapDescriptionIdIfNeeded(
                descriptionIdRemapService,
                viewModel.DescriptionViewModel,
                descriptionLoadService,
                descriptionRuntimeService,
                oldId,
                newId,
                ApplyItemDescriptionRuntime,
                () => viewModel.HasUnsavedChanges = true);
        }

        private void RenderDescriptionPreview(string rawText)
        {
            mainWindowDescriptionCoordinatorService.RenderDescriptionPreview(
                mainWindowDescriptionUiService,
                descriptionPreviewUiService,
                descriptionPreviewService,
                fwDescriptionPreview,
                rawText);
        }

        private void InitializeDescriptionFormattingActions()
        {
            if (fwDescriptionPreview != null)
            {
                fwDescriptionPreview.TextChanged += addon_package_desc_changed;
            }
            if (fwDescriptionColorButton != null)
            {
                fwDescriptionColorButton.Click += click_description_color;
            }
            if (fwDescriptionLineBreakButton != null)
            {
                fwDescriptionLineBreakButton.Click += (s, e) => InsertDescriptionText(Environment.NewLine);
            }
            if (fwDescriptionNormalFontButton != null)
            {
                fwDescriptionNormalFontButton.Click += (s, e) => InsertDescriptionTag("^O053", false);
            }
            if (fwDescriptionSmallFontButton != null)
            {
                fwDescriptionSmallFontButton.Click += (s, e) => InsertDescriptionTag("^O005", "^O053");
            }
            if (fwDescriptionTitleFontButton != null)
            {
                fwDescriptionTitleFontButton.Click += (s, e) => InsertDescriptionTag("^O057", "^O053");
            }
        }

        private void click_description_color(object sender, EventArgs e)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = Color.White;
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                InsertDescriptionTag("^" + ToFwColorTag(dialog.Color), true);
            }
        }

        private void InsertDescriptionTag(string tag, bool resetAfterSelection)
        {
            InsertDescriptionTag(tag, resetAfterSelection ? "^FFFFFF" : string.Empty);
        }

        private void InsertDescriptionTag(string tag, string resetTag)
        {
            if (fwDescriptionEditor == null || string.IsNullOrEmpty(tag))
            {
                return;
            }

            string selectedText = fwDescriptionEditor.SelectedText ?? string.Empty;
            if (selectedText.Length > 0)
            {
                string reset = resetTag ?? string.Empty;
                fwDescriptionEditor.SelectedText = tag + selectedText + reset;
            }
            else
            {
                fwDescriptionEditor.SelectedText = tag;
            }

            fwDescriptionEditor.Focus();
        }

        private void InsertDescriptionText(string text)
        {
            if (fwDescriptionEditor == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            fwDescriptionEditor.SelectedText = text;
            fwDescriptionEditor.Focus();
        }

        private static string ToFwColorTag(Color color)
        {
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private void click_save_description(object sender, EventArgs e)
        {
            if (TryOpenColorPlanGenerator())
            {
                return;
            }

            if (TryApplyAddonPackageDescChange())
            {
                return;
            }

            mainWindowDescriptionCoordinatorService.TrySaveCurrentDescription(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                () => StageCurrentDescriptionChange(true),
                message => MessageBox.Show(message));
        }
    }
}



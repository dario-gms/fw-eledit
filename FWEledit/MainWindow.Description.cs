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
            mainWindowDescriptionCoordinatorService.TrySaveCurrentDescription(
                mainWindowDescriptionUiService,
                descriptionUiService,
                viewModel,
                () => StageCurrentDescriptionChange(true),
                message => MessageBox.Show(message));
        }
    }
}



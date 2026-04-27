using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueRowPickerUiService
    {
        private int previewLoadInProgress;

        private sealed class ModelPreviewBuildResult
        {
            public bool Success { get; set; }
            public string Error { get; set; }
            public ModelPreviewMeshData MeshData { get; set; }
        }

        public void UpdateInlineIconButton(
            InlineIconButtonService inlineIconButtonService,
            Button inlineButton,
            DataGridView itemGrid,
            TabControl rightTabs,
            TabPage valuesTab,
            ItemFieldClassifierService fieldClassifier,
            ref int inlineRowIndex,
            bool suppressValuesUiRefresh)
        {
            if (inlineIconButtonService == null)
            {
                return;
            }

            inlineIconButtonService.UpdateInlineButton(
                inlineButton,
                itemGrid,
                rightTabs,
                valuesTab,
                fieldClassifier,
                ref inlineRowIndex,
                suppressValuesUiRefresh);
        }

        public void HandleInlinePickIconClick(
            DataGridView itemGrid,
            int inlineRowIndex,
            Action<int> openIconPicker)
        {
            if (itemGrid == null)
            {
                return;
            }

            int targetRow = inlineRowIndex;
            if (targetRow < 0 && itemGrid.CurrentCell != null)
            {
                targetRow = itemGrid.CurrentCell.RowIndex;
            }
            if (targetRow < 0)
            {
                return;
            }

            if (openIconPicker != null)
            {
                openIconPicker(targetRow);
            }
        }

        public void HandleCellDoubleClick(
            DataGridView itemGrid,
            eListCollection listCollection,
            int listIndex,
            DataGridViewCellEventArgs e,
            ItemFieldClassifierService fieldClassifier,
            Action<int> openIconPicker,
            Action<int> openAddonTypePicker,
            Action<int> openItemQualityPicker,
            Action<int> openModelPicker,
            Action updateInlineIconButton,
            Action<string> showError)
        {
            if (itemGrid == null || e == null || e.RowIndex < 0 || fieldClassifier == null)
            {
                return;
            }

            try
            {
                string fieldName = Convert.ToString(itemGrid.Rows[e.RowIndex].Cells[0].Value);
                if (fieldClassifier.IsIconFieldName(fieldName))
                {
                    if (openIconPicker != null)
                    {
                        openIconPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsAddonTypeField(listCollection, listIndex, fieldName))
                {
                    if (openAddonTypePicker != null)
                    {
                        openAddonTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsItemQualityFieldName(fieldName))
                {
                    if (openItemQualityPicker != null)
                    {
                        openItemQualityPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsModelFieldName(fieldName))
                {
                    if (openModelPicker != null)
                    {
                        openModelPicker(e.RowIndex);
                    }
                }

                if (updateInlineIconButton != null)
                {
                    updateInlineIconButton();
                }
            }
            catch (Exception ex)
            {
                if (showError != null)
                {
                    showError("FIELD ACTION ERROR!\n" + ex.Message);
                }
            }
        }

        public void OpenModelPickerForValueRow(
            eListCollection listCollection,
            CacheSave database,
            DataGridView itemGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            ModelPickerService modelPickerService,
            ModelPickerCacheService modelPickerCacheService,
            ModelPackageNotificationService modelPackageNotificationService,
            DialogService dialogService,
            PathIdResolutionService pathIdResolutionService,
            IWin32Window owner)
        {
            if (listCollection == null || database == null || itemGrid == null || listIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(itemGrid.Rows[rowIndex].Cells[0].Value);
            if (fieldClassifier == null || !fieldClassifier.IsModelFieldName(fieldName))
            {
                return;
            }

            int currentPathId = TryGetCurrentPathId(itemGrid, rowIndex, modelPickerService, pathIdResolutionService);
            int currentResolvedPathId = currentPathId;
            int resolvedCurrentPathId;
            string resolvedCurrentMappedPath;
            if (TryResolveModelPathById(database, currentPathId, fieldName, modelPickerService, out resolvedCurrentPathId, out resolvedCurrentMappedPath, true)
                && resolvedCurrentPathId > 0)
            {
                currentResolvedPathId = resolvedCurrentPathId;
            }

            List<ModelPickerEntry> entries = BuildModelPickerEntriesForList(
                listCollection,
                database,
                listIndex,
                modelPickerService,
                modelPickerCacheService,
                modelPackageNotificationService,
                dialogService,
                owner);

            using (ModelPickerWindow picker = new ModelPickerWindow(entries, currentResolvedPathId))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                int selectedPathId = picker.SelectedPathId;
                if (selectedPathId <= 0 || selectedPathId == currentPathId)
                {
                    return;
                }

                if (itemGrid.CurrentCell == null || itemGrid.CurrentCell.RowIndex != rowIndex)
                {
                    itemGrid.CurrentCell = itemGrid.Rows[rowIndex].Cells[2];
                }

                itemGrid.Rows[rowIndex].Cells[2].Value = selectedPathId.ToString();
            }
        }

        public void OpenAddonTypePickerForValueRow(
            ISessionService sessionService,
            eListCollection listCollection,
            DataGridView itemGrid,
            DataGridView elementGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            AddonTypeOptionService addonTypeOptionService,
            string appPath,
            string gameRootPath,
            Func<int, int> getFieldIndexForRow,
            Action<string> showMessage,
            IWin32Window owner)
        {
            if (listCollection == null || itemGrid == null || listIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(itemGrid.Rows[rowIndex].Cells[0].Value);
            if (fieldClassifier == null || !fieldClassifier.IsAddonTypeField(listCollection, listIndex, fieldName))
            {
                return;
            }

            int fieldIndex = getFieldIndexForRow != null ? getFieldIndexForRow(rowIndex) : -1;
            if (fieldIndex < 0)
            {
                return;
            }

            int currentType = 0;
            if (elementGrid != null && elementGrid.CurrentCell != null)
            {
                int itemRow = elementGrid.CurrentCell.RowIndex;
                if (itemRow >= 0 && itemRow < listCollection.Lists[listIndex].elementValues.Length)
                {
                    int.TryParse(listCollection.GetValue(listIndex, itemRow, fieldIndex), out currentType);
                }
            }
            if (currentType <= 0)
            {
                int.TryParse(Convert.ToString(itemGrid.Rows[rowIndex].Cells[2].Value), out currentType);
            }

            List<AddonTypeOption> options = addonTypeOptionService != null
                ? addonTypeOptionService.BuildOptions(sessionService, listCollection, listIndex, appPath, gameRootPath)
                : new List<AddonTypeOption>();
            if (options.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("No addon types found.");
                }
                return;
            }

            using (AddonTypePickerWindow picker = new AddonTypePickerWindow(options, currentType))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                itemGrid.Rows[rowIndex].Cells[2].Value = picker.SelectedType.ToString();
            }
        }

        public void OpenItemQualityPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IList<QualityOption> options,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(itemGrid.Rows[rowIndex].Cells[0].Value);
            if (fieldClassifier == null || !fieldClassifier.IsItemQualityFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = Convert.ToString(itemGrid.Rows[rowIndex].Cells[2].Value);
            int.TryParse(rawValue, out currentValue);

            List<QualityOption> safeOptions = options != null
                ? new List<QualityOption>(options)
                : new List<QualityOption>();
            using (QualityPickerWindow picker = new QualityPickerWindow(safeOptions, currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                itemGrid.Rows[rowIndex].Cells[2].Value = picker.SelectedValue.ToString(CultureInfo.InvariantCulture);
            }
        }

        public void OpenIconPickerForValueRow(
            eListCollection listCollection,
            CacheSave database,
            DataGridView itemGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            PathIdResolutionService pathIdResolutionService,
            ModelPickerService modelPickerService,
            ValueRowIndexService valueRowIndexService,
            IconUsageLookupService iconUsageLookupService,
            IWin32Window owner)
        {
            if (listCollection == null || database == null || itemGrid == null || listIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(itemGrid.Rows[rowIndex].Cells[0].Value);
            if (fieldClassifier == null || !fieldClassifier.IsIconFieldName(fieldName))
            {
                return;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                return;
            }

            int currentPathId = TryGetCurrentPathId(itemGrid, rowIndex, modelPickerService, pathIdResolutionService);
            int fieldIndex = valueRowIndexService != null ? valueRowIndexService.GetFieldIndexForValueRow(itemGrid, rowIndex) : -1;
            if (fieldIndex < 0)
            {
                return;
            }

            Dictionary<int, int> usageLookup = iconUsageLookupService != null
                ? iconUsageLookupService.BuildIconUsageLookup(listCollection, listIndex, fieldIndex)
                : new Dictionary<int, int>();

            using (IconPickerWindow picker = new IconPickerWindow(database, currentPathId, usageLookup))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                int selectedPathId = picker.SelectedPathId;
                if (selectedPathId <= 0)
                {
                    return;
                }

                if (itemGrid.CurrentCell == null || itemGrid.CurrentCell.RowIndex != rowIndex)
                {
                    itemGrid.CurrentCell = itemGrid.Rows[rowIndex].Cells[2];
                }

                if (currentPathId == selectedPathId)
                {
                    return;
                }

                itemGrid.Rows[rowIndex].Cells[2].Value = selectedPathId.ToString();
            }
        }

        public async void OpenModelPreviewForValueRow(
            AssetManager assetManager,
            CacheSave database,
            DataGridView itemGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            PathIdResolutionService pathIdResolutionService,
            ModelPickerService modelPickerService,
            ModelPreviewService modelPreviewService,
            Form owner,
            Action<string> showMessage)
        {
            if (assetManager == null || database == null || itemGrid == null || listIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = Convert.ToString(itemGrid.Rows[rowIndex].Cells[0].Value);
            if (fieldClassifier == null || !fieldClassifier.IsModelFieldName(fieldName))
            {
                return;
            }
            if (modelPreviewService == null)
            {
                return;
            }

            int pathId = TryGetCurrentPathId(itemGrid, rowIndex, modelPickerService, pathIdResolutionService);
            if (Interlocked.CompareExchange(ref previewLoadInProgress, 1, 0) != 0)
            {
                if (showMessage != null)
                {
                    showMessage("A model preview is already loading. Please wait.");
                }
                return;
            }

            bool restoreWaitCursor = owner != null && !owner.IsDisposed && owner.UseWaitCursor;
            try
            {
                if (owner != null && !owner.IsDisposed)
                {
                    owner.UseWaitCursor = true;
                }

                ModelPreviewBuildResult result = await Task.Run(delegate
                {
                    ModelPreviewBuildResult buildResult = new ModelPreviewBuildResult();
                    string errorMessage;
                    ModelPreviewMeshData meshData;
                    bool ok = modelPreviewService.TryBuildPreviewMeshData(
                        assetManager,
                        database,
                        pathId,
                        fieldName,
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
                    if (!string.IsNullOrWhiteSpace(result.Error) && showMessage != null)
                    {
                        showMessage(result.Error);
                    }
                    return;
                }

                modelPreviewService.ShowPreviewWindow(result.MeshData);
            }
            catch (Exception ex)
            {
                if (showMessage != null)
                {
                    showMessage("MODEL PREVIEW ERROR!\n" + ex.Message);
                }
            }
            finally
            {
                if (owner != null && !owner.IsDisposed)
                {
                    owner.UseWaitCursor = restoreWaitCursor;
                }
                Interlocked.Exchange(ref previewLoadInProgress, 0);
            }
        }

        private static List<ModelPickerEntry> BuildModelPickerEntriesForList(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            ModelPickerService modelPickerService,
            ModelPickerCacheService modelPickerCacheService,
            ModelPackageNotificationService modelPackageNotificationService,
            DialogService dialogService,
            IWin32Window owner)
        {
            if (listCollection == null || database == null || modelPickerService == null)
            {
                return new List<ModelPickerEntry>();
            }

            return modelPickerService.BuildModelPickerEntries(
                listCollection,
                database,
                listIndex,
                iconKey => database.images(iconKey),
                package =>
                {
                    if (modelPickerCacheService == null || !modelPickerCacheService.TryMarkMissingExtractNotified(package))
                    {
                        return;
                    }
                    if (dialogService != null && modelPackageNotificationService != null)
                    {
                        dialogService.ShowMessage(
                            modelPackageNotificationService.BuildMissingPackageMessage(package),
                            modelPackageNotificationService.Title,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            owner);
                    }
                });
        }

        private static int TryGetCurrentPathId(
            DataGridView itemGrid,
            int rowIndex,
            ModelPickerService modelPickerService,
            PathIdResolutionService pathIdResolutionService)
        {
            if (pathIdResolutionService == null || itemGrid == null)
            {
                return 0;
            }

            TryParsePathIdDelegate extractor = null;
            if (modelPickerService != null)
            {
                extractor = modelPickerService.TryExtractPathId;
            }

            return pathIdResolutionService.TryGetCurrentPathId(itemGrid, rowIndex, extractor);
        }

        private static bool TryResolveModelPathById(
            CacheSave database,
            int pathId,
            string fieldName,
            ModelPickerService modelPickerService,
            out int resolvedPathId,
            out string mappedPath,
            bool allowNeighborOffsets)
        {
            resolvedPathId = 0;
            mappedPath = string.Empty;
            if (modelPickerService == null)
            {
                return false;
            }

            return modelPickerService.TryResolveModelPathById(database, pathId, fieldName, out resolvedPathId, out mappedPath, allowNeighborOffsets);
        }
    }
}

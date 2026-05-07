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
        private bool liveModelPreviewEnabled;
        private int lastPreviewPathId;
        private int lastPreviewListIndex;
        private int lastPreviewRowIndex;

        public ValueRowPickerUiService()
        {
            lastPreviewPathId = 0;
            lastPreviewListIndex = -1;
            lastPreviewRowIndex = -1;
        }

        public bool IsLiveModelPreviewEnabled
        {
            get { return liveModelPreviewEnabled; }
        }

        public void DisableLiveModelPreview()
        {
            liveModelPreviewEnabled = false;
            lastPreviewPathId = 0;
            lastPreviewListIndex = -1;
            lastPreviewRowIndex = -1;
        }

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
            AssetManager assetManager,
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
            string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            int resolvedCurrentPathId;
            string resolvedCurrentMappedPath;
            if (TryResolveModelPathById(database, currentPathId, fieldName, listName, modelPickerService, out resolvedCurrentPathId, out resolvedCurrentMappedPath, true)
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
                if (selectedPathId <= 0)
                {
                    if (!TryCreatePathIdForSelectedModel(
                        database,
                        assetManager,
                        picker.SelectedMappedPath,
                        out selectedPathId,
                        out string createError))
                    {
                        if (!string.IsNullOrWhiteSpace(createError))
                        {
                            MessageBox.Show(createError, "Choice Model", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        return;
                    }
                }

                if (selectedPathId == currentPathId)
                {
                    return;
                }

                if (itemGrid.CurrentCell == null || itemGrid.CurrentCell.RowIndex != rowIndex)
                {
                    itemGrid.CurrentCell = itemGrid.Rows[rowIndex].Cells[2];
                }

                itemGrid.Rows[rowIndex].Cells[2].Value = selectedPathId.ToString();
                modelPickerCacheService?.InvalidatePickerEntries(listIndex);
            }
        }

        private static bool TryCreatePathIdForSelectedModel(
            CacheSave database,
            AssetManager assetManager,
            string mappedPath,
            out int createdPathId,
            out string error)
        {
            createdPathId = 0;
            error = string.Empty;

            if (database == null)
            {
                error = "Database unavailable.";
                return false;
            }

            string normalizedMappedPath = ModelPickerService.NormalizeModelPathLookupKey(mappedPath);
            if (string.IsNullOrWhiteSpace(normalizedMappedPath))
            {
                error = "Invalid model path.";
                return false;
            }

            if (database.pathById == null)
            {
                database.pathById = new System.Collections.Generic.SortedList<int, string>();
            }

            foreach (System.Collections.Generic.KeyValuePair<int, string> kv in database.pathById)
            {
                if (string.Equals(
                        ModelPickerService.NormalizeModelPathLookupKey(kv.Value),
                        normalizedMappedPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    createdPathId = kv.Key;
                    return createdPathId > 0;
                }
            }

            int newPathId = FindCompatibleNewPathId(database.pathById, assetManager);
            if (newPathId <= 0)
            {
                error = "Failed to allocate a new PathID.";
                return false;
            }

            string canonicalMappedPath = (mappedPath ?? string.Empty).Replace('/', '\\').Trim().TrimStart('\\');
            if (string.IsNullOrWhiteSpace(canonicalMappedPath))
            {
                error = "Invalid model path.";
                return false;
            }

            if (assetManager == null)
            {
                error = "Asset manager unavailable.";
                return false;
            }

            int existingPersistedPathId;
            if (assetManager.TryFindPathIdByMappedPath(canonicalMappedPath, out existingPersistedPathId) && existingPersistedPathId > 0)
            {
                createdPathId = existingPersistedPathId;
                if (!database.pathById.ContainsKey(existingPersistedPathId))
                {
                    database.pathById[existingPersistedPathId] = canonicalMappedPath;
                }
                return true;
            }

            int attempts = 0;
            while (attempts < 200000 && newPathId > 0)
            {
                attempts++;
                if (database.pathById.ContainsKey(newPathId))
                {
                    newPathId++;
                    continue;
                }

                database.pathById.Add(newPathId, canonicalMappedPath);
                createdPathId = newPathId;

                if (assetManager.QueuePathDataEntry(newPathId, canonicalMappedPath, out string queueError))
                {
                    return true;
                }

                database.pathById.Remove(newPathId);
                createdPathId = 0;

                if (!string.IsNullOrWhiteSpace(queueError)
                    && queueError.StartsWith("PathID already mapped to another path:", StringComparison.OrdinalIgnoreCase))
                {
                    newPathId++;
                    continue;
                }

                error = string.IsNullOrWhiteSpace(queueError)
                    ? "Failed to queue PathID for path.data save."
                    : "Failed to queue PathID for path.data save:\n" + queueError;
                return false;
            }

            error = "Failed to allocate a collision-free PathID.";
            return false;
        }

        private static int FindCompatibleNewPathId(System.Collections.Generic.SortedList<int, string> pathById, AssetManager assetManager)
        {
            int maxInDatabase = 0;
            if (pathById != null && pathById.Count > 0)
            {
                maxInDatabase = pathById.Keys[pathById.Count - 1];
            }

            int maxInPathData = 0;
            if (assetManager != null)
            {
                maxInPathData = assetManager.GetMaxPathId();
            }

            int candidate = Math.Max(maxInDatabase, maxInPathData) + 1;
            if (candidate <= 0)
            {
                candidate = 1;
            }

            while (pathById != null && pathById.ContainsKey(candidate))
            {
                candidate++;
            }

            return candidate;
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
            eListCollection listCollection,
            DataGridView itemGrid,
            int listIndex,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            PathIdResolutionService pathIdResolutionService,
            ModelPickerService modelPickerService,
            ModelPreviewService modelPreviewService,
            Form owner,
            Action<string> showMessage,
            bool enableLivePreview = true,
            bool suppressBusyMessage = false)
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
            if (fieldClassifier == null || !fieldClassifier.IsModelUsageFieldName(fieldName))
            {
                return;
            }
            string listName = string.Empty;
            if (listCollection != null && listIndex >= 0 && listIndex < listCollection.Lists.Length)
            {
                listName = listCollection.Lists[listIndex].listName ?? string.Empty;
            }
            if (modelPreviewService == null)
            {
                return;
            }

            int pathId = TryGetCurrentPathId(itemGrid, rowIndex, modelPickerService, pathIdResolutionService);
            if (enableLivePreview
                && modelPreviewService != null
                && modelPreviewService.IsPreviewWindowOpen()
                && liveModelPreviewEnabled
                && pathId == lastPreviewPathId
                && listIndex == lastPreviewListIndex
                && rowIndex == lastPreviewRowIndex)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref previewLoadInProgress, 1, 0) != 0)
            {
                if (!suppressBusyMessage && showMessage != null)
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
                    if (!string.IsNullOrWhiteSpace(result.Error) && showMessage != null)
                    {
                        showMessage(result.Error);
                    }
                    return;
                }

                modelPreviewService.ShowPreviewWindow(result.MeshData);
                if (enableLivePreview)
                {
                    liveModelPreviewEnabled = true;
                    lastPreviewPathId = pathId;
                    lastPreviewListIndex = listIndex;
                    lastPreviewRowIndex = rowIndex;
                }
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

            string cacheSignature = BuildPickerEntriesCacheSignature(listCollection, database, listIndex);
            if (modelPickerCacheService != null
                && modelPickerCacheService.TryGetPickerEntries(listIndex, cacheSignature, out List<ModelPickerEntry> cachedEntries))
            {
                return cachedEntries;
            }

            List<ModelPickerEntry> builtEntries = modelPickerService.BuildModelPickerEntries(
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

            modelPickerCacheService?.SetPickerEntries(listIndex, cacheSignature, builtEntries);
            return builtEntries;
        }

        private static string BuildPickerEntriesCacheSignature(
            eListCollection listCollection,
            CacheSave database,
            int listIndex)
        {
            try
            {
                int rows = 0;
                int fields = 0;
                string listName = string.Empty;
                if (listCollection != null
                    && listIndex >= 0
                    && listIndex < listCollection.Lists.Length
                    && listCollection.Lists[listIndex] != null)
                {
                    rows = listCollection.Lists[listIndex].elementValues != null
                        ? listCollection.Lists[listIndex].elementValues.Length
                        : 0;
                    fields = listCollection.Lists[listIndex].elementFields != null
                        ? listCollection.Lists[listIndex].elementFields.Length
                        : 0;
                    listName = listCollection.Lists[listIndex].listName ?? string.Empty;
                }

                int pathCount = database?.pathById?.Count ?? 0;
                int minPath = 0;
                int maxPath = 0;
                if (database?.pathById != null && database.pathById.Count > 0)
                {
                    minPath = database.pathById.Keys[0];
                    maxPath = database.pathById.Keys[database.pathById.Count - 1];
                }

                return listIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "|"
                    + rows.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "|"
                    + fields.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "|"
                    + pathCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "|"
                    + minPath.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "|"
                    + maxPath.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "|"
                    + listName;
            }
            catch
            {
                return listIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
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
            string listName,
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

            return modelPickerService.TryResolveModelPathById(database, pathId, fieldName, listName, out resolvedPathId, out mappedPath, allowNeighborOffsets);
        }
    }
}

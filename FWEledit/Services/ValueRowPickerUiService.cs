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

        public void EnableLiveModelPreview(int pathId, int listIndex, int rowIndex)
        {
            liveModelPreviewEnabled = true;
            lastPreviewPathId = pathId;
            lastPreviewListIndex = listIndex;
            lastPreviewRowIndex = rowIndex;
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
            eListCollection listCollection,
            int listIndex,
            ItemFieldClassifierService fieldClassifier,
            ItemReferenceService itemReferenceService,
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
                listCollection,
                listIndex,
                fieldClassifier,
                itemReferenceService,
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
            ItemReferenceService itemReferenceService,
            Action<int> openIconPicker,
            Action<int> openAddonTypePicker,
            Action<int> openItemQualityPicker,
            Action<int> openGenderTypePicker,
            Action<int> openPetFoodTypePicker,
            Action<int> openPetHeroPicker,
            Action<int> openImmuneTypePicker,
            Action<int> openBindFlagPicker,
            Action<int> openNpcSellMoneyTypePicker,
            Action<int> openReputationPicker,
            Action<int> openSoulToolRewardTypePicker,
            Action<int> openProcTypePicker,
            Action<int> openProfessionMaskPicker,
            Action<int> openRaceMaskPicker,
            Action<int> openModelProfessionPicker,
            Action<int> openModelRacePicker,
            Action<int> openCombinedServicesPicker,
            Action<int> openSkillPicker,
            Action<int> openModelPicker,
            Action<int> openItemReferencePicker,
            Action updateInlineIconButton,
            Action<string> showError)
        {
            if (itemGrid == null || e == null || e.RowIndex < 0 || fieldClassifier == null)
            {
                return;
            }

            try
            {
                string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, e.RowIndex);
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
                else if (fieldClassifier.IsGenderTypeFieldName(fieldName))
                {
                    if (openGenderTypePicker != null)
                    {
                        openGenderTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsPetFoodTypeFieldName(fieldName))
                {
                    if (openPetFoodTypePicker != null)
                    {
                        openPetFoodTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsPetHeroFieldName(fieldName))
                {
                    if (openPetHeroPicker != null)
                    {
                        openPetHeroPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsImmuneTypeFieldName(fieldName))
                {
                    if (openImmuneTypePicker != null)
                    {
                        openImmuneTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsBindFlagFieldName(fieldName))
                {
                    if (openBindFlagPicker != null)
                    {
                        openBindFlagPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsNpcSellMoneyTypeFieldName(
                    listCollection,
                    listIndex,
                    ResolveFieldIndex(itemGrid, e.RowIndex),
                    fieldName))
                {
                    if (openNpcSellMoneyTypePicker != null)
                    {
                        openNpcSellMoneyTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsReputationFieldName(fieldName))
                {
                    if (openReputationPicker != null)
                    {
                        openReputationPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsRewardTypeFieldName(listCollection, listIndex, fieldName))
                {
                    if (openSoulToolRewardTypePicker != null)
                    {
                        openSoulToolRewardTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsProcTypeFieldName(fieldName))
                {
                    if (openProcTypePicker != null)
                    {
                        openProcTypePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsProfessionMaskFieldName(fieldName))
                {
                    if (openProfessionMaskPicker != null)
                    {
                        openProfessionMaskPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsRaceMaskFieldName(fieldName))
                {
                    if (openRaceMaskPicker != null)
                    {
                        openRaceMaskPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsModelProfessionFieldName(fieldName))
                {
                    if (openModelProfessionPicker != null)
                    {
                        openModelProfessionPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsModelRaceFieldName(fieldName))
                {
                    if (openModelRacePicker != null)
                    {
                        openModelRacePicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsCombinedServicesFieldName(fieldName))
                {
                    if (openCombinedServicesPicker != null)
                    {
                        openCombinedServicesPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsSkillFieldName(fieldName))
                {
                    if (openSkillPicker != null)
                    {
                        openSkillPicker(e.RowIndex);
                    }
                }
                else if (fieldClassifier.IsModelFieldName(fieldName))
                {
                    if (openModelPicker != null)
                    {
                        openModelPicker(e.RowIndex);
                    }
                }
                else if (itemReferenceService != null && itemReferenceService.IsReferenceField(listCollection, listIndex, fieldName))
                {
                    if (openItemReferencePicker != null)
                    {
                        openItemReferencePicker(e.RowIndex);
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
                    showError("This field picker could not be opened right now.\n\nDetails: " + ex.Message);
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

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
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

            string preferredPackage = modelPickerService.GuessModelPackageFromMappedPath(resolvedCurrentMappedPath);
            if (string.IsNullOrWhiteSpace(preferredPackage))
            {
                preferredPackage = "models";
            }

            List<ModelPickerEntry> entries = BuildModelPickerEntriesForList(
                listCollection,
                database,
                listIndex,
                preferredPackage,
                modelPickerService,
                modelPickerCacheService,
                modelPackageNotificationService,
                dialogService,
                owner);

            using (ModelPickerWindow picker = new ModelPickerWindow(
                entries,
                currentResolvedPathId,
                preferredPackage,
                package => BuildModelPickerEntriesForList(
                    listCollection,
                    database,
                    listIndex,
                    package,
                    modelPickerService,
                    modelPickerCacheService,
                    modelPackageNotificationService,
                    dialogService,
                    owner),
                iconKey => database.images(iconKey),
                assetManager,
                database))
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

                SetValueCellRawValue(itemGrid, rowIndex, selectedPathId.ToString());
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

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
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
                int.TryParse(GetValueCellRawValue(itemGrid, rowIndex), out currentType);
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

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedType.ToString());
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

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsItemQualityFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
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

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenGenderTypePickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsGenderTypeFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(GenderTypeCatalog.Options), currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenPetFoodTypePickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsPetFoodTypeFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(PetFoodTypeCatalog.Options), currentValue))
            {
                picker.Text = "Choose pet food type...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenPetHeroPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsPetHeroFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(PetHeroCatalog.Options), currentValue))
            {
                picker.Text = "Choose pet type...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenImmuneTypePickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsImmuneTypeFieldName(fieldName))
            {
                return;
            }

            uint currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            ImmuneTypeCatalog.TryParseValue(rawValue, out currentValue);

            using (ImmuneTypePickerWindow picker = new ImmuneTypePickerWindow(currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenBindFlagPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsBindFlagFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(BindFlagCatalog.GetOptions(fieldName)), currentValue))
            {
                picker.Text = string.Equals(fieldName, "keep_bind_prop", StringComparison.OrdinalIgnoreCase)
                    ? "Choose bind preservation..."
                    : "Choose bind state...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenNpcSellMoneyTypePickerForValueRow(
            eListCollection listCollection,
            int listIndex,
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            int fieldIndex = ResolveFieldIndex(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsNpcSellMoneyTypeFieldName(listCollection, listIndex, fieldIndex, fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(NpcSellMoneyTypeCatalog.Options), currentValue))
            {
                picker.Text = "Choose shop currency...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenReputationPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsReputationFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(ReputationCatalog.Options), currentValue))
            {
                picker.Text = "Choose reputation...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenSoulToolRewardTypePickerForValueRow(
            eListCollection listCollection,
            int listIndex,
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsRewardTypeFieldName(listCollection, listIndex, fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            List<QualityOption> options = SoulToolRewardTypeCatalog.IsRewardTypeFieldName(fieldName)
                ? new List<QualityOption>(SoulToolRewardTypeCatalog.Options)
                : new List<QualityOption>(RandomGiftBagRewardTypeCatalog.Options);
            string pickerTitle = SoulToolRewardTypeCatalog.IsRewardTypeFieldName(fieldName)
                ? "Choose Anima reward type..."
                : "Choose gift bag reward type...";

            using (QualityPickerWindow picker = new QualityPickerWindow(options, currentValue))
            {
                picker.Text = pickerTitle;
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenItemReferencePickerForValueRow(
            eListCollection listCollection,
            CacheSave database,
            DataGridView itemGrid,
            int listIndex,
            int currentElementIndex,
            int rowIndex,
            ItemReferenceService itemReferenceService,
            IconResolutionService iconResolutionService,
            AssetManager assetManager,
            IWin32Window owner,
            Action<string> showMessage)
        {
            if (listCollection == null || itemGrid == null || itemReferenceService == null || listIndex < 0)
            {
                return;
            }
            if (rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            int targetListIndex;
            if (!itemReferenceService.TryGetTargetListIndex(listCollection, listIndex, currentElementIndex, fieldName, out targetListIndex))
            {
                return;
            }

            int currentId = 0;
            string currentText = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(currentText, out currentId);
            if (currentId <= 0)
            {
                string normalized = itemReferenceService.NormalizeReferenceInput(listCollection, listIndex, currentElementIndex, fieldName, currentText);
                int.TryParse(normalized, out currentId);
            }

            if (itemReferenceService.IsTaskTargetIndex(targetListIndex) && assetManager != null)
            {
                assetManager.EnsureTaskReferencesUpToDate();
            }

            List<ItemReferenceOption> options = itemReferenceService.IsTitleDefinitionTargetIndex(targetListIndex)
                ? itemReferenceService.BuildTitleDefinitionOptions()
                : itemReferenceService.IsTaskTargetIndex(targetListIndex)
                    ? itemReferenceService.BuildTaskOptions(database)
                : itemReferenceService.IsItemListTargetIndex(targetListIndex)
                    ? itemReferenceService.BuildSearchableItemOptions(listCollection, database, iconResolutionService)
                    : targetListIndex >= 0
                        ? itemReferenceService.BuildOptions(listCollection, targetListIndex, database, iconResolutionService)
                        : itemReferenceService.BuildSearchableOptions(listCollection, database, iconResolutionService);
            if (options.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("No referenced items found.");
                }
                return;
            }

            string targetName = itemReferenceService.IsTitleDefinitionTargetIndex(targetListIndex)
                ? "title"
                : itemReferenceService.IsTaskTargetIndex(targetListIndex)
                ? "task"
                : targetListIndex >= 0 && targetListIndex < listCollection.Lists.Length
                ? listCollection.Lists[targetListIndex].listName ?? "Item"
                : "item";
            using (ItemReferencePickerWindow picker = new ItemReferencePickerWindow(options, currentId, targetListIndex, database, "Choose " + targetName, assetManager))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                itemReferenceService.RememberReferenceOverride(listIndex, currentElementIndex, fieldName, picker.SelectedOption);
                SetValueCellRawValue(
                    itemGrid,
                    rowIndex,
                    picker.SelectedId.ToString(CultureInfo.InvariantCulture),
                    picker.SelectedOption);
            }
        }

        public void OpenProcTypePickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsProcTypeFieldName(fieldName))
            {
                return;
            }

            uint currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            ProcTypeCatalog.TryParseValue(rawValue, out currentValue);

            using (ProcTypePickerWindow picker = new ProcTypePickerWindow(currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenProfessionMaskPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsProfessionMaskFieldName(fieldName))
            {
                return;
            }

            uint currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            ProfessionMaskCatalog.TryParseValue(rawValue, out currentValue);

            using (ProfessionMaskPickerWindow picker = new ProfessionMaskPickerWindow(currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenCombinedServicesPickerForValueRow(
            eListCollection listCollection,
            int listIndex,
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsCombinedServicesFieldName(fieldName))
            {
                return;
            }

            IList<CombinedServiceOption> options = CombinedServicesCatalog.GetOptions(listCollection, listIndex, fieldName);
            if (options == null || options.Count == 0)
            {
                return;
            }

            uint currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            CombinedServicesCatalog.TryParseValue(rawValue, out currentValue);

            using (CombinedServicesPickerWindow picker = new CombinedServicesPickerWindow(fieldName, options, currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, CombinedServicesCatalog.ToStorageString(picker.SelectedValue));
            }
        }

        public void OpenSkillPickerForValueRow(
            CacheSave database,
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner,
            Action<string> showMessage)
        {
            if (database == null || itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsSkillFieldName(fieldName))
            {
                return;
            }

            List<SkillReferenceOption> options = SkillReferenceCatalog.BuildOptions(database);
            if (options == null || options.Count == 0)
            {
                if (showMessage != null)
                {
                    showMessage("No skill or buff entries were found in the game tables.");
                }
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue))
            {
                string normalized = SkillReferenceCatalog.NormalizeInput(database, rawValue);
                int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);
            }

            using (SkillReferencePickerWindow picker = new SkillReferencePickerWindow(options, currentValue, "Choose skill or buff..."))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenRaceMaskPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsRaceMaskFieldName(fieldName))
            {
                return;
            }

            uint currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            RaceMaskCatalog.TryParseValue(rawValue, out currentValue);

            using (RaceMaskPickerWindow picker = new RaceMaskPickerWindow(currentValue))
            {
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenModelProfessionPickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsModelProfessionFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(ModelProfessionCatalog.Options), currentValue))
            {
                picker.Text = "Choose model profession...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void OpenModelRacePickerForValueRow(
            DataGridView itemGrid,
            int rowIndex,
            ItemFieldClassifierService fieldClassifier,
            IWin32Window owner)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
            if (fieldClassifier == null || !fieldClassifier.IsModelRaceFieldName(fieldName))
            {
                return;
            }

            int currentValue = 0;
            string rawValue = GetValueCellRawValue(itemGrid, rowIndex);
            int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentValue);

            using (QualityPickerWindow picker = new QualityPickerWindow(new List<QualityOption>(ModelRaceCatalog.Options), currentValue))
            {
                picker.Text = "Choose model race...";
                if (picker.ShowDialog(owner) != DialogResult.OK)
                {
                    return;
                }

                SetValueCellRawValue(itemGrid, rowIndex, picker.SelectedValue.ToString(CultureInfo.InvariantCulture));
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

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
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

            CreaturePortraitIconService portraitIconService = new CreaturePortraitIconService();
            if (portraitIconService.IsCreaturePortraitField(listCollection, listIndex, fieldName))
            {
                List<TgaPortraitEntry> entries = portraitIconService.BuildPortraitEntries(database, 96);
                if (entries.Count == 0)
                {
                    MessageBox.Show(
                        "No TGA portrait entries were found in path.data.",
                        "TGA Portrait",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                using (TgaPortraitPickerWindow picker = new TgaPortraitPickerWindow(entries, currentPathId))
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

                    SetValueCellRawValue(itemGrid, rowIndex, selectedPathId.ToString());
                    itemGrid.Rows[rowIndex].Cells[2].Value = portraitIconService.FormatPortraitPathIdDisplay(database, selectedPathId.ToString());
                    return;
                }
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

                SetValueCellRawValue(itemGrid, rowIndex, selectedPathId.ToString());
                IconResolutionService iconResolutionService = new IconResolutionService();
                itemGrid.Rows[rowIndex].Cells[2].Value = iconResolutionService.FormatIconPathIdDisplay(database, selectedPathId.ToString());
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

            string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, rowIndex);
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
            string package,
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

            string safePackage = (package ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(safePackage))
            {
                safePackage = "models";
            }

            string listContextSignature = BuildPickerEntriesCacheSignature(listCollection, database, listIndex, modelPickerService);
            string packageContentSignature = modelPickerService.BuildPackageCacheSignature(safePackage);
            string cacheSignature = listContextSignature
                + "|pkg:"
                + safePackage
                + "|pkgsig:"
                + packageContentSignature;
            if (modelPickerCacheService != null
                && modelPickerCacheService.TryGetPickerEntries(listIndex, cacheSignature, out List<ModelPickerEntry> cachedEntries))
            {
                return cachedEntries;
            }

            List<ModelPickerEntry> builtEntries = modelPickerService.BuildModelPickerEntriesForPackage(
                listCollection,
                database,
                listIndex,
                safePackage,
                iconKey => database.images(iconKey),
                missingPackage =>
                {
                    if (modelPickerCacheService == null || !modelPickerCacheService.TryMarkMissingExtractNotified(missingPackage))
                    {
                        return;
                    }
                    if (dialogService != null && modelPackageNotificationService != null)
                    {
                        dialogService.ShowMessage(
                            modelPackageNotificationService.BuildMissingPackageMessage(missingPackage),
                            modelPackageNotificationService.Title,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information,
                            owner);
                    }
                },
                listContextSignature);

            modelPickerCacheService?.SetPickerEntries(listIndex, cacheSignature, builtEntries);
            return builtEntries;
        }

        private static string BuildPickerEntriesCacheSignature(
            eListCollection listCollection,
            CacheSave database,
            int listIndex,
            ModelPickerService modelPickerService)
        {
            try
            {
                int rows = 0;
                int fields = 0;
                string listName = string.Empty;
                ulong usageHash = 1469598103934665603UL;
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

                    if (modelPickerService != null && rows > 0)
                    {
                        int[] modelUsageFields = modelPickerService.GetModelUsageFieldIndices(listCollection, listIndex);
                        for (int row = 0; row < rows; row++)
                        {
                            for (int mf = 0; mf < modelUsageFields.Length; mf++)
                            {
                                string value = listCollection.GetValue(listIndex, row, modelUsageFields[mf]) ?? string.Empty;
                                for (int i = 0; i < value.Length; i++)
                                {
                                    usageHash ^= value[i];
                                    usageHash *= 1099511628211UL;
                                }
                                usageHash ^= ';';
                                usageHash *= 1099511628211UL;
                            }
                        }
                    }
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
                    + listName
                    + "|"
                    + usageHash.ToString("X16", System.Globalization.CultureInfo.InvariantCulture);
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

        private static string GetValueCellRawValue(DataGridView itemGrid, int rowIndex)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return string.Empty;
            }

            object raw = itemGrid.Rows[rowIndex].Cells[2].Tag;
            ValueCellState state = raw as ValueCellState;
            if (state != null)
            {
                return state.RawValue ?? string.Empty;
            }
            if (raw != null)
            {
                return Convert.ToString(raw);
            }

            return Convert.ToString(itemGrid.Rows[rowIndex].Cells[2].Value);
        }

        private static int ResolveFieldIndex(DataGridView itemGrid, int rowIndex)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return -1;
            }

            object tag = itemGrid.Rows[rowIndex].Tag;
            if (tag is int)
            {
                return (int)tag;
            }

            return -1;
        }

        private static void SetValueCellRawValue(DataGridView itemGrid, int rowIndex, string rawValue, ItemReferenceOption referenceOption = null)
        {
            if (itemGrid == null || rowIndex < 0 || rowIndex >= itemGrid.Rows.Count)
            {
                return;
            }

            string safeValue = rawValue ?? string.Empty;
            itemGrid.Rows[rowIndex].Cells[2].Tag = referenceOption != null
                ? (object)new ValueCellState
                {
                    RawValue = safeValue,
                    ReferenceOption = referenceOption
                }
                : (object)safeValue;
            itemGrid.Rows[rowIndex].Cells[2].Value = safeValue;
        }

        private static float ResolvePreviewScaleFromGrid(DataGridView itemGrid, string modelFieldName)
        {
            if (itemGrid == null || itemGrid.Rows == null || itemGrid.Rows.Count <= 0)
            {
                return 1f;
            }

            float fallback = 1f;
            int bestScore = int.MinValue;
            float bestValue = 1f;
            if (TryExtractInlineScaleFromGridModelField(itemGrid, modelFieldName, out float inlineScale))
            {
                bestScore = 500;
                bestValue = inlineScale;
            }
            for (int i = 0; i < itemGrid.Rows.Count; i++)
            {
                string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, i);
                if (!LooksLikeScaleFieldName(fieldName))
                {
                    continue;
                }

                string rawValue = GetValueCellRawValue(itemGrid, i);
                if (TryParsePreviewScale(rawValue, out float parsed))
                {
                    int score = GetScaleFieldAffinity(modelFieldName, fieldName);
                    if (IsPrimaryScaleField(fieldName))
                    {
                        score += 200;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestValue = parsed;
                    }
                    fallback = parsed;
                }
            }

            return bestScore > int.MinValue ? bestValue : fallback;
        }

        private static bool TryExtractInlineScaleFromGridModelField(DataGridView itemGrid, string modelFieldName, out float scale)
        {
            scale = 1f;
            if (itemGrid == null || itemGrid.Rows == null || itemGrid.Rows.Count <= 0 || string.IsNullOrWhiteSpace(modelFieldName))
            {
                return false;
            }

            string normalizedModel = NormalizeFieldToken(modelFieldName);
            for (int i = 0; i < itemGrid.Rows.Count; i++)
            {
                string fieldName = ValueGridFieldNameService.GetFieldName(itemGrid, i);
                if (NormalizeFieldToken(fieldName) != normalizedModel)
                {
                    continue;
                }

                string rawValue = GetValueCellRawValue(itemGrid, i);
                if (TryExtractInlineScaleFromModelValue(rawValue, out float inlineScale))
                {
                    scale = inlineScale;
                    return true;
                }
            }

            return false;
        }

        private static bool TryExtractInlineScaleFromModelValue(string modelFieldRawValue, out float scale)
        {
            scale = 1f;
            if (string.IsNullOrWhiteSpace(modelFieldRawValue))
            {
                return false;
            }

            string[] parts = modelFieldRawValue.Split(new[] { '|', ';', ',', ':', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || parts.Length < 2)
            {
                return false;
            }

            int parsedPathId = 0;
            int.TryParse(parts[0], out parsedPathId);
            for (int i = 1; i < parts.Length; i++)
            {
                string token = parts[i].Trim();
                if (string.IsNullOrWhiteSpace(token))
                {
                    continue;
                }

                if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                    && !float.TryParse(token, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
                {
                    continue;
                }

                if (parsedPathId > 0 && Math.Abs(parsed - parsedPathId) < 0.01f)
                {
                    continue;
                }

                if (parsed <= 0f || parsed < 0.1f || parsed > 5000f)
                {
                    continue;
                }

                scale = NormalizePreviewScale(parsed);
                return scale > 0.01f;
            }

            return false;
        }

        private static bool IsPrimaryScaleField(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim().ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty);

            return normalized.Contains("scalemodel")
                || normalized == "scale"
                || normalized == "modelscale";
        }

        private static bool LooksLikeScaleFieldName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return false;
            }

            string normalized = fieldName.Trim().ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty);

            if (normalized == "size")
            {
                return true;
            }

            return normalized.Contains("scalemodel")
                || normalized == "scale"
                || normalized.Contains("bodyscale")
                || normalized.Contains("modelscale")
                || normalized.Contains("weaponscale")
                || normalized.Contains("itemscale")
                || normalized.Contains("ratio")
                || normalized.Contains("factor")
                || normalized.Contains("proportion")
                || normalized.Contains("percent")
                || normalized.Contains("zoom")
                || normalized.Contains("mul")
                || normalized.Contains("size")
                || fieldName.Contains("缩放")
                || fieldName.Contains("比例")
                || fieldName.Contains("尺寸")
                || fieldName.Contains("大小")
                || fieldName.Contains("倍率");
        }

        private static int GetScaleFieldAffinity(string modelFieldName, string scaleFieldName)
        {
            if (string.IsNullOrWhiteSpace(scaleFieldName))
            {
                return int.MinValue;
            }

            int score = 0;
            string model = NormalizeFieldToken(modelFieldName);
            string scale = NormalizeFieldToken(scaleFieldName);

            if (IsFemaleMarker(model) && IsFemaleMarker(scale))
            {
                score += 120;
            }
            if (IsMaleMarker(model) && IsMaleMarker(scale))
            {
                score += 120;
            }
            if (model.Contains("weapon") && scale.Contains("weapon"))
            {
                score += 30;
            }
            if (model.Contains("model") && scale.Contains("model"))
            {
                score += 20;
            }
            if (!string.IsNullOrWhiteSpace(model)
                && !string.IsNullOrWhiteSpace(scale)
                && (scale.Contains(model) || model.Contains(scale)))
            {
                score += 40;
            }

            return score;
        }

        private static string NormalizeFieldToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
        }

        private static bool IsFemaleMarker(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.Contains("female")
                || token.Contains("woman")
                || token.Contains("girl")
                || token.Contains("feminino")
                || token.Contains("mulher")
                || token.Contains("lady")
                || token.Contains("女");
        }

        private static bool IsMaleMarker(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return token.Contains("male")
                || token.Contains("man")
                || token.Contains("boy")
                || token.Contains("masculino")
                || token.Contains("homem")
                || token.Contains("gent")
                || token.Contains("男");
        }

        private static bool TryParsePreviewScale(string raw, out float scale)
        {
            scale = 1f;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string normalized = raw.Trim();
            int pipe = normalized.IndexOf('|');
            if (pipe > 0)
            {
                normalized = normalized.Substring(0, pipe).Trim();
            }

            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
                && !float.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out parsed))
            {
                return false;
            }
            if (parsed <= 0f)
            {
                return false;
            }
            if (parsed < 0.1f)
            {
                return false;
            }

            scale = NormalizePreviewScale(parsed);
            return scale > 0.01f;
        }

        private static float NormalizePreviewScale(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 1f;
            }

            float normalized = value;
            if (normalized > 10f && normalized <= 500f)
            {
                normalized /= 100f;
            }
            else if (normalized > 500f && normalized <= 5000f)
            {
                normalized /= 1000f;
            }

            if (normalized < 0.05f)
            {
                normalized = 0.05f;
            }
            if (normalized > 8f)
            {
                normalized = 8f;
            }

            return normalized;
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


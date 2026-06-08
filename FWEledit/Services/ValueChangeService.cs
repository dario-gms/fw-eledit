using System;
using System.Collections.Generic;
using System.Drawing;

namespace FWEledit
{
    public sealed class ValueChangeService
    {
        private readonly AddonParamService addonParamService;
        private readonly ModelPickerService modelPickerService;
        private readonly IdGenerationService idGenerationService;
        private readonly IconResolutionService iconResolutionService;
        private readonly ItemReferenceService itemReferenceService;
        private readonly CreaturePortraitIconService creaturePortraitIconService = new CreaturePortraitIconService();

        public ValueChangeService(
            AddonParamService addonParamService,
            ModelPickerService modelPickerService,
            IdGenerationService idGenerationService,
            IconResolutionService iconResolutionService,
            ItemReferenceService itemReferenceService)
        {
            this.addonParamService = addonParamService;
            this.modelPickerService = modelPickerService;
            this.idGenerationService = idGenerationService;
            this.iconResolutionService = iconResolutionService;
            this.itemReferenceService = itemReferenceService;
        }

        public ValueChangeResult Apply(ValueChangeRequest request)
        {
            ValueChangeResult result = new ValueChangeResult();
            if (request == null || request.ListCollection == null)
            {
                result.ErrorMessage = "Invalid change request.";
                return result;
            }

            if (request.ListIndex == request.ListCollection.ConversationListIndex)
            {
                return ApplyConversationChange(request);
            }

            return ApplyListChange(request);
        }

        private ValueChangeResult ApplyListChange(ValueChangeRequest request)
        {
            ValueChangeResult result = new ValueChangeResult();
            string valueToSet = request.NewValue ?? string.Empty;
            if (request.ListIndex < 0 || request.FieldIndex < 0)
            {
                result.ErrorMessage = "Invalid selection.";
                return result;
            }

            if (request.ListIndex == 0 && addonParamService.IsAddonParamField(request.FieldName))
            {
                string normalized;
                if (addonParamService.TryNormalizeAddonParamValueForStorage(request.ListCollection, request.ListIndex, request.CurrentElementIndex, request.FieldName, valueToSet, out normalized))
                {
                    valueToSet = normalized;
                }
            }

            if (request.IsModelField)
            {
                int modelPathId;
                if (modelPickerService == null || !modelPickerService.TryExtractPathId(valueToSet, out modelPathId))
                {
                    request.MarkFieldInvalid?.Invoke(request.ListIndex, request.CurrentElementIndex, request.FieldIndex);
                    result.ErrorMessage = "Invalid model PathID value.";
                    result.MarkInvalid = true;
                    return result;
                }
                valueToSet = modelPathId.ToString();
            }
            else if (itemReferenceService != null && itemReferenceService.IsReferenceField(request.ListCollection, request.ListIndex, request.FieldName))
            {
                valueToSet = itemReferenceService.NormalizeReferenceInput(request.ListCollection, request.ListIndex, request.CurrentElementIndex, request.FieldName, valueToSet);
            }
            else if (GenderTypeCatalog.IsGenderTypeFieldName(request.FieldName))
            {
                valueToSet = GenderTypeCatalog.NormalizeInput(valueToSet);
            }
            else if (ProbabilityDisplayService.IsProbabilityFieldName(request.FieldName))
            {
                valueToSet = ProbabilityDisplayService.NormalizeInput(valueToSet);
            }

            if (request.IsIdEdit)
            {
                int desiredId;
                if (!int.TryParse(valueToSet, out desiredId))
                {
                    request.MarkFieldInvalid?.Invoke(request.ListIndex, request.CurrentElementIndex, request.FieldIndex);
                    result.ErrorMessage = "Invalid ID value.";
                    result.MarkInvalid = true;
                    return result;
                }

                HashSet<int> usedIds = idGenerationService.BuildUsedIds(request.ListCollection, request.ListIndex, request.FieldIndex);
                for (int i = 0; i < request.SelectedElementIndices.Length; i++)
                {
                    int curId;
                    int.TryParse(request.ListCollection.GetValue(request.ListIndex, request.SelectedElementIndices[i], request.FieldIndex), out curId);
                    usedIds.Remove(curId);
                }
                if (usedIds.Contains(desiredId))
                {
                    request.MarkFieldInvalid?.Invoke(request.ListIndex, request.CurrentElementIndex, request.FieldIndex);
                    result.ErrorMessage = "ID already exists in this list.";
                    result.MarkInvalid = true;
                    return result;
                }
            }
            else if (request.IsValueCompatible != null && !request.IsValueCompatible(request.FieldType, valueToSet))
            {
                request.MarkFieldInvalid?.Invoke(request.ListIndex, request.CurrentElementIndex, request.FieldIndex);
                result.ErrorMessage = "Invalid value for field type " + request.FieldType + ".";
                result.MarkInvalid = true;
                return result;
            }

            if (request.ListIndex == 0 &&
                (string.Equals(request.FieldName, "id", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.FieldName, "name", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.FieldName, "type", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.FieldName, "num_params", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.FieldName, "param1", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.FieldName, "param2", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(request.FieldName, "param3", StringComparison.OrdinalIgnoreCase)))
            {
                request.ResetList0DisplayCache?.Invoke();
            }

            for (int i = 0; i < request.SelectedElementIndices.Length; i++)
            {
                int oldId = 0;
                int newId = 0;
                if (request.IsIdEdit)
                {
                    int.TryParse(request.ListCollection.GetValue(request.ListIndex, request.SelectedElementIndices[i], request.FieldIndex), out oldId);
                    int.TryParse(valueToSet, out newId);
                }
                request.ListCollection.SetValue(request.ListIndex, request.SelectedElementIndices[i], request.FieldIndex, valueToSet);
                if (request.IsIdEdit && oldId > 0 && newId > 0 && oldId != newId)
                {
                    request.RemapDescriptionId?.Invoke(oldId, newId);
                }
                request.MarkRowDirty?.Invoke(request.ListIndex, request.SelectedElementIndices[i]);
                request.MarkFieldDirty?.Invoke(request.ListIndex, request.SelectedElementIndices[i], request.FieldIndex);
                request.ClearFieldInvalid?.Invoke(request.ListIndex, request.SelectedElementIndices[i], request.FieldIndex);
            }

            if (itemReferenceService != null && AffectsItemReferenceCache(request.FieldName))
            {
                itemReferenceService.ClearCache();
            }

            result.MarkDirty = true;
            result.RawValue = valueToSet;
            result.DisplayValue = valueToSet;
            if (request.IsModelField)
            {
                string listName = request.ListCollection.Lists[request.ListIndex].listName ?? string.Empty;
                result.DisplayValue = modelPickerService.FormatModelPathIdDisplay(request.Database, valueToSet, request.FieldName, listName);
            }
            else if (string.Equals(request.FieldName, "item_quality", StringComparison.OrdinalIgnoreCase))
            {
                result.DisplayValue = ItemQualityCatalog.FormatDisplay(valueToSet);
            }
            else if (GenderTypeCatalog.IsGenderTypeFieldName(request.FieldName))
            {
                result.DisplayValue = GenderTypeCatalog.FormatDisplay(valueToSet);
            }
            else if (ProbabilityDisplayService.IsProbabilityFieldName(request.FieldName))
            {
                result.DisplayValue = ProbabilityDisplayService.FormatDisplay(valueToSet);
            }
            else if (creaturePortraitIconService.IsCreaturePortraitField(request.ListCollection, request.ListIndex, request.FieldName))
            {
                result.DisplayValue = creaturePortraitIconService.FormatPortraitPathIdDisplay(request.Database, valueToSet);
            }
            else if (itemReferenceService != null && itemReferenceService.IsReferenceField(request.ListCollection, request.ListIndex, request.FieldName))
            {
                result.DisplayValue = itemReferenceService.FormatReferenceValue(request.ListCollection, request.ListIndex, request.CurrentElementIndex, request.FieldName, valueToSet);
            }
            else if (request.ListIndex == 0 && addonParamService.IsAddonParamField(request.FieldName))
            {
                result.DisplayValue = addonParamService.FormatAddonParamValueForUi(request.ListCollection, request.ListIndex, request.CurrentElementIndex, request.FieldName, valueToSet);
            }

            int namePosForStar = -1;
            for (int i = 0; i < request.ListCollection.Lists[request.ListIndex].elementFields.Length; i++)
            {
                if (string.Equals(request.ListCollection.Lists[request.ListIndex].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                {
                    namePosForStar = i;
                    break;
                }
            }
            if (namePosForStar < 0) { namePosForStar = 0; }

            bool requiresIconUpdate = string.Equals(request.FieldName, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.FieldName, "name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.FieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.FieldName, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(request.FieldName, "is_category", StringComparison.OrdinalIgnoreCase)
                || ((request.ListCollection.Lists[request.ListIndex].listName ?? string.Empty).IndexOf("DROPTABLE_ESSENCE", StringComparison.OrdinalIgnoreCase) >= 0
                    && request.FieldName.StartsWith("drops_", StringComparison.OrdinalIgnoreCase)
                    && request.FieldName.EndsWith("_id_obj", StringComparison.OrdinalIgnoreCase));

            int pos = -1;
            int pos2 = -1;
            if (requiresIconUpdate)
            {
                for (int i = 0; i < request.ListCollection.Lists[request.ListIndex].elementFields.Length; i++)
                {
                    if (string.Equals(request.ListCollection.Lists[request.ListIndex].elementFields[i], "name", StringComparison.OrdinalIgnoreCase))
                    {
                        pos = i;
                    }
                    if (string.Equals(request.ListCollection.Lists[request.ListIndex].elementFields[i], "file_icon", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(request.ListCollection.Lists[request.ListIndex].elementFields[i], "file_icon1", StringComparison.OrdinalIgnoreCase))
                    {
                        pos2 = i;
                    }
                }
                if (pos < 0) { pos = 0; }
            }

            for (int i = 0; i < request.SelectedGridIndices.Length; i++)
            {
                int elementIndex = request.SelectedElementIndices[i];
                ListRowUpdate update = new ListRowUpdate
                {
                    GridRowIndex = request.SelectedGridIndices[i],
                    ElementIndex = elementIndex,
                    DisplayName = request.ComposeListDisplayName != null
                        ? request.ComposeListDisplayName(request.ListIndex, elementIndex, namePosForStar)
                        : string.Empty
                };

                if (requiresIconUpdate)
                {
                    Image icon = Properties.Resources.blank;
                    if (pos2 > -1)
                    {
                        string rawIcon = request.ListCollection.GetValue(request.ListIndex, elementIndex, pos2);
                        Bitmap portrait;
                        if (creaturePortraitIconService.TryResolvePortrait(request.Database, request.ListCollection, request.ListIndex, rawIcon, out portrait))
                        {
                            icon = portrait;
                        }
                        else
                        {
                            string path = iconResolutionService.ResolveIconKeyForList(request.Database, request.ListCollection, request.ListIndex, rawIcon);
                            if (request.Database != null && request.Database.sourceBitmap != null && request.Database.ContainsKey(path))
                            {
                                icon = request.Database.images(path);
                            }
                        }
                    }
                    update.IdValue = request.ListCollection.GetValue(request.ListIndex, elementIndex, 0);
                    update.Icon = icon;
                }

                update.UpdateQualityColor = string.Equals(request.FieldName, "item_quality", StringComparison.OrdinalIgnoreCase);
                result.ListRowUpdates.Add(update);
            }

            result.Success = true;
            return result;
        }

        private static bool AffectsItemReferenceCache(string fieldName)
        {
            return string.Equals(fieldName, "id", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "ID", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "name", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "file_icon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "file_icon1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(fieldName, "item_quality", StringComparison.OrdinalIgnoreCase);
        }

        private ValueChangeResult ApplyConversationChange(ValueChangeRequest request)
        {
            ValueChangeResult result = new ValueChangeResult();
            if (request.ConversationList == null)
            {
                result.ErrorMessage = "Conversation parser unavailable.";
                return result;
            }

            string fieldName = request.FieldName ?? string.Empty;
            int r = request.CurrentElementIndex;
            if (fieldName == "id_talk")
            {
                request.ConversationList.talk_procs[r].id_talk = Convert.ToInt32(request.NewValue);
                result.Success = true;
                return result;
            }
            if (fieldName == "text")
            {
                request.ConversationList.talk_procs[r].SetText(request.NewValue ?? string.Empty);
                result.Success = true;
                return result;
            }
            if (fieldName.StartsWith("window_") && fieldName.EndsWith("_id"))
            {
                int q = Convert.ToInt32(fieldName.Replace("window_", "").Replace("_id", ""));
                request.ConversationList.talk_procs[r].windows[q].id = Convert.ToInt32(request.NewValue);
                result.Success = true;
                return result;
            }
            if (fieldName.StartsWith("window_") && fieldName.Contains("option_") && fieldName.EndsWith("_param"))
            {
                string[] s = fieldName.Replace("window_", "").Replace("_option_", ";").Replace("_param", "").Split(new char[] { ';' });
                int q = Convert.ToInt32(s[0]);
                int c = Convert.ToInt32(s[1]);
                request.ConversationList.talk_procs[r].windows[q].options[c].param = Convert.ToInt32(request.NewValue);
                result.Success = true;
                return result;
            }
            if (fieldName.StartsWith("window_") && fieldName.Contains("option_") && fieldName.EndsWith("_text"))
            {
                string[] s = fieldName.Replace("window_", "").Replace("_option_", ";").Replace("_text", "").Split(new char[] { ';' });
                int q = Convert.ToInt32(s[0]);
                int c = Convert.ToInt32(s[1]);
                request.ConversationList.talk_procs[r].windows[q].options[c].SetText(request.NewValue ?? string.Empty);
                result.Success = true;
                return result;
            }
            if (fieldName.StartsWith("window_") && fieldName.Contains("option_") && fieldName.EndsWith("_id"))
            {
                string[] s = fieldName.Replace("window_", "").Replace("_option_", ";").Replace("_id", "").Split(new char[] { ';' });
                int q = Convert.ToInt32(s[0]);
                int c = Convert.ToInt32(s[1]);
                request.ConversationList.talk_procs[r].windows[q].options[c].id = Convert.ToInt32(request.NewValue);
                result.Success = true;
                return result;
            }
            if (fieldName.StartsWith("window_") && fieldName.EndsWith("_id_parent"))
            {
                int q = Convert.ToInt32(fieldName.Replace("window_", "").Replace("_id_parent", ""));
                request.ConversationList.talk_procs[r].windows[q].id_parent = Convert.ToInt32(request.NewValue);
                result.Success = true;
                return result;
            }
            if (fieldName.StartsWith("window_") && fieldName.EndsWith("_talk_text"))
            {
                int q = Convert.ToInt32(fieldName.Replace("window_", "").Replace("_talk_text", ""));
                request.ConversationList.talk_procs[r].windows[q].SetText(request.NewValue ?? string.Empty);
                result.Success = true;
                return result;
            }

            result.Success = true;
            return result;
        }
    }
}

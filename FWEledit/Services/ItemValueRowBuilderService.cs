using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemValueRowBuilderService
    {
        private readonly AddonTypeDisplayService addonTypeDisplayService;
        private readonly AddonParamService addonParamService;
        private readonly ModelPickerService modelPickerService;
        private readonly IconResolutionService iconResolutionService;
        private readonly ItemReferenceService itemReferenceService;
        private readonly CreaturePortraitIconService creaturePortraitIconService;

        public ItemValueRowBuilderService(
            AddonTypeDisplayService addonTypeDisplayService,
            AddonParamService addonParamService,
            ModelPickerService modelPickerService,
            IconResolutionService iconResolutionService,
            ItemReferenceService itemReferenceService)
        {
            this.addonTypeDisplayService = addonTypeDisplayService;
            this.addonParamService = addonParamService;
            this.modelPickerService = modelPickerService;
            this.iconResolutionService = iconResolutionService;
            this.itemReferenceService = itemReferenceService;
            this.creaturePortraitIconService = new CreaturePortraitIconService();
        }

        public List<ValueRowDisplay> BuildRows(
            ISessionService sessionService,
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            int elementIndex,
            System.Func<int, int, string, bool> shouldIncludeField,
            System.Func<int, int, int, string> getDisplayEntryName,
            System.Func<System.Collections.Generic.Dictionary<int, string>> loadAddonTypeHints,
            System.Func<string, bool> isModelFieldName,
            System.Func<int, int, int, bool> isFieldInvalid,
            System.Func<int, int, int, bool> isFieldDirty)
        {
            List<ValueRowDisplay> rows = new List<ValueRowDisplay>();
            if (listCollection == null || listIndex < 0)
            {
                return rows;
            }

            if (listIndex == listCollection.ConversationListIndex)
            {
                if (conversationList == null || elementIndex < 0 || elementIndex >= conversationList.talk_proc_count)
                {
                    return rows;
                }

                rows.Add(new ValueRowDisplay
                {
                    FieldIndex = -1,
                    FieldName = "id_talk",
                    FieldType = "int32",
                    DisplayValue = conversationList.talk_procs[elementIndex].id_talk.ToString(),
                    RawValue = conversationList.talk_procs[elementIndex].id_talk.ToString()
                });
                rows.Add(new ValueRowDisplay
                {
                    FieldIndex = -1,
                    FieldName = "text",
                    FieldType = "wstring:128",
                    DisplayValue = conversationList.talk_procs[elementIndex].GetText(),
                    RawValue = conversationList.talk_procs[elementIndex].GetText()
                });

                for (int q = 0; q < conversationList.talk_procs[elementIndex].num_window; q++)
                {
                    rows.Add(new ValueRowDisplay
                    {
                        FieldIndex = -1,
                        FieldName = "window_" + q + "_id",
                        FieldType = "int32",
                        DisplayValue = conversationList.talk_procs[elementIndex].windows[q].id.ToString(),
                        RawValue = conversationList.talk_procs[elementIndex].windows[q].id.ToString()
                    });
                    rows.Add(new ValueRowDisplay
                    {
                        FieldIndex = -1,
                        FieldName = "window_" + q + "_id_parent",
                        FieldType = "int32",
                        DisplayValue = conversationList.talk_procs[elementIndex].windows[q].id_parent.ToString(),
                        RawValue = conversationList.talk_procs[elementIndex].windows[q].id_parent.ToString()
                    });
                    rows.Add(new ValueRowDisplay
                    {
                        FieldIndex = -1,
                        FieldName = "window_" + q + "_talk_text",
                        FieldType = "wstring:" + conversationList.talk_procs[elementIndex].windows[q].talk_text_len,
                        DisplayValue = conversationList.talk_procs[elementIndex].windows[q].GetText(),
                        RawValue = conversationList.talk_procs[elementIndex].windows[q].GetText()
                    });
                    for (int c = 0; c < conversationList.talk_procs[elementIndex].windows[q].num_option; c++)
                    {
                        rows.Add(new ValueRowDisplay
                        {
                            FieldIndex = -1,
                            FieldName = "window_" + q + "_option_" + c + "_param",
                            FieldType = "int32",
                            DisplayValue = conversationList.talk_procs[elementIndex].windows[q].options[c].param.ToString(),
                            RawValue = conversationList.talk_procs[elementIndex].windows[q].options[c].param.ToString()
                        });
                        rows.Add(new ValueRowDisplay
                        {
                            FieldIndex = -1,
                            FieldName = "window_" + q + "_option_" + c + "_text",
                            FieldType = "wstring:128",
                            DisplayValue = conversationList.talk_procs[elementIndex].windows[q].options[c].GetText(),
                            RawValue = conversationList.talk_procs[elementIndex].windows[q].options[c].GetText()
                        });
                        rows.Add(new ValueRowDisplay
                        {
                            FieldIndex = -1,
                            FieldName = "window_" + q + "_option_" + c + "_id",
                            FieldType = "int32",
                            DisplayValue = conversationList.talk_procs[elementIndex].windows[q].options[c].id.ToString(),
                            RawValue = conversationList.talk_procs[elementIndex].windows[q].options[c].id.ToString()
                        });
                    }
                }

                return rows;
            }

            if (elementIndex < 0 || elementIndex >= listCollection.Lists[listIndex].elementValues.Length)
            {
                return rows;
            }

            for (int f = 0; f < listCollection.Lists[listIndex].elementValues[elementIndex].Length; f++)
            {
                string fieldName = listCollection.Lists[listIndex].elementFields[f];
                if (shouldIncludeField != null && !shouldIncludeField(listIndex, f, fieldName))
                {
                    continue;
                }

                string rawValue = listCollection.GetValue(listIndex, elementIndex, f);
                string fieldValue = rawValue;
                ItemReferenceOption resolvedReferenceOption = null;
                if (listIndex == 0)
                {
                    if (string.Equals(fieldName, "name", System.StringComparison.OrdinalIgnoreCase))
                    {
                        fieldValue = getDisplayEntryName(listIndex, elementIndex, f);
                    }
                    else if (string.Equals(fieldName, "type", System.StringComparison.OrdinalIgnoreCase))
                    {
                        fieldValue = addonTypeDisplayService.GetAddonTypeDisplayForUi(sessionService, listCollection, listIndex, elementIndex, fieldValue, loadAddonTypeHints);
                    }
                    else if (addonParamService.IsAddonParamField(fieldName))
                    {
                        fieldValue = addonParamService.FormatAddonParamValueForUi(listCollection, listIndex, elementIndex, fieldName, fieldValue);
                    }
                }

                if (creaturePortraitIconService.IsCreaturePortraitField(listCollection, listIndex, fieldName))
                {
                    fieldValue = creaturePortraitIconService.FormatPortraitPathIdDisplay(database, fieldValue);
                }
                else if (iconResolutionService != null
                    && (string.Equals(fieldName, "file_icon", System.StringComparison.OrdinalIgnoreCase)
                        || string.Equals(fieldName, "file_icon1", System.StringComparison.OrdinalIgnoreCase)))
                {
                    fieldValue = iconResolutionService.FormatIconPathIdDisplay(database, fieldValue);
                }
                else if (string.Equals(fieldName, "item_quality", System.StringComparison.OrdinalIgnoreCase))
                {
                    fieldValue = ItemQualityCatalog.FormatDisplay(fieldValue);
                }
                else if (GenderTypeCatalog.IsGenderTypeFieldName(fieldName))
                {
                    fieldValue = GenderTypeCatalog.FormatDisplay(fieldValue);
                }
                else if (PetFoodTypeCatalog.IsPetFoodTypeFieldName(fieldName))
                {
                    fieldValue = PetFoodTypeCatalog.FormatDisplay(fieldValue);
                }
                else if (PetHeroCatalog.IsPetHeroFieldName(fieldName))
                {
                    fieldValue = PetHeroCatalog.FormatDisplay(fieldValue);
                }
                else if (ImmuneTypeCatalog.IsImmuneTypeFieldName(fieldName))
                {
                    fieldValue = ImmuneTypeCatalog.FormatDisplay(fieldValue);
                }
                else if (BindFlagCatalog.IsBindFlagFieldName(fieldName))
                {
                    fieldValue = BindFlagCatalog.FormatDisplay(fieldName, fieldValue);
                }
                else if (NpcSellMoneyTypeCatalog.IsMoneyTypeField(listCollection, listIndex, f, fieldName))
                {
                    fieldValue = NpcSellMoneyTypeCatalog.FormatDisplay(fieldValue);
                }
                else if (ReputationCatalog.IsReputationIdFieldName(fieldName))
                {
                    fieldValue = ReputationCatalog.FormatDisplay(fieldValue);
                }
                else if (SoulToolRewardTypeCatalog.IsRewardTypeFieldName(fieldName))
                {
                    fieldValue = SoulToolRewardTypeCatalog.FormatDisplay(fieldValue);
                }
                else if (ProcTypeCatalog.IsProcTypeFieldName(fieldName))
                {
                    fieldValue = ProcTypeCatalog.FormatDisplay(fieldValue);
                }
                else if (ProfessionMaskCatalog.IsProfessionMaskFieldName(fieldName))
                {
                    fieldValue = ProfessionMaskCatalog.FormatDisplay(fieldValue);
                }
                else if (RaceMaskCatalog.IsRaceMaskFieldName(fieldName))
                {
                    fieldValue = RaceMaskCatalog.FormatDisplay(fieldValue);
                }
                else if (ModelProfessionCatalog.IsModelProfessionFieldName(fieldName))
                {
                    fieldValue = ModelProfessionCatalog.FormatDisplay(fieldValue);
                }
                else if (ModelRaceCatalog.IsModelRaceFieldName(fieldName))
                {
                    fieldValue = ModelRaceCatalog.FormatDisplay(fieldValue);
                }
                else if (CombinedServicesCatalog.IsCombinedServicesFieldName(fieldName))
                {
                    fieldValue = CombinedServicesCatalog.FormatDisplay(listCollection, listIndex, fieldName, fieldValue);
                }
                else if (SkillReferenceCatalog.IsSkillFieldName(fieldName))
                {
                    fieldValue = SkillReferenceCatalog.FormatDisplay(listCollection, listIndex, elementIndex, fieldName, database, fieldValue);
                }
                else if (ProbabilityDisplayService.IsProbabilityFieldName(fieldName))
                {
                    fieldValue = ProbabilityDisplayService.FormatDisplay(
                        fieldName,
                        listCollection.Lists[listIndex].elementTypes[f],
                        fieldValue);
                }
                else if (modelPickerService != null && isModelFieldName != null && isModelFieldName(fieldName))
                {
                    string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
                    fieldValue = modelPickerService.FormatModelPathIdDisplay(database, fieldValue, fieldName, listName);
                }
                else if (itemReferenceService != null && itemReferenceService.IsReferenceField(listCollection, listIndex, elementIndex, fieldName))
                {
                    if (itemReferenceService.TryResolveReferenceOption(
                        listCollection,
                        listIndex,
                        elementIndex,
                        fieldName,
                        rawValue,
                        database,
                        iconResolutionService,
                        out resolvedReferenceOption))
                    {
                        fieldValue = string.IsNullOrWhiteSpace(resolvedReferenceOption.Name)
                            ? rawValue
                            : resolvedReferenceOption.Name;
                    }
                    else
                    {
                        fieldValue = itemReferenceService.FormatReferenceValue(
                            listCollection,
                            listIndex,
                            elementIndex,
                            fieldName,
                            fieldValue,
                            database,
                            iconResolutionService);
                    }
                }

                ValueRowDisplay row = new ValueRowDisplay
                {
                    FieldIndex = f,
                    FieldName = fieldName,
                    DisplayFieldName = ModelFieldLabelCatalog.GetDisplayFieldName(
                        listCollection,
                        listCollection.Lists[listIndex].listName,
                        listIndex,
                        f,
                        fieldName),
                    FieldType = listCollection.Lists[listIndex].elementTypes[f],
                    DisplayValue = fieldValue,
                    RawValue = rawValue,
                    ResolvedReferenceOption = resolvedReferenceOption,
                    IsInvalid = isFieldInvalid != null && isFieldInvalid(listIndex, elementIndex, f),
                    IsDirty = isFieldDirty != null && isFieldDirty(listIndex, elementIndex, f)
                };

                rows.Add(row);
            }

            return rows;
        }
    }
}

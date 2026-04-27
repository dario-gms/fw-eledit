using System.Collections.Generic;

namespace FWEledit
{
    public sealed class ItemValueRowBuilderService
    {
        private readonly AddonTypeDisplayService addonTypeDisplayService;
        private readonly AddonParamService addonParamService;
        private readonly ModelPickerService modelPickerService;

        public ItemValueRowBuilderService(
            AddonTypeDisplayService addonTypeDisplayService,
            AddonParamService addonParamService,
            ModelPickerService modelPickerService)
        {
            this.addonTypeDisplayService = addonTypeDisplayService;
            this.addonParamService = addonParamService;
            this.modelPickerService = modelPickerService;
        }

        public List<ValueRowDisplay> BuildRows(
            ISessionService sessionService,
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            int elementIndex,
            System.Func<int, string, bool> shouldIncludeField,
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
                    DisplayValue = conversationList.talk_procs[elementIndex].id_talk.ToString()
                });
                rows.Add(new ValueRowDisplay
                {
                    FieldIndex = -1,
                    FieldName = "text",
                    FieldType = "wstring:128",
                    DisplayValue = conversationList.talk_procs[elementIndex].GetText()
                });

                for (int q = 0; q < conversationList.talk_procs[elementIndex].num_window; q++)
                {
                    rows.Add(new ValueRowDisplay
                    {
                        FieldIndex = -1,
                        FieldName = "window_" + q + "_id",
                        FieldType = "int32",
                        DisplayValue = conversationList.talk_procs[elementIndex].windows[q].id.ToString()
                    });
                    rows.Add(new ValueRowDisplay
                    {
                        FieldIndex = -1,
                        FieldName = "window_" + q + "_id_parent",
                        FieldType = "int32",
                        DisplayValue = conversationList.talk_procs[elementIndex].windows[q].id_parent.ToString()
                    });
                    rows.Add(new ValueRowDisplay
                    {
                        FieldIndex = -1,
                        FieldName = "window_" + q + "_talk_text",
                        FieldType = "wstring:" + conversationList.talk_procs[elementIndex].windows[q].talk_text_len,
                        DisplayValue = conversationList.talk_procs[elementIndex].windows[q].GetText()
                    });
                    for (int c = 0; c < conversationList.talk_procs[elementIndex].windows[q].num_option; c++)
                    {
                        rows.Add(new ValueRowDisplay
                        {
                            FieldIndex = -1,
                            FieldName = "window_" + q + "_option_" + c + "_param",
                            FieldType = "int32",
                            DisplayValue = conversationList.talk_procs[elementIndex].windows[q].options[c].param.ToString()
                        });
                        rows.Add(new ValueRowDisplay
                        {
                            FieldIndex = -1,
                            FieldName = "window_" + q + "_option_" + c + "_text",
                            FieldType = "wstring:128",
                            DisplayValue = conversationList.talk_procs[elementIndex].windows[q].options[c].GetText()
                        });
                        rows.Add(new ValueRowDisplay
                        {
                            FieldIndex = -1,
                            FieldName = "window_" + q + "_option_" + c + "_id",
                            FieldType = "int32",
                            DisplayValue = conversationList.talk_procs[elementIndex].windows[q].options[c].id.ToString()
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
                if (shouldIncludeField != null && !shouldIncludeField(listIndex, fieldName))
                {
                    continue;
                }

                string fieldValue = listCollection.GetValue(listIndex, elementIndex, f);
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

                if (modelPickerService != null && isModelFieldName != null && isModelFieldName(fieldName))
                {
                    string listName = listCollection.Lists[listIndex].listName ?? string.Empty;
                    fieldValue = modelPickerService.FormatModelPathIdDisplay(database, fieldValue, fieldName, listName);
                }

                ValueRowDisplay row = new ValueRowDisplay
                {
                    FieldIndex = f,
                    FieldName = fieldName,
                    FieldType = listCollection.Lists[listIndex].elementTypes[f],
                    DisplayValue = fieldValue,
                    IsInvalid = isFieldInvalid != null && isFieldInvalid(listIndex, elementIndex, f),
                    IsDirty = isFieldDirty != null && isFieldDirty(listIndex, elementIndex, f)
                };

                rows.Add(row);
            }

            return rows;
        }
    }
}

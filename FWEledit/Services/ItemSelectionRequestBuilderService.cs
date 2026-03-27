using System;

namespace FWEledit
{
    public sealed class ItemSelectionRequestBuilderService
    {
        public ItemSelectionRequest Build(
            ISessionService sessionService,
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            ItemSelectionContext context,
            Func<int, string, bool> shouldIncludeField,
            Func<int, int, int, string> getDisplayEntryName,
            Func<System.Collections.Generic.Dictionary<int, string>> loadAddonTypeHints,
            Func<string, bool> isModelFieldName,
            Func<int, int, int, bool> isFieldInvalid,
            Func<int, int, int, bool> isFieldDirty)
        {
            if (context == null)
            {
                return null;
            }

            return new ItemSelectionRequest
            {
                Session = sessionService,
                ListCollection = listCollection,
                ConversationList = conversationList,
                Database = database,
                ListIndex = context.ListIndex,
                ElementIndex = context.ElementIndex,
                ShouldIncludeField = shouldIncludeField,
                GetDisplayEntryName = getDisplayEntryName,
                LoadAddonTypeHints = loadAddonTypeHints,
                IsModelFieldName = isModelFieldName,
                IsFieldInvalid = isFieldInvalid,
                IsFieldDirty = isFieldDirty
            };
        }
    }
}

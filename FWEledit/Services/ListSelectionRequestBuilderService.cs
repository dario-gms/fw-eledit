namespace FWEledit
{
    public sealed class ListSelectionRequestBuilderService
    {
        public ListSelectionRequest Build(
            eListCollection listCollection,
            eListConversation conversationList,
            CacheSave database,
            int listIndex,
            string[][] xrefs,
            ListDisplayService listDisplayService,
            ListRowBuilderService listRowBuilderService,
            System.Func<int, int, int, string> composeListDisplayName,
            bool useLightweightRows)
        {
            return new ListSelectionRequest
            {
                ListCollection = listCollection,
                ConversationList = conversationList,
                Database = database,
                ListIndex = listIndex,
                Xrefs = xrefs,
                ListDisplayService = listDisplayService,
                ListRowBuilderService = listRowBuilderService,
                ComposeListDisplayName = composeListDisplayName,
                UseLightweightRows = useLightweightRows
            };
        }
    }
}

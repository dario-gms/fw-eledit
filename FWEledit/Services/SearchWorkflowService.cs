namespace FWEledit
{
    public sealed class SearchWorkflowService
    {
        public ElementSearchResult FindNext(
            ElementSearchService searchService,
            eListCollection listCollection,
            string query,
            bool matchCase,
            bool exactMatch,
            bool searchAllFields,
            int startListIndex,
            int startElementIndex,
            int startFieldIndex)
        {
            if (searchService == null || listCollection == null || listCollection.Lists == null)
            {
                return new ElementSearchResult();
            }

            ElementSearchRequest request = new ElementSearchRequest
            {
                ListCollection = listCollection,
                Query = query,
                MatchCase = matchCase,
                ExactMatch = exactMatch,
                SearchAllFields = searchAllFields,
                StartListIndex = startListIndex,
                StartElementIndex = startElementIndex,
                StartFieldIndex = startFieldIndex
            };

            return searchService.FindNext(request) ?? new ElementSearchResult();
        }
    }
}

using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class SearchSuggestionService
    {
        public List<SearchSuggestion> BuildSuggestions(
            eListCollection listCollection,
            string query,
            bool matchCase,
            int maxResults,
            Func<int, int> getIdFieldIndex,
            Func<int, int> getNameFieldIndex)
        {
            List<SearchSuggestion> suggestions = new List<SearchSuggestion>();
            if (listCollection == null || listCollection.Lists == null || listCollection.Lists.Length == 0)
            {
                return suggestions;
            }

            string trimmed = (query ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return suggestions;
            }

            string queryCompare = matchCase ? trimmed : trimmed.ToLowerInvariant();
            bool queryIsNumeric = int.TryParse(trimmed, out _);

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                int idFieldIndex = getIdFieldIndex != null ? getIdFieldIndex(listIndex) : -1;
                if (idFieldIndex < 0)
                {
                    idFieldIndex = 0;
                }
                int nameFieldIndex = getNameFieldIndex != null ? getNameFieldIndex(listIndex) : -1;

                for (int i = 0; i < listCollection.Lists[listIndex].elementValues.Length; i++)
                {
                    string idText = listCollection.GetValue(listIndex, i, idFieldIndex) ?? string.Empty;
                    string nameText = nameFieldIndex >= 0 ? (listCollection.GetValue(listIndex, i, nameFieldIndex) ?? string.Empty) : string.Empty;

                    string idCompare = matchCase ? idText : idText.ToLowerInvariant();
                    string nameCompare = matchCase ? nameText : nameText.ToLowerInvariant();

                    bool match = false;
                    if (queryIsNumeric)
                    {
                        if (!string.IsNullOrEmpty(idCompare) && idCompare.StartsWith(queryCompare))
                        {
                            match = true;
                        }
                        else if (!string.IsNullOrEmpty(nameCompare) && nameCompare.Contains(queryCompare))
                        {
                            match = true;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(nameCompare) && nameCompare.Contains(queryCompare))
                        {
                            match = true;
                        }
                        else if (!string.IsNullOrEmpty(idCompare) && (idCompare == queryCompare || idCompare.StartsWith(queryCompare)))
                        {
                            match = true;
                        }
                    }

                    if (match)
                    {
                        suggestions.Add(new SearchSuggestion
                        {
                            ListIndex = listIndex,
                            ElementIndex = i,
                            IdText = idText,
                            NameText = nameText
                        });

                        if (suggestions.Count >= maxResults)
                        {
                            return suggestions;
                        }
                    }
                }
            }

            return suggestions;
        }
    }
}

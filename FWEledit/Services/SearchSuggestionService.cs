using System;
using System.Collections.Generic;

namespace FWEledit
{
    public sealed class SearchSuggestionService
    {
        private const int CacheLimit = 96;
        private eListCollection cachedListCollection;
        private readonly Dictionary<string, List<SearchSuggestion>> cache = new Dictionary<string, List<SearchSuggestion>>(StringComparer.Ordinal);
        private readonly LinkedList<string> cacheOrder = new LinkedList<string>();

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

            EnsureCacheContext(listCollection);

            string queryCompare = matchCase ? trimmed : trimmed.ToLowerInvariant();
            bool queryIsNumeric = int.TryParse(trimmed, out _);
            string cacheKey = queryCompare + "|" + matchCase.ToString() + "|" + maxResults.ToString();
            List<SearchSuggestion> cached;
            if (cache.TryGetValue(cacheKey, out cached))
            {
                Touch(cacheKey);
                return CloneSuggestions(cached);
            }

            for (int listIndex = 0; listIndex < listCollection.Lists.Length; listIndex++)
            {
                if (eListCollection.IsRawTailList(listCollection.Lists[listIndex]))
                {
                    continue;
                }

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
                            Store(cacheKey, suggestions);
                            return suggestions;
                        }
                    }
                }
            }

            Store(cacheKey, suggestions);
            return suggestions;
        }

        public void ClearCache()
        {
            cachedListCollection = null;
            cache.Clear();
            cacheOrder.Clear();
        }

        private void EnsureCacheContext(eListCollection listCollection)
        {
            if (!object.ReferenceEquals(cachedListCollection, listCollection))
            {
                ClearCache();
                cachedListCollection = listCollection;
            }
        }

        private void Store(string key, List<SearchSuggestion> suggestions)
        {
            cache[key] = CloneSuggestions(suggestions);
            Touch(key);
            while (cache.Count > CacheLimit && cacheOrder.First != null)
            {
                string oldest = cacheOrder.First.Value;
                cacheOrder.RemoveFirst();
                cache.Remove(oldest);
            }
        }

        private void Touch(string key)
        {
            LinkedListNode<string> existing = cacheOrder.Find(key);
            if (existing != null)
            {
                cacheOrder.Remove(existing);
            }
            cacheOrder.AddLast(key);
        }

        private static List<SearchSuggestion> CloneSuggestions(List<SearchSuggestion> source)
        {
            List<SearchSuggestion> clone = new List<SearchSuggestion>();
            if (source == null)
            {
                return clone;
            }

            for (int i = 0; i < source.Count; i++)
            {
                SearchSuggestion suggestion = source[i];
                if (suggestion == null)
                {
                    continue;
                }

                clone.Add(new SearchSuggestion
                {
                    ListIndex = suggestion.ListIndex,
                    ElementIndex = suggestion.ElementIndex,
                    IdText = suggestion.IdText,
                    NameText = suggestion.NameText
                });
            }

            return clone;
        }
    }
}

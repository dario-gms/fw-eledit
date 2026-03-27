using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SearchSuggestionUiService
    {
        public void UpdateSuggestions(
            ListBox listBox,
            string rawQuery,
            bool matchCase,
            int maxHeight,
            Func<string, bool> isPlaceholder,
            Func<string, bool, List<SearchSuggestion>> buildSuggestions)
        {
            if (listBox == null)
            {
                return;
            }

            string query = (rawQuery ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(query) || (isPlaceholder != null && isPlaceholder(query)))
            {
                HideSuggestions(listBox);
                return;
            }

            List<SearchSuggestion> suggestions = buildSuggestions != null
                ? buildSuggestions(query, matchCase)
                : new List<SearchSuggestion>();

            listBox.BeginUpdate();
            listBox.Items.Clear();
            for (int i = 0; i < suggestions.Count; i++)
            {
                listBox.Items.Add(suggestions[i]);
            }
            listBox.EndUpdate();

            if (listBox.Items.Count == 0)
            {
                HideSuggestions(listBox);
                return;
            }

            int desiredHeight = Math.Min(maxHeight, (listBox.ItemHeight + 2) * listBox.Items.Count + 4);
            listBox.Height = Math.Max(60, desiredHeight);
            listBox.Visible = true;
            listBox.BringToFront();
        }

        public void ApplySuggestion(SearchSuggestion suggestion, Action<int, int> navigateToListItem, ListBox listBox)
        {
            if (suggestion == null)
            {
                return;
            }

            if (navigateToListItem != null)
            {
                navigateToListItem(suggestion.ListIndex, suggestion.ElementIndex);
            }
            HideSuggestions(listBox);
        }

        public void HideSuggestions(ListBox listBox)
        {
            if (listBox == null)
            {
                return;
            }
            listBox.Visible = false;
            listBox.Items.Clear();
        }
    }
}

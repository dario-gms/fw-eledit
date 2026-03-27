using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowSearchUiService
    {
        public void HandleSearchEnter(
            SearchBoxUiService searchBoxUiService,
            TextBox searchBox,
            Func<string, bool> isPlaceholder)
        {
            if (searchBoxUiService == null)
            {
                return;
            }

            searchBoxUiService.HandleSearchEnter(searchBox, isPlaceholder);
        }

        public void HandleSearchLeave(
            SearchBoxUiService searchBoxUiService,
            TextBox searchBox,
            bool searchAll,
            PlaceholderTextService placeholderTextService,
            ListBox suggestionList,
            Control invoker,
            Action hideSuggestions)
        {
            if (searchBoxUiService == null)
            {
                return;
            }

            searchBoxUiService.HandleSearchLeave(
                searchBox,
                searchAll,
                placeholderTextService,
                suggestionList,
                invoker,
                hideSuggestions);
        }

        public void HandleSearchKeyDown(
            SearchBoxUiService searchBoxUiService,
            KeyEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion,
            Action performSearch,
            Action hideSuggestions)
        {
            if (searchBoxUiService == null)
            {
                return;
            }

            searchBoxUiService.HandleSearchKeyDown(
                e,
                suggestionList,
                applySuggestion,
                performSearch,
                hideSuggestions);
        }

        public void HandleSuggestionMouseClick(
            SearchBoxUiService searchBoxUiService,
            MouseEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion)
        {
            if (searchBoxUiService == null)
            {
                return;
            }

            searchBoxUiService.HandleSuggestionMouseClick(
                e,
                suggestionList,
                applySuggestion);
        }

        public void HandleSuggestionKeyDown(
            SearchBoxUiService searchBoxUiService,
            KeyEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion,
            Action focusSearch,
            Action hideSuggestions)
        {
            if (searchBoxUiService == null)
            {
                return;
            }

            searchBoxUiService.HandleSuggestionKeyDown(
                e,
                suggestionList,
                applySuggestion,
                focusSearch,
                hideSuggestions);
        }

        public void ApplySearchSuggestion(
            SearchSuggestionUiService suggestionUiService,
            SearchSuggestion suggestion,
            Action<int, int> navigateToListItem,
            ListBox suggestionList)
        {
            if (suggestionUiService == null)
            {
                return;
            }

            suggestionUiService.ApplySuggestion(suggestion, navigateToListItem, suggestionList);
        }

        public void UpdateSearchSuggestions(
            SearchSuggestionUiService suggestionUiService,
            SearchSuggestionService suggestionService,
            ListBox suggestionList,
            string rawQuery,
            bool matchCase,
            int maxHeight,
            Func<string, bool> isPlaceholder,
            eListCollection listCollection,
            int suggestionMax,
            Func<int, int> getIdFieldIndex,
            Func<int, int> getNameFieldIndex)
        {
            if (suggestionUiService == null)
            {
                return;
            }

            suggestionUiService.UpdateSuggestions(
                suggestionList,
                rawQuery,
                matchCase,
                maxHeight,
                isPlaceholder,
                (query, isMatchCase) => BuildSuggestions(
                    suggestionService,
                    listCollection,
                    query,
                    isMatchCase,
                    suggestionMax,
                    getIdFieldIndex,
                    getNameFieldIndex));
        }

        public void HideSuggestions(
            SearchSuggestionUiService suggestionUiService,
            ListBox suggestionList)
        {
            if (suggestionUiService == null)
            {
                return;
            }

            suggestionUiService.HideSuggestions(suggestionList);
        }

        public void NavigateToListItem(
            SearchUiService searchUiService,
            eListCollection listCollection,
            int listIndex,
            int rowIndex,
            ComboBox listCombo,
            DataGridView elementGrid,
            Action<bool> setEnableSelectionItem)
        {
            if (searchUiService == null)
            {
                return;
            }

            searchUiService.NavigateToListItem(
                listCollection,
                listIndex,
                rowIndex,
                listCombo,
                elementGrid,
                setEnableSelectionItem);
        }

        private List<SearchSuggestion> BuildSuggestions(
            SearchSuggestionService suggestionService,
            eListCollection listCollection,
            string query,
            bool matchCase,
            int suggestionMax,
            Func<int, int> getIdFieldIndex,
            Func<int, int> getNameFieldIndex)
        {
            if (suggestionService == null)
            {
                return new List<SearchSuggestion>();
            }

            return suggestionService.BuildSuggestions(
                listCollection,
                query,
                matchCase,
                suggestionMax,
                getIdFieldIndex,
                getNameFieldIndex);
        }
    }
}

using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class MainWindowSearchCoordinatorService
    {
        public void HandleSearchClick(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchSuggestionUiService suggestionUiService,
            ListBox suggestionList,
            SearchUiService searchUiService,
            SearchWorkflowService searchWorkflowService,
            ElementSearchService elementSearchService,
            ISessionService sessionService,
            string searchText,
            Func<string, bool> isPlaceholder,
            bool matchCase,
            bool exactMatch,
            bool searchAll,
            int listIndex,
            int elementRowIndex,
            int valueRowIndex,
            ComboBox listCombo,
            DataGridView elementsGrid,
            DataGridView valuesGrid,
            Action<int, string> ensureEquipmentTab,
            Func<int, int> findValueRowForField,
            Action<bool> setEnableSelectionItem,
            Action<string> showMessage)
        {
            if (mainWindowSearchUiService == null || searchUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowSearchUiService.HideSuggestions(suggestionUiService, suggestionList);
            searchUiService.TrySearchAndSelect(
                searchWorkflowService,
                elementSearchService,
                sessionService.ListCollection,
                searchText,
                isPlaceholder,
                matchCase,
                exactMatch,
                searchAll,
                listIndex,
                elementRowIndex,
                valueRowIndex,
                listCombo,
                elementsGrid,
                valuesGrid,
                ensureEquipmentTab,
                findValueRowForField,
                setEnableSelectionItem,
                showMessage);
        }

        public void HandleSearchConditionChanged(
            SearchBoxUiService searchBoxUiService,
            TextBox searchBox,
            bool searchAll,
            SearchPlaceholderService placeholderService,
            Action hideSuggestions)
        {
            if (searchBoxUiService == null)
            {
                return;
            }

            searchBoxUiService.UpdateSearchPlaceholder(
                searchBox,
                searchAll,
                placeholderService,
                hideSuggestions);
        }

        public void HandleSearchEnter(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchBoxUiService searchBoxUiService,
            TextBox searchBox,
            Func<string, bool> isPlaceholder)
        {
            if (mainWindowSearchUiService == null)
            {
                return;
            }

            mainWindowSearchUiService.HandleSearchEnter(
                searchBoxUiService,
                searchBox,
                isPlaceholder);
        }

        public void HandleSearchLeave(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchBoxUiService searchBoxUiService,
            TextBox searchBox,
            bool searchAll,
            PlaceholderTextService placeholderTextService,
            ListBox suggestionList,
            Control invoker,
            Action hideSuggestions)
        {
            if (mainWindowSearchUiService == null)
            {
                return;
            }

            mainWindowSearchUiService.HandleSearchLeave(
                searchBoxUiService,
                searchBox,
                searchAll,
                placeholderTextService,
                suggestionList,
                invoker,
                hideSuggestions);
        }

        public void HandleSearchTextChanged(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchSuggestionUiService suggestionUiService,
            SearchSuggestionService suggestionService,
            ListBox suggestionList,
            string searchText,
            bool matchCase,
            int maxHeight,
            Func<string, bool> isPlaceholder,
            ISessionService sessionService,
            int suggestionMax,
            Func<int, int> getIdFieldIndex,
            Func<int, int> getNameFieldIndex)
        {
            if (mainWindowSearchUiService == null || sessionService == null)
            {
                return;
            }

            mainWindowSearchUiService.UpdateSearchSuggestions(
                suggestionUiService,
                suggestionService,
                suggestionList,
                searchText,
                matchCase,
                maxHeight,
                isPlaceholder,
                sessionService.ListCollection,
                suggestionMax,
                getIdFieldIndex,
                getNameFieldIndex);
        }

        public void HandleSearchKeyDown(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchBoxUiService searchBoxUiService,
            KeyEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion,
            Action triggerSearch,
            Action hideSuggestions)
        {
            if (mainWindowSearchUiService == null)
            {
                return;
            }

            mainWindowSearchUiService.HandleSearchKeyDown(
                searchBoxUiService,
                e,
                suggestionList,
                applySuggestion,
                triggerSearch,
                hideSuggestions);
        }

        public void HandleSuggestionMouseClick(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchBoxUiService searchBoxUiService,
            MouseEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion)
        {
            if (mainWindowSearchUiService == null)
            {
                return;
            }

            mainWindowSearchUiService.HandleSuggestionMouseClick(
                searchBoxUiService,
                e,
                suggestionList,
                applySuggestion);
        }

        public void HandleSuggestionKeyDown(
            MainWindowSearchUiService mainWindowSearchUiService,
            SearchBoxUiService searchBoxUiService,
            KeyEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion,
            Action focusSearch,
            Action hideSuggestions)
        {
            if (mainWindowSearchUiService == null)
            {
                return;
            }

            mainWindowSearchUiService.HandleSuggestionKeyDown(
                searchBoxUiService,
                e,
                suggestionList,
                applySuggestion,
                focusSearch,
                hideSuggestions);
        }
    }
}

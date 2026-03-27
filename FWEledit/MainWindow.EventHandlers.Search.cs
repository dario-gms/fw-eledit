using System;
using System.Windows.Forms;

namespace FWEledit
{
    public partial class MainWindow : Form
    {
        private void click_search(object sender, EventArgs ea)
        {
            mainWindowSearchCoordinatorService.HandleSearchClick(
                mainWindowSearchUiService,
                searchSuggestionUiService,
                searchSuggestionList,
                searchUiService,
                searchWorkflowService,
                elementSearchService,
                sessionService,
                textBox_search.Text,
                viewModel.IsSearchPlaceholder,
                checkBox_SearchMatchCase.Checked,
                checkBox_SearchExactMatching.Checked,
                checkBox_SearchAll.Checked,
                comboBox_lists.SelectedIndex,
                dataGridView_elems.CurrentCell != null ? dataGridView_elems.CurrentCell.RowIndex : -1,
                dataGridView_item.CurrentCell != null ? dataGridView_item.CurrentCell.RowIndex : -1,
                comboBox_lists,
                dataGridView_elems,
                dataGridView_item,
                EnsureEquipmentTabForField,
                fieldIndex => valueRowIndexService.FindRowByFieldIndex(dataGridView_item, fieldIndex),
                value => viewModel.EnableSelectionItem = value,
                message => MessageBox.Show(message));
        }


        private void CheckSearchCondition(object sender, EventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSearchConditionChanged(
                searchBoxUiService,
                textBox_search,
                checkBox_SearchAll.Checked,
                searchPlaceholderService,
                () => mainWindowSearchUiService.HideSuggestions(searchSuggestionUiService, searchSuggestionList));
        }


        private void textBox_search_enter(object sender, EventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSearchEnter(
                mainWindowSearchUiService,
                searchBoxUiService,
                textBox_search,
                viewModel.IsSearchPlaceholder);
        }


        private void textBox_search_leave(object sender, EventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSearchLeave(
                mainWindowSearchUiService,
                searchBoxUiService,
                textBox_search,
                checkBox_SearchAll.Checked,
                placeholderTextService,
                searchSuggestionList,
                this,
                () => mainWindowSearchUiService.HideSuggestions(searchSuggestionUiService, searchSuggestionList));
        }


        private void textBox_search_TextChanged(object sender, EventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSearchTextChanged(
                mainWindowSearchUiService,
                searchSuggestionUiService,
                searchSuggestionService,
                searchSuggestionList,
                textBox_search.Text,
                checkBox_SearchMatchCase.Checked,
                viewModel.SearchSuggestionMaxHeight,
                viewModel.IsSearchPlaceholder,
                sessionService,
                viewModel.SearchSuggestionMax,
                listIndex => idGenerationService.GetIdFieldIndex(sessionService.ListCollection, listIndex),
                listIndex => fieldIndexLookupService.GetNameFieldIndex(sessionService.ListCollection, listIndex));
        }


        private void textBox_search_KeyDown(object sender, KeyEventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSearchKeyDown(
                mainWindowSearchUiService,
                searchBoxUiService,
                e,
                searchSuggestionList,
                suggestion => mainWindowSearchUiService.ApplySearchSuggestion(
                    searchSuggestionUiService,
                    suggestion,
                    (listIndex, rowIndex) => mainWindowSearchUiService.NavigateToListItem(
                        searchUiService,
                        sessionService.ListCollection,
                        listIndex,
                        rowIndex,
                        comboBox_lists,
                        dataGridView_elems,
                        value => viewModel.EnableSelectionItem = value),
                    searchSuggestionList),
                () => click_search(sender, EventArgs.Empty),
                () => mainWindowSearchUiService.HideSuggestions(searchSuggestionUiService, searchSuggestionList));
        }


        private void searchSuggestionList_MouseClick(object sender, MouseEventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSuggestionMouseClick(
                mainWindowSearchUiService,
                searchBoxUiService,
                e,
                searchSuggestionList,
                suggestion => mainWindowSearchUiService.ApplySearchSuggestion(
                    searchSuggestionUiService,
                    suggestion,
                    (listIndex, rowIndex) => mainWindowSearchUiService.NavigateToListItem(
                        searchUiService,
                        sessionService.ListCollection,
                        listIndex,
                        rowIndex,
                        comboBox_lists,
                        dataGridView_elems,
                        value => viewModel.EnableSelectionItem = value),
                    searchSuggestionList));
        }


        private void searchSuggestionList_KeyDown(object sender, KeyEventArgs e)
        {
            mainWindowSearchCoordinatorService.HandleSuggestionKeyDown(
                mainWindowSearchUiService,
                searchBoxUiService,
                e,
                searchSuggestionList,
                suggestion => mainWindowSearchUiService.ApplySearchSuggestion(
                    searchSuggestionUiService,
                    suggestion,
                    (listIndex, rowIndex) => mainWindowSearchUiService.NavigateToListItem(
                        searchUiService,
                        sessionService.ListCollection,
                        listIndex,
                        rowIndex,
                        comboBox_lists,
                        dataGridView_elems,
                        value => viewModel.EnableSelectionItem = value),
                    searchSuggestionList),
                () => textBox_search.Focus(),
                () => mainWindowSearchUiService.HideSuggestions(searchSuggestionUiService, searchSuggestionList));
        }
    }
}





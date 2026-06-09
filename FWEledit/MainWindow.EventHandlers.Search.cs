using System;
using System.Drawing;
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

        private void searchSuggestionList_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }

            e.DrawBackground();
            if (e.Index < 0 || e.Index >= listBox.Items.Count)
            {
                return;
            }

            SearchSuggestion suggestion = listBox.Items[e.Index] as SearchSuggestion;
            if (suggestion == null)
            {
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = selected ? Color.FromArgb(49, 93, 130) : Color.FromArgb(24, 26, 30);
            Color secondary = selected ? Color.FromArgb(231, 237, 244) : Color.FromArgb(133, 145, 162);
            Color border = selected ? Color.FromArgb(91, 145, 188) : Color.FromArgb(42, 48, 58);
            Color nameColor = ResolveSearchSuggestionNameColor(suggestion, selected);

            using (SolidBrush brush = new SolidBrush(background))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            Rectangle iconBounds = new Rectangle(e.Bounds.Left + 8, e.Bounds.Top + 5, 32, 32);
            Bitmap icon = ResolveSearchSuggestionIcon(suggestion);
            if (icon != null)
            {
                e.Graphics.DrawImage(icon, iconBounds);
            }

            using (Pen pen = new Pen(border))
            {
                e.Graphics.DrawRectangle(pen, iconBounds);
            }

            string title = !string.IsNullOrWhiteSpace(suggestion.NameText)
                ? suggestion.NameText
                : ("ID " + (suggestion.IdText ?? string.Empty));
            string subtitle = "ID " + (suggestion.IdText ?? string.Empty) + "  •  " + GetSearchSuggestionListLabel(suggestion);

            Rectangle titleBounds = new Rectangle(e.Bounds.Left + 48, e.Bounds.Top + 5, e.Bounds.Width - 56, 16);
            Rectangle subtitleBounds = new Rectangle(e.Bounds.Left + 48, e.Bounds.Top + 22, e.Bounds.Width - 56, 14);
            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix;

            TextRenderer.DrawText(e.Graphics, title, listBox.Font, titleBounds, nameColor, flags);
            TextRenderer.DrawText(e.Graphics, subtitle, listBox.Font, subtitleBounds, secondary, flags);

            e.DrawFocusRectangle();
        }

        private Bitmap ResolveSearchSuggestionIcon(SearchSuggestion suggestion)
        {
            if (suggestion == null
                || sessionService == null
                || sessionService.ListCollection == null
                || sessionService.Database == null
                || suggestion.ListIndex < 0
                || suggestion.ListIndex >= sessionService.ListCollection.Lists.Length
                || suggestion.ElementIndex < 0
                || suggestion.ElementIndex >= sessionService.ListCollection.Lists[suggestion.ListIndex].elementValues.Length)
            {
                return Properties.Resources.NoIcon;
            }

            int iconFieldIndex = fieldIndexLookupService.GetIconFieldIndex(sessionService.ListCollection, suggestion.ListIndex);
            if (iconFieldIndex < 0)
            {
                return Properties.Resources.NoIcon;
            }

            string rawIcon = sessionService.ListCollection.GetValue(suggestion.ListIndex, suggestion.ElementIndex, iconFieldIndex);
            if (string.IsNullOrWhiteSpace(rawIcon))
            {
                return Properties.Resources.NoIcon;
            }

            Bitmap portrait;
            if (creaturePortraitIconService.TryResolvePortrait(
                sessionService.Database,
                sessionService.ListCollection,
                suggestion.ListIndex,
                rawIcon,
                out portrait)
                && portrait != null)
            {
                return portrait;
            }

            string iconKey = iconResolutionService.ResolveIconKeyForList(
                sessionService.Database,
                sessionService.ListCollection,
                suggestion.ListIndex,
                rawIcon);
            if (!string.IsNullOrWhiteSpace(iconKey)
                && sessionService.Database.sourceBitmap != null
                && sessionService.Database.ContainsKey(iconKey))
            {
                return sessionService.Database.images(iconKey);
            }

            return Properties.Resources.NoIcon;
        }

        private Color ResolveSearchSuggestionNameColor(SearchSuggestion suggestion, bool selected)
        {
            if (selected)
            {
                return Color.White;
            }

            if (suggestion == null
                || sessionService == null
                || sessionService.ListCollection == null
                || suggestion.ListIndex < 0
                || suggestion.ListIndex >= sessionService.ListCollection.Lists.Length
                || suggestion.ElementIndex < 0
                || suggestion.ElementIndex >= sessionService.ListCollection.Lists[suggestion.ListIndex].elementValues.Length)
            {
                return Color.FromArgb(226, 231, 239);
            }

            int qualityFieldIndex = fieldIndexLookupService.GetItemQualityFieldIndex(sessionService.ListCollection, suggestion.ListIndex);
            if (qualityFieldIndex < 0)
            {
                return Color.FromArgb(226, 231, 239);
            }

            int quality;
            if (!int.TryParse(sessionService.ListCollection.GetValue(suggestion.ListIndex, suggestion.ElementIndex, qualityFieldIndex), out quality))
            {
                return Color.FromArgb(226, 231, 239);
            }

            Color? qualityColor = itemQualityColorService.GetQualityColor(quality);
            return qualityColor ?? Color.FromArgb(226, 231, 239);
        }

        private string GetSearchSuggestionListLabel(SearchSuggestion suggestion)
        {
            if (suggestion == null
                || sessionService == null
                || sessionService.ListCollection == null
                || suggestion.ListIndex < 0
                || suggestion.ListIndex >= sessionService.ListCollection.Lists.Length)
            {
                return "[" + (suggestion != null ? suggestion.ListIndex.ToString("D3") : "000") + "]";
            }

            string listName = sessionService.ListCollection.Lists[suggestion.ListIndex].listName ?? string.Empty;
            string[] split = listName.Split(new string[] { " - " }, StringSplitOptions.None);
            string normalized = split.Length > 1 ? split[1].Trim() : listName.Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "List " + suggestion.ListIndex.ToString();
            }

            return "[" + suggestion.ListIndex.ToString("D3") + "] " + normalized;
        }
    }
}





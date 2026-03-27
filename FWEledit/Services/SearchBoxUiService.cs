using System;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class SearchBoxUiService
    {
        public void HandleSearchEnter(TextBox textBox, Func<string, bool> isPlaceholder)
        {
            if (textBox == null || isPlaceholder == null)
            {
                return;
            }

            if (isPlaceholder(textBox.Text))
            {
                textBox.Clear();
            }
        }

        public void HandleSearchLeave(
            TextBox textBox,
            bool searchAll,
            PlaceholderTextService placeholderTextService,
            ListBox suggestionList,
            Control invoker,
            Action hideSuggestions)
        {
            if (textBox == null || placeholderTextService == null)
            {
                return;
            }

            string placeholder = searchAll ? "VALUE" : "ID or NAME";
            placeholderTextService.RestoreIfEmpty(textBox, placeholder);

            if (invoker != null && hideSuggestions != null)
            {
                invoker.BeginInvoke((Action)(() =>
                {
                    if (suggestionList != null && !suggestionList.Focused && !textBox.Focused)
                    {
                        hideSuggestions();
                    }
                }));
            }
        }

        public void HandleSearchKeyDown(
            KeyEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion,
            Action performSearch,
            Action hideSuggestions)
        {
            if (e == null)
            {
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (suggestionList != null && suggestionList.Visible && suggestionList.SelectedItem != null)
                {
                    if (applySuggestion != null)
                    {
                        applySuggestion(suggestionList.SelectedItem as SearchSuggestion);
                    }
                }
                else if (performSearch != null)
                {
                    performSearch();
                }
                return;
            }

            if (e.KeyCode == Keys.Down && suggestionList != null && suggestionList.Visible && suggestionList.Items.Count > 0)
            {
                suggestionList.Focus();
                if (suggestionList.SelectedIndex < 0)
                {
                    suggestionList.SelectedIndex = 0;
                }
                e.SuppressKeyPress = true;
            }

            if (e.KeyCode == Keys.Escape && suggestionList != null && suggestionList.Visible)
            {
                if (hideSuggestions != null)
                {
                    hideSuggestions();
                }
                e.SuppressKeyPress = true;
            }
        }

        public void HandleSuggestionMouseClick(
            MouseEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion)
        {
            if (suggestionList == null || e == null)
            {
                return;
            }

            int index = suggestionList.IndexFromPoint(e.Location);
            if (index >= 0 && index < suggestionList.Items.Count && applySuggestion != null)
            {
                applySuggestion(suggestionList.Items[index] as SearchSuggestion);
            }
        }

        public void HandleSuggestionKeyDown(
            KeyEventArgs e,
            ListBox suggestionList,
            Action<SearchSuggestion> applySuggestion,
            Action focusSearch,
            Action hideSuggestions)
        {
            if (e == null)
            {
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                if (suggestionList != null && applySuggestion != null)
                {
                    applySuggestion(suggestionList.SelectedItem as SearchSuggestion);
                }
                return;
            }

            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                if (hideSuggestions != null)
                {
                    hideSuggestions();
                }
                if (focusSearch != null)
                {
                    focusSearch();
                }
            }
        }

        public void UpdateSearchPlaceholder(
            TextBox textBox,
            bool searchAll,
            SearchPlaceholderService placeholderService,
            Action hideSuggestions)
        {
            if (textBox == null || placeholderService == null)
            {
                return;
            }

            textBox.Text = placeholderService.ResolvePlaceholderText(searchAll, textBox.Text);
            if (hideSuggestions != null)
            {
                hideSuggestions();
            }
        }
    }
}

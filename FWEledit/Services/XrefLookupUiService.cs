using System;

namespace FWEledit
{
    public sealed class XrefLookupUiService
    {
        public void ShowResults(XrefLookupResult result, int conversationListIndex, Action<string> showMessage)
        {
            if (result == null)
            {
                if (showMessage != null)
                {
                    showMessage("No results found");
                }
                return;
            }

            if (result.IsConversationList)
            {
                if (showMessage != null)
                {
                    showMessage("Operation not supported in List " + conversationListIndex.ToString());
                }
                return;
            }

            if (!result.Success)
            {
                if (showMessage != null)
                {
                    showMessage(string.IsNullOrWhiteSpace(result.ErrorMessage) ? "Invalid List" : result.ErrorMessage);
                }
                return;
            }

            if (!result.HasRules)
            {
                if (showMessage != null)
                {
                    showMessage(string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? "No cross-reference rules for this list."
                        : result.ErrorMessage);
                }
                return;
            }

            if (!result.HasResults)
            {
                if (showMessage != null)
                {
                    showMessage("No results found");
                }
                return;
            }

            ReferencesWindow window = new ReferencesWindow();
            for (int i = 0; i < result.Rows.Count; i++)
            {
                XrefResultRow row = result.Rows[i];
                window.dataGridView.Rows.Add(new string[]
                {
                    row.ListIndex.ToString(),
                    row.ListName,
                    row.ItemId,
                    row.ItemName,
                    row.FieldLabel
                });
            }
            window.Show();
        }
    }
}

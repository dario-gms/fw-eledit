using System;
using System.Drawing;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ValueChangeResultUiService
    {
        public bool ApplyResult(
            ValueChangeResult result,
            DataGridView itemGrid,
            DataGridView elementGrid,
            ValueChangeContext context,
            eListCollection listCollection,
            eListConversation conversationList,
            Action<int, int, DataGridViewRow> applyQualityColor,
            Action markUnsavedChanges,
            Action<string> showMessage)
        {
            if (result == null || itemGrid == null || elementGrid == null || context == null)
            {
                return false;
            }

            if (!result.Success)
            {
                if (result.MarkInvalid)
                {
                    itemGrid.Rows[context.GridRow].Cells[2].Style.ForeColor = Color.Red;
                }
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage) && showMessage != null)
                {
                    showMessage(result.ErrorMessage);
                }
                return false;
            }

            if (!string.IsNullOrWhiteSpace(result.DisplayValue))
            {
                itemGrid.Rows[context.GridRow].Cells[2].Value = result.DisplayValue;
            }

            if (result.MarkDirty)
            {
                itemGrid.Rows[context.GridRow].Cells[2].Style.ForeColor = Color.DeepSkyBlue;
                if (markUnsavedChanges != null)
                {
                    markUnsavedChanges();
                }
            }

            if (result.ListRowUpdates.Count > 0)
            {
                for (int i = 0; i < result.ListRowUpdates.Count; i++)
                {
                    ListRowUpdate update = result.ListRowUpdates[i];
                    if (update.GridRowIndex < 0 || update.GridRowIndex >= elementGrid.Rows.Count)
                    {
                        continue;
                    }
                    DataGridViewRow row = elementGrid.Rows[update.GridRowIndex];
                    if (update.IdValue != null)
                    {
                        row.Cells[0].Value = update.IdValue;
                    }
                    if (update.Icon != null)
                    {
                        row.Cells[1].Value = update.Icon;
                    }
                    if (!string.IsNullOrWhiteSpace(update.DisplayName))
                    {
                        row.Cells[2].Value = update.DisplayName;
                    }
                    if (update.UpdateQualityColor && applyQualityColor != null)
                    {
                        applyQualityColor(context.ListIndex, update.ElementIndex, row);
                    }
                }
            }

            UpdateConversationTalkText(
                listCollection,
                conversationList,
                context,
                itemGrid);

            return true;
        }

        private static void UpdateConversationTalkText(
            eListCollection listCollection,
            eListConversation conversationList,
            ValueChangeContext context,
            DataGridView itemGrid)
        {
            if (listCollection == null || conversationList == null)
            {
                return;
            }
            if (conversationList.talk_procs == null)
            {
                return;
            }
            if (context.ListIndex != listCollection.ConversationListIndex)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(context.EditedField))
            {
                return;
            }
            if (!context.EditedField.StartsWith("window_") || !context.EditedField.EndsWith("_talk_text"))
            {
                return;
            }

            int q;
            if (!int.TryParse(context.EditedField.Replace("window_", "").Replace("_talk_text", ""), out q))
            {
                return;
            }
            if (context.ElementIndex < 0 || context.ElementIndex >= conversationList.talk_procs.Length)
            {
                return;
            }
            if (q < 0 || q >= conversationList.talk_procs[context.ElementIndex].windows.Length)
            {
                return;
            }

            int len = conversationList.talk_procs[context.ElementIndex].windows[q].talk_text_len;
            itemGrid[1, context.GridRow].Value = "wstring:" + len;
        }
    }
}
